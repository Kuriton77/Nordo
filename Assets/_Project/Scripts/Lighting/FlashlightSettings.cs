using UnityEngine;

namespace Nordo.Lighting
{
    /// <summary>
    /// Designer-authored tuning for the flashlight beam and its power draw. Kept as a
    /// <see cref="ScriptableObject"/> so different flashlights/difficulties reference distinct presets
    /// and the beam can be balanced without code changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Nordo/Lighting/Flashlight Settings", fileName = "FlashlightSettings")]
    public sealed class FlashlightSettings : ScriptableObject
    {
        [Header("Beam")]
        [Tooltip("Base beam brightness (Unity Light intensity) before flicker modulation.")]
        [Range(0.1f, 20f)] public float Intensity = 4.5f;

        [Tooltip("How far the beam reaches, in metres.")]
        [Range(1f, 60f)] public float Range = 22f;

        [Tooltip("Outer cone angle of the spotlight, in degrees.")]
        [Range(5f, 120f)] public float SpotAngle = 45f;

        [Tooltip("Inner cone angle (the fully-bright core), in degrees. Should be < Spot Angle.")]
        [Range(0f, 120f)] public float InnerSpotAngle = 28f;

        [Tooltip("Beam colour — a slightly warm white reads as an incandescent torch.")]
        public Color Color = new Color(1f, 0.96f, 0.88f);

        [Header("Power")]
        [Tooltip("Charge units drained per second while the light is on.")]
        [Range(0.1f, 20f)] public float DrainPerSecond = 2f;

        [Tooltip("Battery fraction (0–1) below which low-battery behaviour (dimming warning) begins.")]
        [Range(0f, 1f)] public float LowBatteryThreshold = 0.2f;

        [Header("Warning")]
        [Tooltip("Seconds between low-battery warning beeps.")]
        [Range(0.5f, 10f)] public float WarningBeepInterval = 3f;
    }
}
