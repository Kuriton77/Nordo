using UnityEngine;

namespace Nordo.Lighting
{
    /// <summary>
    /// A reusable quality preset applied to a Unity <see cref="Light"/>: shadow type/strength/resolution,
    /// render mode, and a volumetric hint. Author Low/Medium/High assets and switch them from a graphics
    /// settings menu (Milestone 11) to scale visual cost — the flashlight applies whichever it's given.
    /// This keeps "light quality presets" as data, not branching code.
    /// </summary>
    [CreateAssetMenu(menuName = "Nordo/Lighting/Light Quality Preset", fileName = "LightQuality_")]
    public sealed class LightQualityPreset : ScriptableObject
    {
        [Header("Shadows")]
        [Tooltip("Shadow type cast by the light.")]
        [SerializeField] private LightShadows _shadows = LightShadows.Soft;

        [Tooltip("Shadow darkness, 0 (none) to 1 (black).")]
        [Range(0f, 1f)] [SerializeField] private float _shadowStrength = 0.85f;

        [Tooltip("Per-light shadow map resolution.")]
        [SerializeField] private LightShadowResolution _shadowResolution = LightShadowResolution.FromQualitySettings;

        [Header("Rendering")]
        [Tooltip("Pixel (Important) vs. vertex/baked (Auto) — Important gives per-pixel shadows and cone.")]
        [SerializeField] private LightRenderMode _renderMode = LightRenderMode.Auto;

        [Header("Volumetric")]
        [Tooltip("Hint for a VolumetricBeam whether to show the light shaft at this quality level.")]
        [SerializeField] private bool _enableVolumetric = true;

        /// <summary>Whether volumetric light shafts should be shown at this quality.</summary>
        public bool EnableVolumetric => _enableVolumetric;

        /// <summary>Applies this preset's settings to the given light. Safe with a null light.</summary>
        public void Apply(Light light)
        {
            if (light == null)
            {
                return;
            }

            light.shadows = _shadows;
            light.shadowStrength = _shadowStrength;
            light.shadowResolution = _shadowResolution;
            light.renderMode = _renderMode;
        }
    }
}
