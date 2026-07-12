namespace Nordo.Core
{
    /// <summary>
    /// The query/consume surface of the player's inventory, keyed by item id. Interaction and puzzle
    /// code (locked doors checking for a key, a fuse box checking for a fuse) depends on this Core
    /// abstraction, never on the concrete inventory or on item assets — so those systems stay
    /// decoupled and the inventory implementation can change freely. Adding items is the concrete
    /// service's job (it happens by listening to pickup events), so it is intentionally not here.
    /// </summary>
    public interface IInventory
    {
        /// <summary>How many of an item the player currently holds.</summary>
        int Count(string itemId);

        /// <summary>Whether the player holds at least <paramref name="quantity"/> of the item.</summary>
        bool Has(string itemId, int quantity = 1);

        /// <summary>Removes up to <paramref name="quantity"/>; returns true only if it all came out.</summary>
        bool TryRemove(string itemId, int quantity = 1);
    }
}
