using UnityEngine;

namespace Nordo.Interaction
{
    /// <summary>
    /// A sliding drawer. Translates a slide transform along a local axis between closed and a set
    /// open distance, reusing all of <see cref="OpenableBase"/>'s animation/audio/noise/lock logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Drawer : OpenableBase
    {
        [Header("Slide")]
        [Tooltip("Transform that slides. If empty, this object's transform is used.")]
        [SerializeField] private Transform _slide;

        [Tooltip("Local direction the drawer slides open (usually forward/-Z toward the player).")]
        [SerializeField] private Vector3 _slideAxis = Vector3.forward;

        [Tooltip("How far the drawer travels when fully open, in metres.")]
        [Range(0.05f, 1.5f)] [SerializeField] private float _openDistance = 0.45f;

        private Vector3 _closedPosition;

        protected override void CacheClosedState()
        {
            if (_slide == null)
            {
                _slide = transform;
            }

            _closedPosition = _slide.localPosition;
        }

        protected override void ApplyOpenAmount(float amount)
        {
            _slide.localPosition = _closedPosition + _slideAxis.normalized * (_openDistance * amount);
        }
    }
}
