namespace Nordo.Core.Events
{
    /// <summary>
    /// Drives the on-screen interaction prompt. The interactor raises this as the player's focus
    /// changes; a UI presenter subscribes and shows/hides the label. Keeping it an event means the
    /// interaction logic has zero dependency on any particular UI implementation.
    /// </summary>
    public readonly struct InteractionPromptEvent : IGameEvent
    {
        /// <summary>Whether a prompt should currently be shown.</summary>
        public readonly bool Visible;

        /// <summary>The prompt text, e.g. "Open", "Locked", "Pick up Brass Key".</summary>
        public readonly string Text;

        public InteractionPromptEvent(bool visible, string text)
        {
            Visible = visible;
            Text = text;
        }
    }

    /// <summary>
    /// Requests that player control be locked or unlocked. Used by inspection (and later menus and
    /// cutscenes) to freeze look/movement without those systems referencing the player directly —
    /// the motor and camera subscribe and disable themselves. A single, reusable control gate.
    /// </summary>
    public readonly struct ControlLockEvent : IGameEvent
    {
        /// <summary>True to suspend player movement/look, false to restore it.</summary>
        public readonly bool Locked;

        public ControlLockEvent(bool locked)
        {
            Locked = locked;
        }
    }
}
