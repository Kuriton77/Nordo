using UnityEngine;

namespace Nordo.Rendering
{
    /// <summary>
    /// The single source of truth for Nordo's LOCKED art direction (see docs/ART_DIRECTION.md):
    /// the cold blue-grey palette, the always-on atmospheric fog, and the dim ambient. Reference one
    /// authoritative asset from every scene/atmosphere so the whole game stays visually consistent.
    /// Defaults are pre-loaded with the locked values, so a fresh instance is already compliant.
    /// </summary>
    [CreateAssetMenu(menuName = "Nordo/Rendering/PSX Palette", fileName = "PSXPalette")]
    public sealed class PSXPalette : ScriptableObject
    {
        [Header("Core Cold Palette (rule 5)")]
        public Color Void = Hex(0x0A, 0x0C, 0x10);
        public Color DeepSteel = Hex(0x14, 0x1A, 0x22);
        public Color Slate = Hex(0x23, 0x2B, 0x34);
        public Color ColdGrey = Hex(0x3A, 0x44, 0x4D);
        public Color IceHighlight = Hex(0x6E, 0x7C, 0x86);
        public Color SicklyTeal = Hex(0x2C, 0x4A, 0x47);

        [Header("Accents — rare, deliberate (rule 5)")]
        public Color WarningAmber = Hex(0xC8, 0x79, 0x1E);
        public Color SignalRed = Hex(0x8E, 0x2B, 0x2B);

        [Header("Atmospheric Fog (rule 4)")]
        [Tooltip("Fog colour — cold blue-grey. Also used as the ambient base.")]
        public Color FogColor = Hex(0x39, 0x43, 0x4E);

        [Tooltip("Exponential-squared fog density. Higher = the world drowns closer.")]
        [Range(0.005f, 0.2f)] public float FogDensity = 0.055f;

        [Tooltip("Fog mode. Exponential Squared gives the heavy, soft PSX murk.")]
        public FogMode FogMode = FogMode.ExponentialSquared;

        [Header("Ambient Lighting (rule 7 — keep it dark)")]
        [Tooltip("Flat ambient colour. Deliberately dim so the flashlight and few lights matter.")]
        public Color AmbientColor = Hex(0x1A, 0x20, 0x28);

        [Tooltip("Ambient intensity multiplier. Keep low; rooms should be dark.")]
        [Range(0f, 1.5f)] public float AmbientIntensity = 0.55f;

        /// <summary>Builds an sRGB colour from 8-bit channels.</summary>
        private static Color Hex(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);
    }
}
