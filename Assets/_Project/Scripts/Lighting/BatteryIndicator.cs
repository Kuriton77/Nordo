using UnityEngine;

namespace Nordo.Lighting
{
    /// <summary>
    /// A diegetic battery gauge: a small emissive element on the flashlight body (an LED strip, a dial
    /// glow) that changes colour with the remaining charge — green → amber → red — and blinks when the
    /// battery is low. No screen HUD; the player reads their power straight off the torch, which keeps
    /// them looking at the world.
    /// <para>
    /// Drives emission through a <see cref="MaterialPropertyBlock"/> (batch-friendly) and can also
    /// modulate an optional indicator <see cref="Light"/> (an actual glowing LED).
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BatteryIndicator : MonoBehaviour
    {
        [Header("Source")]
        [Tooltip("Flashlight whose battery this indicator reflects. If empty, one on a parent is used.")]
        [SerializeField] private FlashlightController _flashlight;

        [Header("Emissive Target")]
        [Tooltip("Renderer of the indicator element (its material should have Emission enabled).")]
        [SerializeField] private Renderer _indicatorRenderer;

        [Tooltip("Colour by charge fraction (0 = empty on the left, 1 = full on the right).")]
        [SerializeField] private Gradient _chargeGradient = CreateDefaultGradient();

        [Tooltip("Emission brightness multiplier.")]
        [Range(0f, 5f)] [SerializeField] private float _emissionStrength = 1.5f;

        [Header("Optional LED Light")]
        [SerializeField] private Light _indicatorLight;

        [Header("Low-Battery Blink")]
        [Range(0f, 1f)] [SerializeField] private float _lowThreshold = 0.2f;
        [Range(0.5f, 10f)] [SerializeField] private float _blinkSpeed = 4f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _block;

        private void Awake()
        {
            if (_flashlight == null)
            {
                _flashlight = GetComponentInParent<FlashlightController>();
            }

            _block = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            if (_flashlight == null)
            {
                return;
            }

            float fraction = _flashlight.BatteryFraction;
            Color color = _chargeGradient.Evaluate(fraction);

            // Blink when low: modulate brightness with a sine so it pulses.
            float brightness = _emissionStrength;
            if (fraction <= _lowThreshold)
            {
                float blink = 0.5f + 0.5f * Mathf.Sin(Time.time * _blinkSpeed * Mathf.PI);
                brightness *= Mathf.Lerp(0.15f, 1f, blink);
            }

            Color emission = color * brightness;

            if (_indicatorRenderer != null)
            {
                _indicatorRenderer.GetPropertyBlock(_block);
                _block.SetColor(EmissionColorId, emission);
                _indicatorRenderer.SetPropertyBlock(_block);
            }

            if (_indicatorLight != null)
            {
                _indicatorLight.color = color;
                _indicatorLight.intensity = brightness;
            }
        }

        private static Gradient CreateDefaultGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.9f, 0.1f, 0.05f), 0f),   // empty  → red
                    new GradientColorKey(new Color(0.95f, 0.6f, 0.05f), 0.35f), // low   → amber
                    new GradientColorKey(new Color(0.2f, 0.9f, 0.2f), 1f)      // full  → green
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }
    }
}
