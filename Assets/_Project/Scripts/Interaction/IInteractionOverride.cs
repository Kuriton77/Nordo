namespace Nordo.Interaction
{
    /// <summary>
    /// A player-side system that can temporarily take over the interact button — holding a thrown
    /// object, or inspecting a prop. The <see cref="PlayerInteractor"/> consults all overrides
    /// before doing normal world interaction, so behaviours like "while holding, interact throws"
    /// compose cleanly without the interactor knowing about them (Open/Closed again).
    /// </summary>
    public interface IInteractionOverride
    {
        /// <summary>
        /// While true, normal world focus/highlight/prompt is suppressed and this override owns the
        /// prompt (e.g. inspecting an object hides "Open door" prompts behind it).
        /// </summary>
        bool IsBlocking { get; }

        /// <summary>The prompt to display while <see cref="IsBlocking"/> is true (may be empty).</summary>
        string BlockingPrompt { get; }

        /// <summary>
        /// Offered the interact press. Returns true if it consumed the input (stopping the interactor
        /// from also interacting with whatever is under the crosshair).
        /// </summary>
        bool HandleInteract(in InteractionContext context);
    }
}
