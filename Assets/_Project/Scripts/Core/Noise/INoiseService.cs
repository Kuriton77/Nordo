namespace Nordo.Core
{
    /// <summary>
    /// The central hub that carries noises from emitters to listeners. Registered in the
    /// <see cref="ServiceLocator"/> so any system can report a noise or (for listeners) subscribe,
    /// without emitters and listeners ever knowing about each other — a pure mediator.
    /// <para>
    /// Emitters typically don't even call this directly: they raise a <c>NoiseEvent</c> on the
    /// <see cref="EventBus{T}"/> and the service translates it. <see cref="ReportNoise"/> is provided
    /// for code that already holds a service reference and wants to skip the bus.
    /// </para>
    /// </summary>
    public interface INoiseService
    {
        /// <summary>Registers a listener to receive perceivable noises. Idempotent.</summary>
        void RegisterListener(INoiseListener listener);

        /// <summary>Removes a previously registered listener. Safe if not registered.</summary>
        void UnregisterListener(INoiseListener listener);

        /// <summary>Propagates a stimulus to all listeners that can perceive it.</summary>
        void ReportNoise(in NoiseStimulus stimulus);
    }
}
