namespace Nordo.Core.Events
{
    /// <summary>
    /// Raised when the player collects a battery. The flashlight subscribes and recharges by
    /// <see cref="Charge"/>. Emitting an event keeps battery pickups decoupled from the flashlight —
    /// a pickup doesn't need a reference to whatever consumes the charge.
    /// </summary>
    public readonly struct BatteryCollectedEvent : IGameEvent
    {
        /// <summary>Amount of battery charge granted (in the battery's charge units).</summary>
        public readonly float Charge;

        public BatteryCollectedEvent(float charge)
        {
            Charge = charge;
        }
    }

    /// <summary>
    /// Requests a temporary dramatic flicker of the flashlight — a scripted scare, a nearby
    /// electrical surge, an enemy proximity effect (Milestone 7+). The emergency-flicker modulator
    /// listens for this, so any system can trigger the effect without touching the flashlight.
    /// </summary>
    public readonly struct EmergencyFlickerRequestEvent : IGameEvent
    {
        /// <summary>How long the flicker lasts, in seconds.</summary>
        public readonly float Duration;

        /// <summary>How violent the flicker is, in [0, 1].</summary>
        public readonly float Strength;

        public EmergencyFlickerRequestEvent(float duration, float strength)
        {
            Duration = duration;
            Strength = strength;
        }
    }
}
