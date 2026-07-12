using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;
using Nordo.Items;

namespace Nordo.Interaction
{
    /// <summary>
    /// A battery the player can pick up. On interaction it raises a <see cref="BatteryCollectedEvent"/>
    /// (which the flashlight consumes to recharge) and, if an <see cref="ItemDefinition"/> is assigned,
    /// an <see cref="ItemPickedUpEvent"/> too — so it also lands in the inventory (Milestone 6). This
    /// dual emit is exactly what "integrated with the existing inventory" means: one pickup, both the
    /// immediate recharge and the persistent record.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BatteryPickup : InteractableBase
    {
        [Header("Battery")]
        [Tooltip("Charge units this battery restores to the flashlight.")]
        [Min(0f)] [SerializeField] private float _chargeAmount = 60f;

        [Tooltip("Optional item definition so the battery is also recorded by the inventory.")]
        [SerializeField] private ItemDefinition _item;

        [Header("Feedback")]
        [SerializeField] private AudioClip _pickupClip;
        [Range(0f, 1f)] [SerializeField] private float _pickupVolume = 0.7f;

        [Header("Noise")]
        [Range(0f, 1f)] [SerializeField] private float _pickupNoiseLoudness = 0.12f;
        [Range(1f, 20f)] [SerializeField] private float _pickupNoiseRange = 6f;

        /// <inheritdoc />
        public override string GetPrompt(in InteractionContext context)
        {
            return _item != null ? $"Pick up {_item.DisplayName}" : "Pick up Battery";
        }

        /// <inheritdoc />
        protected override void OnInteract(in InteractionContext context)
        {
            Vector3 position = transform.position;

            // Immediate recharge for the flashlight.
            EventBus<BatteryCollectedEvent>.Raise(new BatteryCollectedEvent(_chargeAmount));

            // Inventory record (if this battery is also a tracked item).
            if (_item != null)
            {
                EventBus<ItemPickedUpEvent>.Raise(new ItemPickedUpEvent(_item, 1, position));
            }

            // A faint noise, like the footstep/pickup emitters.
            EventBus<NoiseEvent>.Raise(new NoiseEvent(
                new NoiseStimulus(position, _pickupNoiseLoudness, _pickupNoiseRange, SoundPriority.Minor, NoiseSourceKind.Object)));

            if (_pickupClip != null)
            {
                AudioSource.PlayClipAtPoint(_pickupClip, position, _pickupVolume);
            }

            Destroy(gameObject);
        }
    }
}
