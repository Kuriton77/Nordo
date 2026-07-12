using System.Collections.Generic;
using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;
using Nordo.Items;

namespace Nordo.Progression
{
    /// <summary>One stack in the inventory: an item type and how many are held.</summary>
    public readonly struct ItemStack
    {
        public readonly ItemDefinition Item;
        public readonly int Count;
        public ItemStack(ItemDefinition item, int count) { Item = item; Count = count; }
    }

    /// <summary>
    /// The player's inventory. Listens for <see cref="ItemPickedUpEvent"/> to store items, exposes the
    /// Core <see cref="IInventory"/> query/consume surface for puzzle and door logic, and raises change
    /// + message events for the UI. Registered in the <see cref="ServiceLocator"/> so locks and puzzles
    /// can find it without a direct reference.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    [DisallowMultipleComponent]
    public sealed class InventoryService : MonoBehaviour, IInventory
    {
        private readonly Dictionary<string, ItemDefinition> _defsById = new();
        private readonly Dictionary<string, int> _counts = new();

        private void Awake()
        {
            ServiceLocator.Register<IInventory>(this);
        }

        private void OnEnable()
        {
            EventBus<ItemPickedUpEvent>.Subscribe(OnItemPickedUp);
        }

        private void OnDisable()
        {
            EventBus<ItemPickedUpEvent>.Unsubscribe(OnItemPickedUp);
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet(out IInventory current) && ReferenceEquals(current, this))
            {
                ServiceLocator.Unregister<IInventory>();
            }
        }

        private void OnItemPickedUp(ItemPickedUpEvent evt)
        {
            if (evt.Item != null)
            {
                Add(evt.Item, evt.Quantity);
            }
        }

        /// <summary>Adds items and notifies listeners (used by pickups and, directly, by the builder).</summary>
        public void Add(ItemDefinition item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                return;
            }

            _defsById[item.Id] = item;
            _counts.TryGetValue(item.Id, out int current);
            _counts[item.Id] = current + quantity;

            EventBus<InventoryChangedEvent>.Raise(new InventoryChangedEvent());
            EventBus<GameMessageEvent>.Raise(new GameMessageEvent($"Picked up: {item.DisplayName}"));
        }

        /// <inheritdoc />
        public int Count(string itemId)
        {
            return _counts.TryGetValue(itemId, out int c) ? c : 0;
        }

        /// <inheritdoc />
        public bool Has(string itemId, int quantity = 1) => Count(itemId) >= quantity;

        /// <inheritdoc />
        public bool TryRemove(string itemId, int quantity = 1)
        {
            if (quantity <= 0 || !Has(itemId, quantity))
            {
                return false;
            }

            int remaining = _counts[itemId] - quantity;
            if (remaining <= 0)
            {
                _counts.Remove(itemId);
                _defsById.Remove(itemId);
            }
            else
            {
                _counts[itemId] = remaining;
            }

            EventBus<InventoryChangedEvent>.Raise(new InventoryChangedEvent());
            return true;
        }

        /// <summary>Copies the current stacks into <paramref name="buffer"/> for the UI (alloc-free per call).</summary>
        public void GetStacks(List<ItemStack> buffer)
        {
            buffer.Clear();
            foreach (KeyValuePair<string, int> kv in _counts)
            {
                if (_defsById.TryGetValue(kv.Key, out ItemDefinition def))
                {
                    buffer.Add(new ItemStack(def, kv.Value));
                }
            }
        }
    }
}
