using UnityEngine;

namespace Nordo.Rendering
{
    /// <summary>
    /// Applies the locked art direction's fog and ambient (from a <see cref="PSXPalette"/>) to the
    /// scene at load. <b>Every playable scene must have one of these</b> — it is what guarantees rules
    /// 4 (heavy fog), 5 (cold palette) and 7 (dark, limited light) hold consistently everywhere, rather
    /// than depending on someone remembering to tick the fog checkbox.
    /// <para>
    /// If no palette is assigned it creates the compliant default, so a scene is never accidentally
    /// un-fogged.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-100)] // apply before most gameplay starts
    [DisallowMultipleComponent]
    public sealed class SceneAtmosphere : MonoBehaviour
    {
        [Tooltip("The project's authoritative palette/fog/ambient. If empty, the locked default is used.")]
        [SerializeField] private PSXPalette _palette;

        [Tooltip("Re-apply every frame (useful while tuning in the editor). Off is fine at runtime.")]
        [SerializeField] private bool _applyContinuously;

        private void Awake() => Apply();

        private void OnValidate()
        {
            // Live-preview while editing values in the inspector.
            if (Application.isPlaying)
            {
                Apply();
            }
        }

        private void Update()
        {
            if (_applyContinuously)
            {
                Apply();
            }
        }

        /// <summary>Pushes the palette's fog and ambient into Unity's render settings.</summary>
        public void Apply()
        {
            if (_palette == null)
            {
                _palette = ScriptableObject.CreateInstance<PSXPalette>();
            }

            // Heavy atmospheric fog (rules 4, 5).
            RenderSettings.fog = true;
            RenderSettings.fogMode = _palette.FogMode;
            RenderSettings.fogColor = _palette.FogColor;
            RenderSettings.fogDensity = _palette.FogDensity;

            // Dim, flat, cold ambient (rules 5, 7).
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = _palette.AmbientColor * _palette.AmbientIntensity;
        }

        /// <summary>The palette currently in use (never null after Awake).</summary>
        public PSXPalette Palette => _palette;
    }
}
