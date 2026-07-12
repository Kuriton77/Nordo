using System.Collections.Generic;
using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Noise
{
    /// <summary>
    /// The central sound-propagation service and the beating heart of Nordo's stealth. It listens on
    /// the single canonical <see cref="NoiseEvent"/> channel (every emitter — footsteps, breath,
    /// machines, impacts, doors — raises one), attenuates each stimulus by distance falloff and
    /// multi-occluder muffling, and delivers only the perceivable results to registered
    /// <see cref="INoiseListener"/>s (the enemy's ears in Milestone 7).
    /// <para>
    /// Registered in the <see cref="ServiceLocator"/> as <see cref="INoiseService"/> so emitters and
    /// listeners never reference each other. Optimised for large levels: a cheap distance early-out
    /// runs before any raycast, occlusion uses a preallocated <c>RaycastNonAlloc</c> buffer, and there
    /// is zero per-noise heap allocation. The propagation maths lives in <see cref="NoiseAttenuation"/>
    /// so it can be unit-tested.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-50)] // register before most gameplay Awakes/OnEnables that might query it
    [DisallowMultipleComponent]
    public sealed class NoiseSystem : MonoBehaviour, INoiseService
    {
        [Header("Distance Falloff")]
        [Tooltip("Maps normalized distance (0 at source → 1 at the edge of range) to a loudness " +
                 "multiplier. Leave as the default linear ramp, or shape it for a sharper near-field.")]
        [SerializeField] private AnimationCurve _falloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [Header("Occlusion")]
        [Tooltip("If enabled, walls between a sound and a listener muffle it (one raycast per listener in range).")]
        [SerializeField] private bool _useOcclusion = true;

        [Tooltip("Layers that block/muffle sound (walls, doors). Should exclude the player and enemies.")]
        [SerializeField] private LayerMask _occluderMask = ~0;

        [Tooltip("Fraction of loudness that survives passing through ONE occluder (compounds per wall).")]
        [Range(0f, 1f)] [SerializeField] private float _occlusionTransmission = 0.4f;

        [Tooltip("Max occluders counted between a sound and a listener (buffer size). Higher = costlier.")]
        [Range(1, 32)] [SerializeField] private int _maxOccluders = 8;

        [Header("Delivery")]
        [Tooltip("Perceived loudness below this is treated as inaudible and not delivered.")]
        [Range(0f, 0.2f)] [SerializeField] private float _audibilityFloor = 0.02f;

        [Header("Debug")]
        [Tooltip("Log every perceived noise to the console (very verbose; QA only).")]
        [SerializeField] private bool _logHeardNoises;

        // Live listeners. A List keeps iteration allocation-free and cache-friendly for the small
        // counts we expect (a handful of AI agents), which beats a HashSet here.
        private readonly List<INoiseListener> _listeners = new();

        // Preallocated buffer for occlusion counting — reused every query, so no GC on the hot path.
        private RaycastHit[] _occlusionBuffer;

        /// <summary>The most recent stimulus reported, exposed for debug/telemetry.</summary>
        public NoiseStimulus LastStimulus { get; private set; }

        /// <summary>Number of currently registered listeners (for diagnostics/stress testing).</summary>
        public int ListenerCount => _listeners.Count;

        private void Awake()
        {
            _occlusionBuffer = new RaycastHit[Mathf.Max(1, _maxOccluders)];
            ServiceLocator.Register<INoiseService>(this);
        }

        private void OnEnable()
        {
            EventBus<NoiseEvent>.Subscribe(OnNoiseEvent);
        }

        private void OnDisable()
        {
            EventBus<NoiseEvent>.Unsubscribe(OnNoiseEvent);
        }

        private void OnDestroy()
        {
            // Only clear the registration if we're still the active service.
            if (ServiceLocator.TryGet(out INoiseService current) && ReferenceEquals(current, this))
            {
                ServiceLocator.Unregister<INoiseService>();
            }
        }

        /// <inheritdoc />
        public void RegisterListener(INoiseListener listener)
        {
            if (listener != null && !_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        /// <inheritdoc />
        public void UnregisterListener(INoiseListener listener)
        {
            _listeners.Remove(listener);
        }

        /// <inheritdoc />
        public void ReportNoise(in NoiseStimulus stimulus)
        {
            LastStimulus = stimulus;

            float effectiveRange = stimulus.EffectiveRange;
            for (int i = 0; i < _listeners.Count; i++)
            {
                INoiseListener listener = _listeners[i];
                if (listener == null)
                {
                    continue;
                }

                Vector3 listenerPos = listener.Position;
                float distance = Vector3.Distance(stimulus.Position, listenerPos);

                // Cheap early-out: if the sound cannot reach even the listener's own hearing radius,
                // skip it before doing any raycast. This is the hot path in a busy level.
                float reach = Mathf.Max(effectiveRange, listener.HearingRange);
                if (distance > reach || reach <= 0f)
                {
                    continue;
                }

                int occluders = _useOcclusion ? CountOccluders(stimulus.Position, listenerPos, distance) : 0;
                float perceived = NoiseAttenuation.Perceive(
                    distance, effectiveRange, stimulus.Loudness, occluders, _occlusionTransmission, _falloffCurve);

                if (perceived < _audibilityFloor)
                {
                    continue;
                }

                if (_logHeardNoises)
                {
                    Debug.Log($"[NoiseSystem] {stimulus.Kind} heard @ {perceived:0.00} " +
                              $"(priority {stimulus.Priority}, {occluders} occluder(s)).");
                }

                listener.OnHeardNoise(in stimulus, perceived);
            }
        }

        /// <summary>Counts blockers between a sound and a listener using the preallocated buffer.</summary>
        private int CountOccluders(Vector3 from, Vector3 to, float distance)
        {
            if (distance <= 0.01f)
            {
                return 0;
            }

            Vector3 direction = (to - from) / distance;
            return Physics.RaycastNonAlloc(from, direction, _occlusionBuffer, distance, _occluderMask, QueryTriggerInteraction.Ignore);
        }

        private void OnNoiseEvent(NoiseEvent evt) => ReportNoise(evt.Stimulus);
    }
}
