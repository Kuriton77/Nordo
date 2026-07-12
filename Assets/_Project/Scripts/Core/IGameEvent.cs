namespace Nordo.Core
{
    /// <summary>
    /// Marker interface for every event that travels through <see cref="EventBus{T}"/>.
    /// <para>
    /// Constraining the bus to <c>struct, IGameEvent</c> gives us two guarantees:
    /// (1) events are value types, so raising one never allocates on the managed heap
    /// (critical for GC-free hot paths), and (2) only intentional event types flow through
    /// the bus, which keeps the system's surface discoverable and self-documenting.
    /// </para>
    /// </summary>
    public interface IGameEvent
    {
    }
}
