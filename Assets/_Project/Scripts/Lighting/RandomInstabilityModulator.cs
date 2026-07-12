using UnityEngine;

namespace Nordo.Lighting
{
    /// <summary>
    /// Gives the flashlight a faint, believable electrical imperfection: a constant subtle Perlin
    /// shimmer plus occasional brief dips, as if the contacts are slightly loose. Never fully dark —
    /// this is texture, not a scare (see <see cref="EmergencyFlickerModulator"/> for that).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RandomInstabilityModulator : MonoBehaviour, ILightModulator
    {
        [Header("Enable")]
        [SerializeField] private bool _enabled = true;

        [Header("Constant Shimmer")]
        [Tooltip("Amount of continuous brightness wobble, in [0, 1].")]
        [Range(0f, 0.3f)] [SerializeField] private float _shimmerDepth = 0.05f;

        [Tooltip("Speed of the continuous shimmer.")]
        [Range(0.5f, 20f)] [SerializeField] private float _shimmerSpeed = 6f;

        [Header("Occasional Dips")]
        [Tooltip("Average number of brief dips per second.")]
        [Range(0f, 3f)] [SerializeField] private float _dipsPerSecond = 0.25f;

        [Tooltip("How dark a dip goes, in [0, 1] (0.5 = halves brightness).")]
        [Range(0f, 0.9f)] [SerializeField] private float _dipDepth = 0.4f;

        [Tooltip("How long a dip lasts, in seconds.")]
        [Range(0.02f, 0.4f)] [SerializeField] private float _dipDuration = 0.09f;

        private float _noiseSeed;
        private float _dipTimer;   // remaining time in the current dip
        private float _dipStrength;

        /// <inheritdoc />
        public bool IsActive => _enabled;

        private void Awake()
        {
            _noiseSeed = Random.value * 100f;
        }

        /// <inheritdoc />
        public float Evaluate(float deltaTime, in LightModulatorState state)
        {
            // Continuous shimmer: signed Perlin noise around 1.0.
            float shimmer = 1f - (Mathf.PerlinNoise(_noiseSeed, Time.time * _shimmerSpeed) - 0.5f) * 2f * _shimmerDepth;

            // Occasional dips modelled as a Poisson-ish process using per-frame probability.
            if (_dipTimer > 0f)
            {
                _dipTimer -= deltaTime;
            }
            else if (_dipsPerSecond > 0f && Random.value < _dipsPerSecond * deltaTime)
            {
                _dipTimer = _dipDuration;
                _dipStrength = _dipDepth * Random.Range(0.5f, 1f);
            }

            float dip = _dipTimer > 0f ? 1f - _dipStrength : 1f;

            return Mathf.Clamp01(shimmer * dip);
        }
    }
}
