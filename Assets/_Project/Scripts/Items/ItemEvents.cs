using UnityEngine;
using Nordo.Core;

namespace Nordo.Items
{
    /// <summary>
    /// Raised when the player collects an item from the world. The Milestone-6 inventory subscribes
    /// to add it to a stack; audio/UI can react too. Emitting an event (rather than the pickup
    /// pushing into the inventory directly) keeps pickups usable before the inventory exists and
    /// decoupled from how storage is implemented.
    /// </summary>
    public readonly struct ItemPickedUpEvent : IGameEvent
    {
        /// <summary>The definition of the collected item.</summary>
        public readonly ItemDefinition Item;

        /// <summary>How many units were collected.</summary>
        public readonly int Quantity;

        /// <summary>Where the item was collected from.</summary>
        public readonly Vector3 Position;

        public ItemPickedUpEvent(ItemDefinition item, int quantity, Vector3 position)
        {
            Item = item;
            Quantity = quantity;
            Position = position;
        }
    }
}
