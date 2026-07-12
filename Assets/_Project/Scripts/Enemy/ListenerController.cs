using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Nordo.Core;
using Nordo.Core.Events;
using Nordo.Noise;

namespace Nordo.Enemy
{
    /// <summary>
    /// The Listener — a blind hunter driven entirely by the acoustic system. It is a
    /// <see cref="NoiseListenerBase"/> (so it receives already-attenuated noise from the
    /// <c>NoiseSystem</c>) that feeds a finite state machine: Patrol → Investigate → Search → Chase →
    /// Attack, with Lose-target and Return folded in. There is <b>no vision</b> anywhere in this class;
    /// every decision comes from what it hears and where.
    /// <para>
    /// This class is the shared context and mediator for the state objects: it owns the NavMeshAgent,
    /// config, blackboard (memory), suspicion, and the animator/audio hooks, and exposes the small,
    /// intention-revealing API the states use (MoveTo, ReachedDestination, DistanceToPlayer…).
    /// </para>
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [DisallowMultipleComponent]
    public sealed class ListenerController : NoiseListenerBase
    {
        [Header("Configuration")]
        [Tooltip("Difficulty/behaviour parameters. If empty, a default is created at runtime.")]
        [SerializeField] private ListenerConfig _config;

        [Header("Patrol")]
        [Tooltip("Optional patrol route. If empty, the enemy idles at its start position when calm.")]
        [SerializeField] private PatrolRoute _patrolRoute;

        [Header("Target")]
        [Tooltip("The player transform (for attack proximity). If empty, found by tag.")]
        [SerializeField] private Transform _playerTarget;

        [Tooltip("Tag used to find the player if no transform is assigned.")]
        [SerializeField] private string _playerTag = "Player";

        [Header("Doors")]
        [Tooltip("Layers to probe for closed doors to push open.")]
        [SerializeField] private LayerMask _doorMask = ~0;

        [Header("Debug")]
        [Tooltip("Draw state, hearing range, memory and path gizmos.")]
        [SerializeField] private bool _drawDebug = true;

        private NavMeshAgent _agent;
        private ListenerBlackboard _blackboard;
        private IListenerAnimator _animator;
        private ListenerAudio _audio;
        private readonly Dictionary<ListenerStateId, IListenerState> _states = new();
        private IListenerState _current;
        private float _suspicion;

        // --- Public context for states ---------------------------------------------

        /// <summary>The nav agent driving movement.</summary>
        public NavMeshAgent Agent => _agent;

        /// <summary>The active configuration (never null after Awake).</summary>
        public ListenerConfig Config => _config;

        /// <summary>Short-term memory (last heard position, home, etc.).</summary>
        public ListenerBlackboard Blackboard => _blackboard;

        /// <summary>The patrol route (may be null).</summary>
        public PatrolRoute Patrol => _patrolRoute;

        /// <summary>Assigns a patrol route at runtime (used by the vertical-slice builder).</summary>
        public void AssignPatrol(PatrolRoute route) => _patrolRoute = route;

        /// <summary>The player transform, if known.</summary>
        public Transform Player => _playerTarget;

        /// <summary>Current suspicion, in [0, 1].</summary>
        public float Suspicion => _suspicion;

        /// <summary>The currently active behavioural state.</summary>
        public ListenerStateId CurrentState => _current?.Id ?? ListenerStateId.Patrol;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();

            if (_config == null)
            {
                _config = ScriptableObject.CreateInstance<ListenerConfig>();
            }

            // Feed the hearing radius from config into the base listener.
            _hearingRange = _config.HearingRange;

            _blackboard = new ListenerBlackboard { HomePosition = transform.position };
            _animator = GetComponent<IListenerAnimator>();
            _audio = GetComponent<ListenerAudio>();

            if (_playerTarget == null && !string.IsNullOrEmpty(_playerTag))
            {
                GameObject found = GameObject.FindGameObjectWithTag(_playerTag);
                if (found != null)
                {
                    _playerTarget = found.transform;
                }
            }

            _agent.angularSpeed = _config.AngularSpeed;
            _agent.stoppingDistance = _config.ReachThreshold * 0.5f;
            _agent.autoBraking = true;

            BuildStates();
        }

        protected override void Start()
        {
            base.Start(); // NoiseListenerBase registration fallback
            ChangeState(ListenerStateId.Patrol);
        }

        private void Update()
        {
            if (_current == null || _config == null)
            {
                return;
            }

            float dt = Time.deltaTime;

            // Suspicion always decays; hearing tops it back up in OnHeardNoise.
            _suspicion = Mathf.Max(0f, _suspicion - _config.SuspicionDecayPerSecond * dt);

            ProbeForDoors();
            _current.Tick(dt);

            // Presentation hooks.
            if (_animator != null)
            {
                _animator.SetLocomotion(_config.ChaseSpeed > 0f ? _agent.velocity.magnitude / _config.ChaseSpeed : 0f);
            }

            if (_audio != null)
            {
                _audio.SetAggression(AggressionForState(_current.Id));
            }
        }

        // --- Noise input ------------------------------------------------------------

        /// <inheritdoc />
        public override void OnHeardNoise(in NoiseStimulus stimulus, float perceivedLoudness)
        {
            _blackboard.RecordNoise(stimulus.Position, perceivedLoudness, stimulus.Priority);
            _suspicion = Mathf.Clamp01(_suspicion + perceivedLoudness * _config.SuspicionPerLoudness * PriorityMultiplier(stimulus.Priority));
        }

        // --- State machine ----------------------------------------------------------

        private void BuildStates()
        {
            _states[ListenerStateId.Patrol] = new PatrolState(this);
            _states[ListenerStateId.Investigate] = new InvestigateState(this);
            _states[ListenerStateId.Search] = new SearchState(this);
            _states[ListenerStateId.Chase] = new ChaseState(this);
            _states[ListenerStateId.Attack] = new AttackState(this);
            _states[ListenerStateId.Return] = new ReturnState(this);
        }

        /// <summary>Transitions to a new state (no-op if already in it).</summary>
        public void ChangeState(ListenerStateId id)
        {
            if (_current != null && _current.Id == id)
            {
                return;
            }

            ListenerStateId previous = _current?.Id ?? id;
            _current?.Exit();
            _current = _states[id];

            _animator?.SetState(id);
            _audio?.OnStateEntered(id, previous);
            EventBus<ListenerStateChangedEvent>.Raise(new ListenerStateChangedEvent(id, previous));

            _current.Enter();
        }

        // --- Movement / query helpers used by states --------------------------------

        /// <summary>Sets the agent's move speed for the current behaviour.</summary>
        public void SetSpeed(float speed) => _agent.speed = speed;

        /// <summary>Paths the agent toward a world position.</summary>
        public void MoveTo(Vector3 position)
        {
            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.SetDestination(position);
            }
        }

        /// <summary>Halts the agent where it is.</summary>
        public void StopMoving()
        {
            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = true;
            }
        }

        /// <summary>True when the agent has effectively arrived at its destination.</summary>
        public bool ReachedDestination()
        {
            if (!_agent.isOnNavMesh || _agent.pathPending)
            {
                return false;
            }

            return _agent.remainingDistance <= Mathf.Max(_agent.stoppingDistance, _config.ReachThreshold);
        }

        /// <summary>Samples a reachable point on the navmesh within a radius of a centre.</summary>
        public bool TrySampleAround(Vector3 center, float radius, out Vector3 result)
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                Vector3 random = center + Random.insideUnitSphere * radius;
                random.y = center.y;
                if (NavMesh.SamplePosition(random, out NavMeshHit hit, radius, NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }
            }

            result = center;
            return false;
        }

        /// <summary>Distance to the player, or infinity if the player is unknown.</summary>
        public float DistanceToPlayer()
        {
            return _playerTarget != null ? Vector3.Distance(transform.position, _playerTarget.position) : Mathf.Infinity;
        }

        /// <summary>Rotates smoothly to face a world position (horizontal only).</summary>
        public void FaceTowards(Vector3 worldPosition, float deltaTime)
        {
            Vector3 dir = worldPosition - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, _config.AngularSpeed * deltaTime);
        }

        /// <summary>Fires the animator's attack and returns immediately (the strike timing is state-driven).</summary>
        public void PlayAttackAnimation() => _animator?.TriggerAttack();

        // --- Internals --------------------------------------------------------------

        private void ProbeForDoors()
        {
            Vector3 velocity = _agent.velocity;
            if (velocity.sqrMagnitude < 0.25f)
            {
                return;
            }

            Vector3 origin = transform.position + Vector3.up;
            if (Physics.Raycast(origin, velocity.normalized, out RaycastHit hit, _config.DoorProbeDistance, _doorMask, QueryTriggerInteraction.Ignore))
            {
                IAutoDoor door = hit.collider.GetComponentInParent<IAutoDoor>();
                if (door != null && door.CanBeOpenedByAI)
                {
                    door.OpenForAI();
                }
            }
        }

        private static float PriorityMultiplier(SoundPriority priority) => priority switch
        {
            SoundPriority.Alarming => 1.4f,
            SoundPriority.Notable => 1f,
            SoundPriority.Minor => 0.7f,
            _ => 0.4f
        };

        private static float AggressionForState(ListenerStateId state) => state switch
        {
            ListenerStateId.Chase or ListenerStateId.Attack => 1f,
            ListenerStateId.Investigate or ListenerStateId.Search => 0.5f,
            _ => 0f
        };

        private void OnDrawGizmos()
        {
            if (!_drawDebug)
            {
                return;
            }

            Gizmos.color = StateColor();
            Gizmos.DrawWireSphere(transform.position, _config != null ? _config.HearingRange : _hearingRange);

            if (_config != null)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
                Gizmos.DrawWireSphere(transform.position, _config.AttackRange);
            }

            if (_blackboard != null && _blackboard.HasMemory)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, _blackboard.LastHeardPosition);
                Gizmos.DrawSphere(_blackboard.LastHeardPosition, 0.25f);
            }

            if (Application.isPlaying && _agent != null && _agent.hasPath)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, _agent.destination);
            }
        }

        private Color StateColor() => CurrentState switch
        {
            ListenerStateId.Chase => Color.red,
            ListenerStateId.Attack => new Color(1f, 0f, 0.4f),
            ListenerStateId.Investigate => new Color(1f, 0.6f, 0f),
            ListenerStateId.Search => Color.yellow,
            ListenerStateId.Return => new Color(0.4f, 0.6f, 1f),
            _ => new Color(0.3f, 0.9f, 0.4f)
        };
    }
}
