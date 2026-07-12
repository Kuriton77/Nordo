using UnityEngine;
using Nordo.Core;

namespace Nordo.Noise
{
    /// <summary>
    /// Base class for anything that hears. Handles the boilerplate of registering and unregistering
    /// with the <see cref="INoiseService"/> so concrete listeners (the enemy in Milestone 7, or the
    /// debug visualizer here) only implement <see cref="OnHeardNoise"/>. This is the reusable seam
    /// between the world's sounds and any AI that reacts to them.
    /// </summary>
    public abstract class NoiseListenerBase : MonoBehaviour, INoiseListener
    {
        [Header("Hearing")]
        [Tooltip("Absolute hearing radius in metres. Sounds beyond both this and their own range are ignored.")]
        [Min(0f)]
        [SerializeField] protected float _hearingRange = 18f;

        private INoiseService _service;
        private bool _registered;

        /// <inheritdoc />
        public Vector3 Position => transform.position;

        /// <inheritdoc />
        public float HearingRange => _hearingRange;

        /// <inheritdoc />
        public abstract void OnHeardNoise(in NoiseStimulus stimulus, float perceivedLoudness);

        protected virtual void OnEnable()
        {
            TryRegister();
        }

        protected virtual void Start()
        {
            // Fallback in case the service was not yet available during OnEnable
            // (e.g. this listener was instantiated before the NoiseSystem).
            TryRegister();
        }

        protected virtual void OnDisable()
        {
            if (_registered && _service != null)
            {
                _service.UnregisterListener(this);
            }

            _registered = false;
            _service = null;
        }

        private void TryRegister()
        {
            if (_registered)
            {
                return;
            }

            if (ServiceLocator.TryGet(out _service) && _service != null)
            {
                _service.RegisterListener(this);
                _registered = true;
            }
        }
    }
}
