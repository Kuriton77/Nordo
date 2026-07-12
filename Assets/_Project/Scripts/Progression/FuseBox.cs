using System;
using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;
using Nordo.Interaction;

namespace Nordo.Progression
{
    /// <summary>
    /// A fuse box with an empty slot. Interacting with a matching fuse in the inventory installs it
    /// (consuming the fuse), powering the box — a prerequisite the generator checks. Without the fuse it
    /// just tells the player what it needs. A classic dependency link in the puzzle chain.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FuseBox : InteractableBase
    {
        [Tooltip("Item id of the fuse this box accepts.")]
        [SerializeField] private string _requiredFuseId = "fuse";

        [SerializeField] private AudioClip _installClip;

        private bool _installed;

        /// <summary>Whether a fuse is installed (i.e. the box is powered).</summary>
        public bool IsPowered => _installed;

        /// <summary>Raised when the fuse is installed.</summary>
        public event Action Installed;

        /// <summary>Sets the accepted fuse id at runtime (used by the level builder).</summary>
        public void SetRequiredFuse(string fuseId) => _requiredFuseId = fuseId;

        /// <inheritdoc />
        public override string GetPrompt(in InteractionContext context)
        {
            if (_installed)
            {
                return "Fuse installed";
            }

            bool hasFuse = ServiceLocator.TryGet(out IInventory inv) && inv != null && inv.Has(_requiredFuseId);
            return hasFuse ? "Insert fuse" : "Fuse slot (empty)";
        }

        /// <inheritdoc />
        protected override void OnInteract(in InteractionContext context)
        {
            if (_installed)
            {
                return;
            }

            if (ServiceLocator.TryGet(out IInventory inv) && inv != null && inv.TryRemove(_requiredFuseId, 1))
            {
                _installed = true;
                if (_installClip != null)
                {
                    AudioSource.PlayClipAtPoint(_installClip, transform.position);
                }

                // A firm mechanical clunk — audible to The Listener.
                EventBus<NoiseEvent>.Raise(new NoiseEvent(
                    new NoiseStimulus(transform.position, 0.3f, 9f, SoundPriority.Minor, NoiseSourceKind.Object)));
                EventBus<GameMessageEvent>.Raise(new GameMessageEvent("You slot the fuse home. It hums with charge."));

                Installed?.Invoke();
            }
            else
            {
                EventBus<GameMessageEvent>.Raise(new GameMessageEvent("The slot is dead — you need a fuse."));
            }
        }
    }
}
