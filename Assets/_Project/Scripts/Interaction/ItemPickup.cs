using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;
using Nordo.Items;

namespace Nordo.Interaction
{
    /// <summary>
    /// A world item the player can pick up. On interaction it raises an <see cref="ItemPickedUpEvent"/>
    /// (for the future inventory), plays a pickup sound, emits a faint noise, and removes itself from
    /// the world. It carries an <see cref="ItemDefinition"/> so what it grants is pure data.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemPickup : InteractableBase
    {
        [Header("Item")]
        [SerializeField] private ItemDefinition _item;

        [Tooltip("How many units this pickup grants.")]
        [Min(1)] [SerializeField] private int _quantity = 1;

        [Header("Feedback")]
        [SerializeField] private AudioClip _pickupClip;
        [Range(0f, 1f)] [SerializeField] private float _pickupVolume = 0.7f;

        [Header("Noise")]
        [Range(0f, 1f)] [SerializeField] private float _pickupNoiseLoudness = 0.12f;
        [Range(1f, 20f)] [SerializeField] private float _pickupNoiseRange = 6f;

        /// <inheritdoc />
        public override string GetPrompt(in InteractionContext context)
        {
            return _item != null ? $"Pick up {_item.DisplayName}" : "Pick up";
        }

        /// <inheritdoc />
        protected override void OnInteract(in InteractionContext context)
        {
            if (_item == null)
            {
                Debug.LogWarning($"[ItemPickup] '{name}' has no ItemDefinition assigned.", this);
                return;
            }

            Vector3 position = transform.position;

            EventBus<ItemPickedUpEvent>.Raise(new ItemPickedUpEvent(_item, _quantity, position));
            EventBus<NoiseEvent>.Raise(new NoiseEvent(
                new NoiseStimulus(position, _pickupNoiseLoudness, _pickupNoiseRange, SoundPriority.Minor, NoiseSourceKind.Object)));

            // Play the sound detached so it survives this object's destruction.
            if (_pickupClip != null)
            {
                AudioSource.PlayClipAtPoint(_pickupClip, position, _pickupVolume);
            }

            Destroy(gameObject);
        }
    }
}
