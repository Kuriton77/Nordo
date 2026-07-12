using UnityEngine;

namespace Nordo.Items
{
    /// <summary>The broad role an item plays, used for inventory filtering and gameplay checks.</summary>
    public enum ItemCategory
    {
        Generic = 0,
        Key = 1,
        Tool = 2,
        Consumable = 3,
        Document = 4,
        QuestItem = 5,
        Fuse = 6,
        Battery = 7
    }

    /// <summary>
    /// The authorable definition of an item type — its identity, presentation and gameplay data.
    /// This is the "inventory-ready" foundation: pickups reference a definition, and the Milestone-6
    /// inventory will store stacks of these. Being a <see cref="ScriptableObject"/>, every item is a
    /// shared asset (one source of truth) that designers create without touching code.
    /// </summary>
    [CreateAssetMenu(menuName = "Nordo/Items/Item Definition", fileName = "Item_")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique id used by save data and puzzle logic (e.g. \"key_generator_room\"). " +
                 "Must be unique and should never change once content ships.")]
        [SerializeField] private string _id = "item_id";

        [Tooltip("Player-facing name, e.g. \"Brass Key\".")]
        [SerializeField] private string _displayName = "New Item";

        [Tooltip("Flavour / usage text shown in inventory or on inspection.")]
        [TextArea(2, 5)]
        [SerializeField] private string _description = string.Empty;

        [Tooltip("Icon shown in the inventory grid.")]
        [SerializeField] private Sprite _icon;

        [Header("Classification")]
        [SerializeField] private ItemCategory _category = ItemCategory.Generic;

        [Header("Stacking")]
        [Tooltip("Whether multiple units share one inventory slot.")]
        [SerializeField] private bool _isStackable;

        [Tooltip("Maximum units per stack when stackable.")]
        [Min(1)]
        [SerializeField] private int _maxStack = 1;

        [Header("Physical")]
        [Tooltip("Mass in kilograms — feeds encumbrance and, if dropped, physics/impact noise.")]
        [Min(0f)]
        [SerializeField] private float _weight = 0.5f;

        [Tooltip("Optional prefab spawned when this item is dropped back into the world.")]
        [SerializeField] private GameObject _worldPrefab;

        /// <summary>Stable unique id used by saves and puzzle logic.</summary>
        public string Id => _id;

        /// <summary>Player-facing display name.</summary>
        public string DisplayName => _displayName;

        /// <summary>Flavour / usage description.</summary>
        public string Description => _description;

        /// <summary>Inventory icon.</summary>
        public Sprite Icon => _icon;

        /// <summary>Item role/category.</summary>
        public ItemCategory Category => _category;

        /// <summary>Whether this item stacks in a single slot.</summary>
        public bool IsStackable => _isStackable;

        /// <summary>Maximum stack size (1 when not stackable).</summary>
        public int MaxStack => _isStackable ? Mathf.Max(1, _maxStack) : 1;

        /// <summary>Mass in kilograms.</summary>
        public float Weight => _weight;

        /// <summary>Prefab used when dropping the item into the world.</summary>
        public GameObject WorldPrefab => _worldPrefab;
    }
}
