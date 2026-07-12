namespace Nordo.Lighting
{
    /// <summary>
    /// Immutable per-frame context handed to every <see cref="ILightModulator"/>: the light's on-state,
    /// battery level, and how long it has been on. Modulators stay stateless with respect to where the
    /// data comes from.
    /// </summary>
    public readonly struct LightModulatorState
    {
        /// <summary>Whether the flashlight is currently on.</summary>
        public readonly bool IsOn;

        /// <summary>Battery charge as a 0–1 fraction.</summary>
        public readonly float BatteryFraction;

        /// <summary>Seconds the light has been continuously on.</summary>
        public readonly float TimeOn;

        public LightModulatorState(bool isOn, float batteryFraction, float timeOn)
        {
            IsOn = isOn;
            BatteryFraction = batteryFraction;
            TimeOn = timeOn;
        }
    }

    /// <summary>
    /// A composable brightness modifier for the flashlight — flicker, low-battery instability,
    /// emergency scares. Each returns a multiplier in [0, 1] that the flashlight applies on top of its
    /// base intensity, so any number of modulators stack (the same Open/Closed pattern as the camera
    /// effects). Add a behaviour by attaching a component; the flashlight discovers it automatically.
    /// </summary>
    public interface ILightModulator
    {
        /// <summary>When false, the flashlight ignores this modulator (it contributes a multiplier of 1).</summary>
        bool IsActive { get; }

        /// <summary>Returns this modulator's brightness multiplier in [0, 1] for the current frame.</summary>
        float Evaluate(float deltaTime, in LightModulatorState state);
    }
}
