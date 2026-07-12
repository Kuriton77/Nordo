using UnityEngine;

namespace Nordo.Lighting
{
    /// <summary>
    /// Makes a dying battery <em>feel</em> dying: below a threshold the beam both dims and flickers,
    /// and the effect intensifies the closer the battery gets to empty. This is the readable warning
    /// that turns "manage your battery" into visible, tense feedback.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LowBatteryModulator : MonoBehaviour, ILightModulator
    {
        [Header("Enable")]
        [SerializeField] private bool _enabled = true;

        [Header("Onset")]
        [Tooltip("Battery fraction at/below which the effect starts (match FlashlightSettings for consistency).")]
        [Range(0f, 1f)] [SerializeField] private float _threshold = 0.2f;

        [Header("Effect at Empty")]
        [Tooltip("Maximum steady dimming when the battery is nearly empty, in [0, 1].")]
        [Range(0f, 0.8f)] [SerializeField] private float _maxDim = 0.4f;

        [Tooltip("Maximum flicker depth when nearly empty, in [0, 1].")]
        [Range(0f, 0.8f)] [SerializeField] private float _maxFlicker = 0.5f;

        [Tooltip("Flicker speed when nearly empty.")]
        [Range(1f, 30f)] [SerializeField] private float _flickerSpeed = 14f;

        private float _noiseSeed;

        /// <inheritdoc />
        public bool IsActive => _enabled;

        private void Awake()
        {
            _noiseSeed = Random.value * 100f + 25f;
        }

        /// <inheritdoc />
        public float Evaluate(float deltaTime, in LightModulatorState state)
        {
            if (state.BatteryFraction >= _threshold || _threshold <= 0f)
            {
                return 1f;
            }

            // 0 at the threshold, 1 at empty — how "deep into low battery" we are.
            float severity = Mathf.Clamp01(1f - state.BatteryFraction / _threshold);

            float dim = 1f - _maxDim * severity;
            float flicker = 1f - (Mathf.PerlinNoise(_noiseSeed, Time.time * _flickerSpeed) * _maxFlicker * severity);

            return Mathf.Clamp01(dim * flicker);
        }
    }
}
