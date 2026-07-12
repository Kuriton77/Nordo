namespace Nordo.Core
{
    /// <summary>
    /// Marks a component as participating in save/load, exposing a serializable state snapshot.
    /// The full save service arrives in Milestone 9; defining this interface in Core now lets systems
    /// (like the flashlight) implement real capture/restore today, so persistence support is genuine
    /// rather than retrofitted. <typeparamref name="TState"/> should be a small <c>[Serializable]</c>
    /// struct/class.
    /// </summary>
    /// <typeparam name="TState">The serializable state payload for this saveable.</typeparam>
    public interface ISaveable<TState>
    {
        /// <summary>
        /// Stable id identifying this object across sessions (so its state can be matched on load).
        /// Must be unique within a scene and stable across builds.
        /// </summary>
        string SaveId { get; }

        /// <summary>Produces a snapshot of the current state.</summary>
        TState CaptureState();

        /// <summary>Applies a previously captured snapshot.</summary>
        void RestoreState(TState state);
    }
}
