using System;
using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Interaction
{
    /// <summary>
    /// A reusable lock you compose onto <em>any</em> interactable (door, drawer, cabinet, chest).
    /// It holds the locked state and the id of the key that opens it, plays a rattle + emits a small
    /// noise when the player tries a locked object, and fires <see cref="Unlocked"/> when opened.
    /// Because it's a separate component (composition over inheritance), the openable logic stays
    /// lock-agnostic and the same lock works everywhere.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Lockable : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private bool _isLocked = true;

        [Tooltip("Id of the ItemDefinition that unlocks this. Empty = any key / opened only by script.")]
        [SerializeField] private string _requiredKeyId = string.Empty;

        [Tooltip("Prompt shown while locked.")]
        [SerializeField] private string _lockedPrompt = "Locked";

        [Tooltip("Consume the key from the inventory when it opens this lock.")]
        [SerializeField] private bool _consumeKey = true;

        [Header("Feedback")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _lockedRattle;
        [SerializeField] private AudioClip _unlockSound;

        [Header("Locked-Attempt Noise")]
        [Range(0f, 1f)] [SerializeField] private float _rattleLoudness = 0.3f;
        [Range(1f, 30f)] [SerializeField] private float _rattleRange = 8f;

        /// <summary>Raised when the lock transitions to unlocked.</summary>
        public event Action Unlocked;

        /// <summary>Whether the object is currently locked.</summary>
        public bool IsLocked => _isLocked;

        /// <summary>Id of the key required to unlock (empty means no specific key).</summary>
        public string RequiredKeyId => _requiredKeyId;

        /// <summary>Prompt to show while locked.</summary>
        public string LockedPrompt => _lockedPrompt;

        /// <summary>
        /// Attempts to unlock with the given key id. Returns true if the object is now unlocked
        /// (including if it was already). The inventory/use flow (Milestone 6) calls this.
        /// </summary>
        public bool TryUnlock(string keyId)
        {
            if (!_isLocked)
            {
                return true;
            }

            if (string.IsNullOrEmpty(_requiredKeyId) || _requiredKeyId == keyId)
            {
                Unlock();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to open this lock using the player's inventory: if a required key is present it is
        /// (optionally) consumed and the lock opens. Returns true if the object is now unlocked.
        /// Called automatically by <see cref="InteractableBase"/> before reporting "Locked".
        /// </summary>
        public bool TryAutoUnlock()
        {
            if (!_isLocked)
            {
                return true;
            }

            // A lock with no key id is script/puzzle-only and cannot be opened from the inventory.
            if (string.IsNullOrEmpty(_requiredKeyId))
            {
                return false;
            }

            if (!ServiceLocator.TryGet(out IInventory inventory) || inventory == null)
            {
                return false;
            }

            if (!inventory.Has(_requiredKeyId))
            {
                return false;
            }

            if (_consumeKey)
            {
                inventory.TryRemove(_requiredKeyId, 1);
            }

            Unlock();
            return true;
        }

        /// <summary>Configures the lock at runtime (used by the level builder).</summary>
        public void Configure(bool isLocked, string requiredKeyId, bool consumeKey = true)
        {
            _isLocked = isLocked;
            _requiredKeyId = requiredKeyId;
            _consumeKey = consumeKey;
        }

        /// <summary>Unlocks unconditionally (e.g. a puzzle solving itself). Idempotent.</summary>
        public void Unlock()
        {
            if (!_isLocked)
            {
                return;
            }

            _isLocked = false;
            PlayClip(_unlockSound, 1f);
            Unlocked?.Invoke();
        }

        /// <summary>Re-locks the object.</summary>
        public void Lock() => _isLocked = true;

        /// <summary>Called by an interactable when the player tries it while locked: rattle + noise.</summary>
        public void NotifyLocked()
        {
            PlayClip(_lockedRattle, 1f);
            EventBus<NoiseEvent>.Raise(new NoiseEvent(
                new NoiseStimulus(transform.position, _rattleLoudness, _rattleRange, SoundPriority.Minor, NoiseSourceKind.Door)));
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (_audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip, volume);
            }
        }
    }
}
