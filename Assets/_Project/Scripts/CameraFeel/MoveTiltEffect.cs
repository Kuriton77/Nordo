using UnityEngine;
using Nordo.Core.Events;

namespace Nordo.CameraFeel
{
    /// <summary>
    /// Leans the camera into motion. Strafing rolls the view slightly (like shifting weight into a
    /// side-step), and sprinting adds a small forward pitch so speed feels committed. Both targets
    /// are smoothed internally so onset/recovery are gradual regardless of the rig's own smoothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MoveTiltEffect : MonoBehaviour, ICameraEffect
    {
        [Header("Enable")]
        [SerializeField] private bool _enabled = true;

        [Header("Strafe Roll")]
        [Tooltip("Maximum roll when strafing fully left/right, in degrees.")]
        [Range(0f, 8f)] [SerializeField] private float _maxStrafeRoll = 2.2f;

        [Header("Sprint Lean")]
        [Tooltip("Forward pitch added while sprinting, in degrees.")]
        [Range(0f, 6f)] [SerializeField] private float _sprintForwardLean = 1.6f;

        [Header("Response")]
        [Tooltip("How quickly the tilt eases toward its target (per second).")]
        [Range(1f, 20f)] [SerializeField] private float _responsiveness = 7f;

        private float _currentRoll;
        private float _currentPitch;

        /// <inheritdoc />
        public bool IsActive => _enabled;

        /// <inheritdoc />
        public CameraEffectSample Evaluate(float deltaTime, in CameraRigState state)
        {
            // Only tilt while grounded and moving; otherwise ease back to neutral.
            bool moving = state.IsGrounded && state.PlanarSpeed > 0.1f;

            float targetRoll = moving ? -Mathf.Clamp(state.MoveInput.x, -1f, 1f) * _maxStrafeRoll : 0f;
            float targetPitch = (moving && state.Stance == LocomotionStance.Sprinting) ? _sprintForwardLean : 0f;

            float t = 1f - Mathf.Exp(-_responsiveness * deltaTime); // frame-rate-independent lerp
            _currentRoll = Mathf.Lerp(_currentRoll, targetRoll, t);
            _currentPitch = Mathf.Lerp(_currentPitch, targetPitch, t);

            return new CameraEffectSample(Vector3.zero, new Vector3(_currentPitch, 0f, _currentRoll));
        }
    }
}
