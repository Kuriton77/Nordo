using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;
using Nordo.Input;

namespace Nordo.Player
{
    /// <summary>
    /// The first-person locomotion motor: walk, sprint, crouch, jump, gravity and stamina,
    /// built on Unity's <see cref="CharacterController"/> for reliable, non-physics-jitter movement.
    /// <para>
    /// This component owns the per-frame input refresh (<see cref="InputReader.Tick"/>) and is the
    /// authority on the player's <see cref="LocomotionStance"/>. It raises stance/jump/land events
    /// on the <see cref="EventBus{T}"/> so audio, animation and the future noise system can react
    /// without referencing the player directly — the acoustic-stealth core hangs off these signals.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public sealed class FirstPersonMotor : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Shared input asset. Assign the same InputReader used by PlayerLook.")]
        [SerializeField] private InputReader _input;

        [Tooltip("Tuning data for speeds, crouch, jump and stamina.")]
        [SerializeField] private MovementSettings _settings;

        [Header("Ground Check")]
        [Tooltip("Extra downward probe distance beyond the controller's own grounded flag, for coyote-style reliability.")]
        [Range(0f, 0.5f)] [SerializeField] private float _groundProbeDistance = 0.12f;

        [Tooltip("Layers considered solid ground for jumping and landing.")]
        [SerializeField] private LayerMask _groundMask = ~0;

        // --- Public read-only state (for UI / audio / stealth later) ---------

        /// <summary>The player's current movement stance.</summary>
        public LocomotionStance Stance { get; private set; } = LocomotionStance.Idle;

        /// <summary>Current stamina, in seconds of remaining sprint. Range [0, MaxStamina].</summary>
        public float Stamina { get; private set; }

        /// <summary>Stamina as a 0–1 fraction, convenient for meters/UI.</summary>
        public float StaminaNormalized => _settings != null && _settings.MaxStamina > 0f
            ? Stamina / _settings.MaxStamina
            : 0f;

        /// <summary>Current horizontal speed in metres/second.</summary>
        public float CurrentSpeed => new Vector2(_velocity.x, _velocity.z).magnitude;

        /// <summary>True when the controller is on the ground this frame.</summary>
        public bool IsGrounded { get; private set; }

        // --- Internals -------------------------------------------------------

        private CharacterController _controller;
        private Vector3 _velocity;            // full 3D velocity; Y carries gravity/jump
        private float _targetHeight;
        private float _standingCenterY;
        private float _timeSinceSprint;
        private bool _wasGroundedLastFrame;
        private float _previousFallSpeed;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (_settings == null)
            {
                Debug.LogError($"[FirstPersonMotor] No MovementSettings assigned on '{name}'.", this);
                enabled = false;
                return;
            }

            Stamina = _settings.MaxStamina;
            _targetHeight = _settings.StandingHeight;
            _controller.height = _settings.StandingHeight;
            _standingCenterY = _controller.center.y;
        }

        private void OnEnable()
        {
            if (_input != null)
            {
                _input.EnableGameplayInput();
                _input.JumpPerformed += OnJumpPerformed;
            }
        }

        private void OnDisable()
        {
            if (_input != null)
            {
                _input.JumpPerformed -= OnJumpPerformed;
            }
        }

        private void Update()
        {
            if (_input == null || _settings == null)
            {
                return;
            }

            // Refresh the shared input snapshot once per frame, before anyone reads it.
            _input.Tick();

            float dt = Time.deltaTime;

            UpdateGrounded();
            HandleCrouch(dt);
            HandleStamina(dt);

            Vector3 desiredHorizontal = ComputeHorizontalVelocity(dt);
            ApplyGravityAndJump(dt);

            // Combine and move.
            _velocity.x = desiredHorizontal.x;
            _velocity.z = desiredHorizontal.z;
            _controller.Move(_velocity * dt);

            DetectLanding();
            UpdateStance();

            _wasGroundedLastFrame = IsGrounded;
        }

        /// <summary>
        /// Determines grounded state. We trust the controller's flag but also cast a short ray so
        /// stepping over small gaps or slopes doesn't briefly register as airborne.
        /// </summary>
        private void UpdateGrounded()
        {
            if (_controller.isGrounded)
            {
                IsGrounded = true;
                return;
            }

            Vector3 origin = transform.position + _controller.center;
            float rayLength = (_controller.height * 0.5f) + _groundProbeDistance;
            IsGrounded = Physics.Raycast(origin, Vector3.down, rayLength, _groundMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Smoothly blends capsule height for crouching, refusing to stand under low ceilings.</summary>
        private void HandleCrouch(float dt)
        {
            bool wantsCrouch = _input.CrouchHeld;

            // If trying to stand, make sure there is headroom above.
            if (!wantsCrouch && _targetHeight < _settings.StandingHeight && HasCeilingAbove())
            {
                wantsCrouch = true; // forced to stay crouched
            }

            _targetHeight = wantsCrouch ? _settings.CrouchHeight : _settings.StandingHeight;

            float speed = _settings.StandingHeight == _settings.CrouchHeight
                ? float.MaxValue
                : Mathf.Abs(_settings.StandingHeight - _settings.CrouchHeight) / Mathf.Max(0.0001f, _settings.CrouchTransitionTime);

            float newHeight = Mathf.MoveTowards(_controller.height, _targetHeight, speed * dt);
            ApplyHeight(newHeight);
        }

        /// <summary>Applies a capsule height while keeping the feet planted (adjusting the centre).</summary>
        private void ApplyHeight(float height)
        {
            float delta = height - _controller.height;
            _controller.height = height;

            // Raise/lower centre by half the delta so the base of the capsule stays put.
            Vector3 center = _controller.center;
            center.y = _standingCenterY - (_settings.StandingHeight - height) * 0.5f;
            _controller.center = center;
        }

        /// <summary>Casts upward to see whether the player can stand without clipping into geometry.</summary>
        private bool HasCeilingAbove()
        {
            Vector3 origin = transform.position + Vector3.up * _controller.radius;
            float castDistance = _settings.StandingHeight - _controller.radius;
            return Physics.SphereCast(origin, _controller.radius * 0.95f, Vector3.up, out _, castDistance, _groundMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Drains stamina while sprinting and regenerates it (after a delay) otherwise.</summary>
        private void HandleStamina(float dt)
        {
            bool sprintingNow = IsTryingToSprint();

            if (sprintingNow)
            {
                Stamina = Mathf.Max(0f, Stamina - _settings.StaminaDrainPerSecond * dt);
                _timeSinceSprint = 0f;
            }
            else
            {
                _timeSinceSprint += dt;
                if (_timeSinceSprint >= _settings.StaminaRegenDelay)
                {
                    Stamina = Mathf.Min(_settings.MaxStamina, Stamina + _settings.StaminaRegenPerSecond * dt);
                }
            }
        }

        /// <summary>True when the player is holding sprint, moving forward-ish, upright, and has stamina.</summary>
        private bool IsTryingToSprint()
        {
            return _input.SprintHeld
                   && Stamina > 0f
                   && !_input.CrouchHeld
                   && _input.MoveInput.sqrMagnitude > 0.01f;
        }

        /// <summary>Computes the smoothed horizontal velocity for this frame in world space.</summary>
        private Vector3 ComputeHorizontalVelocity(float dt)
        {
            Vector2 move = Vector2.ClampMagnitude(_input.MoveInput, 1f);
            Vector3 wishDir = (transform.right * move.x + transform.forward * move.y);

            float targetSpeed = ResolveTargetSpeed();
            Vector3 targetVelocity = wishDir * targetSpeed;

            Vector3 currentHorizontal = new Vector3(_velocity.x, 0f, _velocity.z);

            // Accelerate toward target when there is input, decelerate toward zero otherwise.
            float rate = move.sqrMagnitude > 0.01f ? _settings.Acceleration : _settings.Deceleration;
            Vector3 result = Vector3.MoveTowards(currentHorizontal, targetVelocity, rate * dt);
            return result;
        }

        /// <summary>Selects the appropriate max speed for the current stance intent.</summary>
        private float ResolveTargetSpeed()
        {
            if (_input.CrouchHeld || _targetHeight < _settings.StandingHeight - 0.01f)
            {
                return _settings.CrouchSpeed;
            }

            if (IsTryingToSprint())
            {
                return _settings.SprintSpeed;
            }

            return _settings.WalkSpeed;
        }

        /// <summary>Applies gravity, ground-stick, and jump impulse to the vertical velocity.</summary>
        private void ApplyGravityAndJump(float dt)
        {
            if (IsGrounded && _velocity.y <= 0f)
            {
                // Keep a small downward force so we hug the ground and register isGrounded reliably.
                _velocity.y = -_settings.GroundStickForce;

                if (_input != null && ConsumeJumpRequest())
                {
                    // v = sqrt(2 * g * h) gives the exact launch speed for the desired apex height.
                    _velocity.y = Mathf.Sqrt(2f * _settings.Gravity * _settings.JumpHeight);
                    EventBus<PlayerJumpedEvent>.Raise(new PlayerJumpedEvent(transform.position));
                }
            }
            else
            {
                _velocity.y -= _settings.Gravity * dt;
            }

            // Cache fall speed pre-move so DetectLanding can report impact velocity.
            _previousFallSpeed = _velocity.y < 0f ? -_velocity.y : 0f;
        }

        // Jump is edge-triggered via the InputReader event; we latch it so a press between
        // frames is never missed regardless of frame rate.
        private bool _jumpQueued;

        private void OnJumpPerformed() => _jumpQueued = true;

        private bool ConsumeJumpRequest()
        {
            if (!_jumpQueued)
            {
                return false;
            }

            _jumpQueued = false;
            return true;
        }

        /// <summary>Raises a landing event on the transition from airborne to grounded.</summary>
        private void DetectLanding()
        {
            if (IsGrounded && !_wasGroundedLastFrame)
            {
                EventBus<PlayerLandedEvent>.Raise(new PlayerLandedEvent(transform.position, _previousFallSpeed));
            }
        }

        /// <summary>Recomputes the stance and broadcasts a change event when it differs.</summary>
        private void UpdateStance()
        {
            LocomotionStance newStance = DetermineStance();
            if (newStance != Stance)
            {
                Stance = newStance;
                EventBus<PlayerStanceChangedEvent>.Raise(new PlayerStanceChangedEvent(newStance));
            }
        }

        private LocomotionStance DetermineStance()
        {
            bool moving = CurrentSpeed > 0.15f && _input.MoveInput.sqrMagnitude > 0.01f;
            if (!moving)
            {
                return LocomotionStance.Idle;
            }

            if (_input.CrouchHeld || _controller.height < _settings.StandingHeight - 0.01f)
            {
                return LocomotionStance.Crouching;
            }

            return IsTryingToSprint() ? LocomotionStance.Sprinting : LocomotionStance.Walking;
        }
    }
}
