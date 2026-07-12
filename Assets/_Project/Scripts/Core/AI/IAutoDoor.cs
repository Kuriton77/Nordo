namespace Nordo.Core
{
    /// <summary>
    /// A door the AI can operate. Defined in Core so the enemy can push doors open in its path without
    /// depending on the interaction assembly — doors implement this, the enemy consumes it. Locked
    /// doors report <see cref="CanBeOpenedByAI"/> as false, which is what makes a locked room a safe
    /// hiding place from a hunter that would otherwise just walk in.
    /// </summary>
    public interface IAutoDoor
    {
        /// <summary>Whether the door is currently open.</summary>
        bool IsOpen { get; }

        /// <summary>True when the AI is allowed to open it (closed and unlocked).</summary>
        bool CanBeOpenedByAI { get; }

        /// <summary>Opens the door on the AI's behalf (no-op if locked). Emits the normal door noise.</summary>
        void OpenForAI();
    }
}
