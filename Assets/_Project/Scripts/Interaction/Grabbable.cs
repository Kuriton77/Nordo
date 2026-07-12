using UnityEngine;

namespace Nordo.Interaction
{
    /// <summary>
    /// A physics prop the player can pick up and carry (then throw or drop). Interacting with it asks
    /// the player's <see cref="HeldItemController"/> to hold it. Kept deliberately thin — the holding,
    /// carrying and throwing all live in the controller, so a Grabbable is just "a Rigidbody that
    /// knows how to be picked up".
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class Grabbable : InteractableBase
    {
        private Rigidbody _body;

        /// <summary>The prop's rigidbody (its mass is its weight, used for hold limits and throw feel).</summary>
        public Rigidbody Body => _body;

        protected override void Awake()
        {
            base.Awake();
            _body = GetComponent<Rigidbody>();
        }

        /// <inheritdoc />
        public override string GetPrompt(in InteractionContext context) => _promptVerb;

        /// <inheritdoc />
        protected override void OnInteract(in InteractionContext context)
        {
            if (context.InteractorRoot == null)
            {
                return;
            }

            HeldItemController holder = context.InteractorRoot.GetComponentInChildren<HeldItemController>();
            if (holder != null)
            {
                holder.TryHold(this);
            }
        }
    }
}
