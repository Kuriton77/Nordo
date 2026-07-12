using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;
using Nordo.Input;

namespace Nordo.Player
{
    /// <summary>
    /// Drives first-person camera aiming. Yaw rotates the player body (so movement follows the
    /// gaze); pitch rotates only the camera pivot and is clamped to avoid over-rotation.
    /// <para>
    /// Reads look input in <see cref="LateUpdate"/> so the camera settles <em>after</em> all
    /// movement has been applied for the frame, eliminating visual jitter.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerLook : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Shared input asset. Assign the same InputReader used by the rest of the player.")]
        [SerializeField] private InputReader _input;

        [Tooltip("The transform rotated for pitch (up/down). Usually the camera pivot / holder.")]
        [SerializeField] private Transform _cameraPivot;

        [Header("Sensitivity")]
        [Tooltip("Degrees of rotation per unit of look input, horizontal.")]
        [Range(0.01f, 1f)] [SerializeField] private float _horizontalSensitivity = 0.12f;

        [Tooltip("Degrees of rotation per unit of look input, vertical.")]
        [Range(0.01f, 1f)] [SerializeField] private float _verticalSensitivity = 0.12f;

        [Tooltip("Invert the vertical look axis (flight-sim style).")]
        [SerializeField] private bool _invertY;

        [Header("Smoothing")]
        [Tooltip("Seconds over which raw look input is smoothed. 0 = fully raw/instant (competitive feel); " +
                 "small values (~0.03–0.06) remove mouse jitter for a filmic feel. High-DPI mice benefit from a touch.")]
        [Range(0f, 0.2f)] [SerializeField] private float _smoothingTime = 0.02f;

        [Header("Constraints")]
        [Tooltip("Lowest the camera can look (degrees below horizon).")]
        [Range(-90f, 0f)] [SerializeField] private float _minPitch = -85f;

        [Tooltip("Highest the camera can look (degrees above horizon).")]
        [Range(0f, 90f)] [SerializeField] private float _maxPitch = 85f;

        private float _pitch;
        private Vector2 _smoothedLook;
        private Vector2 _smoothVelocity;
        private bool _controlLocked;

        private void OnEnable() => EventBus<ControlLockEvent>.Subscribe(OnControlLock);

        private void OnDisable() => EventBus<ControlLockEvent>.Unsubscribe(OnControlLock);

        private void OnControlLock(ControlLockEvent evt) => _controlLocked = evt.Locked;

        private void Reset()
        {
            // Convenience: default the pivot to the first child camera if present.
            Camera child = GetComponentInChildren<Camera>();
            if (child != null)
            {
                _cameraPivot = child.transform;
            }
        }

        private void LateUpdate()
        {
            if (_input == null || _cameraPivot == null)
            {
                return;
            }

            // Freeze aiming while paused (timeScale 0) or while control is locked (e.g. inspecting
            // an object), so the camera can't drift. Both checks keep PlayerLook decoupled from
            // whoever owns those states.
            if (Time.timeScale == 0f || _controlLocked)
            {
                return;
            }

            // Smooth the raw look delta toward its target to shave off mouse jitter without
            // adding perceptible latency. At _smoothingTime == 0 this passes input straight through.
            Vector2 rawLook = _input.LookInput;
            Vector2 look;
            if (_smoothingTime > 0f)
            {
                _smoothedLook = Vector2.SmoothDamp(_smoothedLook, rawLook, ref _smoothVelocity, _smoothingTime, Mathf.Infinity, Time.unscaledDeltaTime);
                look = _smoothedLook;
            }
            else
            {
                look = rawLook;
            }

            // Yaw: rotate the body around the world-up axis.
            float yawDelta = look.x * _horizontalSensitivity;
            transform.Rotate(Vector3.up, yawDelta, Space.Self);

            // Pitch: accumulate and clamp, then apply to the pivot only.
            float pitchDelta = look.y * _verticalSensitivity * (_invertY ? 1f : -1f);
            _pitch = Mathf.Clamp(_pitch + pitchDelta, _minPitch, _maxPitch);
            _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }
}
