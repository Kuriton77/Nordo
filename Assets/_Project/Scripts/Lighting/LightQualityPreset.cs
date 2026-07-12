using UnityEngine;

namespace Nordo.Lighting
{
    /// <summary>
    /// A reusable quality preset applied to a Unity <see cref="Light"/>: shadow type/strength, render
    /// mode, and a volumetric hint. Author Low/Medium/High assets and switch them from a graphics
    /// settings menu (Milestone 11) to scale visual cost — the flashlight applies whichever it's given.
    /// <para>
    /// Per-light shadow-map <b>resolution</b> is deliberately not part of the preset: that knob
    /// (<c>Light.shadowResolution</c>) belongs to the legacy Built-in pipeline and is ignored by URP,
    /// which takes shadow resolution from the pipeline asset
    /// (<c>Assets/_Project/Settings/URP/Nordo_URP_Pipeline</c>). Dropping it keeps this preset
    /// pipeline-correct for Unity 2022.3 + URP and removes a Built-in-only API dependency.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "Nordo/Lighting/Light Quality Preset", fileName = "LightQuality_")]
    public sealed class LightQualityPreset : ScriptableObject
    {
        [Header("Shadows")]
        [Tooltip("Shadow type cast by the light. Hard shadows are the art-direction default.")]
        [SerializeField] private LightShadows _shadows = LightShadows.Hard;

        [Tooltip("Shadow darkness, 0 (none) to 1 (black).")]
        [Range(0f, 1f)] [SerializeField] private float _shadowStrength = 0.85f;

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
            light.renderMode = _renderMode;
        }
    }
}
