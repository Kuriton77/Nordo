using System.Collections.Generic;
using UnityEngine;
using Nordo.Core;

namespace Nordo.CameraFeel
{
    /// <summary>
    /// The heart of the camera-feel stack. Each frame it builds a <see cref="CameraRigState"/>,
    /// asks every attached <see cref="ICameraEffect"/> for its additive offset, sums them,
    /// smooths the total, and applies it to this transform (the "effects" transform that sits
    /// between the pitch pivot and the actual Camera).
    /// <para>
    /// Recommended hierarchy:
    /// <code>
    /// Player            (yaw, FirstPersonMotor)      &lt;- Yaw Source
    ///  └ CameraPivot    (pitch, PlayerLook)          &lt;- Pitch Source
    ///     └ CameraRig   (this + effect components)   &lt;- offsets applied here
    ///        └ Camera
    /// </code>
    /// Runs in <see cref="LateUpdate"/> with a late execution order so it applies after
    /// <c>PlayerLook</c> has set the base aim for the frame — no jitter.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class CameraRig : MonoBehaviour
    {
        [Header("Locomotion Source")]
        [Tooltip("A component implementing ILocomotionState (e.g. FirstPersonMotor). " +
                 "If left empty, the rig searches its parents automatically.")]
        [SerializeField] private MonoBehaviour _locomotionSource;

        [Header("Aim Sources (for look-sway deltas)")]
        [Tooltip("Transform that yaws with the body (the player root). Optional.")]
        [SerializeField] private Transform _yawSource;

        [Tooltip("Transform that pitches with aiming (the camera pivot). Optional.")]
        [SerializeField] private Transform _pitchSource;

        /// <summary>Runtime wiring of the aim sources for look-sway (used by the bootstrap).</summary>
        public void SetAimSources(Transform yawSource, Transform pitchSource)
        {
            _yawSource = yawSource;
            _pitchSource = pitchSource;
        }

        [Header("Smoothing")]
        [Tooltip("Response time for positional offsets. Smaller = snappier, larger = floatier.")]
        [Range(0f, 0.3f)] [SerializeField] private float _positionSmoothTime = 0.05f;

        [Tooltip("Response time for rotational offsets, in seconds.")]
        [Range(0f, 0.3f)] [SerializeField] private float _rotationSmoothTime = 0.05f;

        private ILocomotionState _locomotion;
        private readonly List<ICameraEffect> _effects = new();

        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private Vector3 _smoothedPosition;
        private Vector3 _positionVelocity;
        private Vector3 _smoothedEuler;
        private Vector3 _eulerVelocity;

        private float _previousYaw;
        private float _previousPitch;

        private void Awake()
        {
            // Resolve the locomotion provider: explicit reference first, then a parent search.
            _locomotion = _locomotionSource as ILocomotionState;
            if (_locomotion == null)
            {
                _locomotion = GetComponentInParent<ILocomotionState>();
            }

            if (_locomotion == null)
            {
                Debug.LogError($"[CameraRig] No ILocomotionState found for '{name}'. " +
                               "Assign a FirstPersonMotor (or a parent must contain one).", this);
            }

            // Cache the resting local transform so offsets are applied relative to it.
            _restPosition = transform.localPosition;
            _restRotation = transform.localRotation;

            // Collect every effect on this GameObject. Attach/remove components to change the stack.
            GetComponents(_effects);

            CacheAimAngles();
        }

        private void LateUpdate()
        {
            if (_locomotion == null)
            {
                return;
            }

            // Ignore paused frames so nothing drifts behind menus.
            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            CameraRigState state = BuildState();

            // Accumulate additive contributions from the active effects.
            CameraEffectSample total = CameraEffectSample.Zero;
            for (int i = 0; i < _effects.Count; i++)
            {
                ICameraEffect effect = _effects[i];
                if (effect != null && effect.IsActive)
                {
                    total += effect.Evaluate(dt, in state);
                }
            }

            // Critically-damped smoothing of the summed target, then apply relative to rest.
            _smoothedPosition = Vector3.SmoothDamp(_smoothedPosition, total.PositionOffset, ref _positionVelocity, _positionSmoothTime, Mathf.Infinity, dt);
            _smoothedEuler = SmoothDampEuler(_smoothedEuler, total.EulerOffset, ref _eulerVelocity, _rotationSmoothTime, dt);

            transform.localPosition = _restPosition + _smoothedPosition;
            transform.localRotation = _restRotation * Quaternion.Euler(_smoothedEuler);
        }

        /// <summary>Assembles this frame's snapshot from the locomotion state and aim deltas.</summary>
        private CameraRigState BuildState()
        {
            float yawDelta = 0f;
            float pitchDelta = 0f;

            if (_yawSource != null)
            {
                float yaw = _yawSource.eulerAngles.y;
                yawDelta = Mathf.DeltaAngle(_previousYaw, yaw);
                _previousYaw = yaw;
            }

            if (_pitchSource != null)
            {
                float pitch = _pitchSource.localEulerAngles.x;
                pitchDelta = Mathf.DeltaAngle(_previousPitch, pitch);
                _previousPitch = pitch;
            }

            return new CameraRigState(
                _locomotion.Stance,
                _locomotion.CurrentSpeed,
                _locomotion.NormalizedPlanarSpeed,
                _locomotion.IsGrounded,
                _locomotion.MoveInput,
                yawDelta,
                pitchDelta);
        }

        private void CacheAimAngles()
        {
            if (_yawSource != null)
            {
                _previousYaw = _yawSource.eulerAngles.y;
            }

            if (_pitchSource != null)
            {
                _previousPitch = _pitchSource.localEulerAngles.x;
            }
        }

        /// <summary>Per-axis <see cref="Mathf.SmoothDamp"/> for a Euler vector.</summary>
        private static Vector3 SmoothDampEuler(Vector3 current, Vector3 target, ref Vector3 velocity, float smoothTime, float dt)
        {
            return new Vector3(
                Mathf.SmoothDamp(current.x, target.x, ref velocity.x, smoothTime, Mathf.Infinity, dt),
                Mathf.SmoothDamp(current.y, target.y, ref velocity.y, smoothTime, Mathf.Infinity, dt),
                Mathf.SmoothDamp(current.z, target.z, ref velocity.z, smoothTime, Mathf.Infinity, dt));
        }
    }
}
