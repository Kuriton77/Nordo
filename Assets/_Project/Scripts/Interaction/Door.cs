using UnityEngine;

namespace Nordo.Interaction
{
    /// <summary>
    /// A hinged door. Rotates a hinge transform around a local axis between closed (0°) and the
    /// configured open angle, with all the smooth-animation, audio, noise and lock behaviour
    /// inherited from <see cref="OpenableBase"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Door : OpenableBase
    {
        [Header("Hinge")]
        [Tooltip("Transform that rotates. If empty, this object's transform is used.")]
        [SerializeField] private Transform _hinge;

        [Tooltip("Local axis to rotate about (usually up for a standard door).")]
        [SerializeField] private Vector3 _hingeAxis = Vector3.up;

        [Tooltip("Angle in degrees the door swings when fully open. Negative swings the other way.")]
        [Range(-170f, 170f)] [SerializeField] private float _openAngle = 95f;

        private Quaternion _closedRotation;

        protected override void CacheClosedState()
        {
            if (_hinge == null)
            {
                _hinge = transform;
            }

            _closedRotation = _hinge.localRotation;
        }

        protected override void ApplyOpenAmount(float amount)
        {
            _hinge.localRotation = _closedRotation * Quaternion.AngleAxis(_openAngle * amount, _hingeAxis);
        }
    }
}
