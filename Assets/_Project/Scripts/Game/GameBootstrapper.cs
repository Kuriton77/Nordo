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
        [Tooltip("Log player locomotion events (jump/land/stance) to the console. For validating Milestones 1–2.")]
        [SerializeField] private bool _logLocomotionEvents = true;

        [Tooltip("Log footstep events (surface + loudness). Verbose — useful when tuning the footstep system.")]
        [SerializeField] private bool _logFootsteps;

        [Tooltip("Log breath exhale events. Verbose — useful when tuning the breath system.")]
        [SerializeField] private bool _logBreaths;

        private void OnEnable()
        {
            EventBus<PlayerJumpedEvent>.Subscribe(OnPlayerJumped);
            EventBus<PlayerLandedEvent>.Subscribe(OnPlayerLanded);
            EventBus<PlayerStanceChangedEvent>.Subscribe(OnStanceChanged);
            EventBus<FootstepEvent>.Subscribe(OnFootstep);
            EventBus<BreathEvent>.Subscribe(OnBreath);
        }

        private void OnDisable()
        {
            EventBus<PlayerJumpedEvent>.Unsubscribe(OnPlayerJumped);
            EventBus<PlayerLandedEvent>.Unsubscribe(OnPlayerLanded);
            EventBus<PlayerStanceChangedEvent>.Unsubscribe(OnStanceChanged);
            EventBus<FootstepEvent>.Unsubscribe(OnFootstep);
            EventBus<BreathEvent>.Unsubscribe(OnBreath);
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

        private void OnFootstep(FootstepEvent evt)
        {
            if (_logFootsteps)
            {
                Debug.Log($"[Nordo] Footstep on {evt.Surface} ({evt.Stance}) loudness {evt.Loudness:0.00} at {evt.Position}.");
            }
        }

        private void OnBreath(BreathEvent evt)
        {
            if (_logBreaths && evt.IsExhale)
            {
                Debug.Log($"[Nordo] Exhale (intensity {evt.Intensity:0.00}).");
            }
        }
    }
}
