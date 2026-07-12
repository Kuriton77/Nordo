using UnityEngine;

namespace Nordo.Interaction
{
    /// <summary>
    /// The contract every interactable object fulfils — doors, drawers, pickups, switches, anything
    /// the player can look at and use. The <see cref="PlayerInteractor"/> depends only on this
    /// interface, so new interactable kinds are added purely by implementing it (Open/Closed): no
    /// interactor changes, ever. This is the single most important abstraction in the game's
    /// moment-to-moment interaction loop.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>The interactable's transform (for distance checks, prompts, effects).</summary>
        Transform Transform { get; }

        /// <summary>Whether the object can be interacted with right now (e.g. not mid-animation).</summary>
        bool CanInteract(in InteractionContext context);

        /// <summary>The verb/label to show while focused, e.g. "Open", "Locked", "Pick up Key".</summary>
        string GetPrompt(in InteractionContext context);

        /// <summary>Called when the player's crosshair begins focusing this object (drive highlight).</summary>
        void OnFocusEnter(in InteractionContext context);

        /// <summary>Called when focus leaves this object (clear highlight).</summary>
        void OnFocusExit(in InteractionContext context);

        /// <summary>Performs the interaction.</summary>
        void Interact(in InteractionContext context);
    }
}
