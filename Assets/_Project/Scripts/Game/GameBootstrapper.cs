using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Game
{
    /// <summary>
    /// The scene composition root. As the project grows this is where persistent services
    /// (noise, save, audio) will be created and registered with the <see cref="ServiceLocator"/>.
    /// <para>
    /// In Milestone 1 it serves a second, practical purpose: it subscribes to the player
    /// locomotion events on the <see cref="EventBus{T}"/> and optionally logs them, giving QA a
    /// zero-UI way to confirm that jumping, landing and stance changes are firing correctly.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [Header("Diagnostics")]
        [Tooltip("Log player locomotion events to the console. Handy while validating Milestone 1; turn off for release.")]
        [SerializeField] private bool _logLocomotionEvents = true;

        private void OnEnable()
        {
            EventBus<PlayerJumpedEvent>.Subscribe(OnPlayerJumped);
            EventBus<PlayerLandedEvent>.Subscribe(OnPlayerLanded);
            EventBus<PlayerStanceChangedEvent>.Subscribe(OnStanceChanged);
        }

        private void OnDisable()
        {
            EventBus<PlayerJumpedEvent>.Unsubscribe(OnPlayerJumped);
            EventBus<PlayerLandedEvent>.Unsubscribe(OnPlayerLanded);
            EventBus<PlayerStanceChangedEvent>.Unsubscribe(OnStanceChanged);
        }

        private void OnPlayerJumped(PlayerJumpedEvent evt)
        {
            if (_logLocomotionEvents)
            {
                Debug.Log($"[Nordo] Player jumped at {evt.Position}.");
            }
        }

        private void OnPlayerLanded(PlayerLandedEvent evt)
        {
            if (_logLocomotionEvents)
            {
                Debug.Log($"[Nordo] Player landed at {evt.Position} (impact speed {evt.FallSpeed:0.0} m/s).");
            }
        }

        private void OnStanceChanged(PlayerStanceChangedEvent evt)
        {
            if (_logLocomotionEvents)
            {
                Debug.Log($"[Nordo] Stance -> {evt.Stance}.");
            }
        }
    }
}
