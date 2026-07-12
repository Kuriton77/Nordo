namespace Nordo.Core.Events
{
    /// <summary>Raised when the inventory contents change; UI refreshes off this (payload-free refresh signal).</summary>
    public readonly struct InventoryChangedEvent : IGameEvent
    {
    }

    /// <summary>Raised when the current objective changes (advanced or completed). UI/logbook react.</summary>
    public readonly struct ObjectiveChangedEvent : IGameEvent
    {
        /// <summary>Current objective id (empty when the chain is complete).</summary>
        public readonly string CurrentId;

        /// <summary>Current objective title.</summary>
        public readonly string CurrentTitle;

        /// <summary>True when the whole objective chain is finished.</summary>
        public readonly bool AllComplete;

        public ObjectiveChangedEvent(string currentId, string currentTitle, bool allComplete)
        {
            CurrentId = currentId;
            CurrentTitle = currentTitle;
            AllComplete = allComplete;
        }
    }

    /// <summary>Raised when a power grid is energised or cut. Powered devices and UI react.</summary>
    public readonly struct PowerStateChangedEvent : IGameEvent
    {
        /// <summary>Identifier of the power grid that changed.</summary>
        public readonly string GridId;

        /// <summary>Whether the grid is now powered.</summary>
        public readonly bool Powered;

        public PowerStateChangedEvent(string gridId, bool powered)
        {
            GridId = gridId;
            Powered = powered;
        }
    }

    /// <summary>Raised when the player reads an environmental note; the logbook records it and the UI shows it.</summary>
    public readonly struct NoteReadEvent : IGameEvent
    {
        public readonly string NoteId;
        public readonly string Title;
        public readonly string Body;

        public NoteReadEvent(string noteId, string title, string body)
        {
            NoteId = noteId;
            Title = title;
            Body = body;
        }
    }

    /// <summary>
    /// A transient, player-facing feedback message ("Power restored.", "You need a fuse."). Any system
    /// can raise one; the HUD shows it as a toast. Decouples feedback from the UI implementation.
    /// </summary>
    public readonly struct GameMessageEvent : IGameEvent
    {
        public readonly string Text;
        public readonly float Duration;

        public GameMessageEvent(string text, float duration = 3f)
        {
            Text = text;
            Duration = duration;
        }
    }
}
