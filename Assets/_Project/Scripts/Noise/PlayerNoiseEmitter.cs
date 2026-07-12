using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Noise
{
    /// <summary>
    /// Translates the player's presentation events (footsteps, breathing, jumps) into canonical
    /// <see cref="NoiseEvent"/>s. Centralising this here — rather than sprinkling noise calls through
    /// the player/audio code — means <b>all</b> player noise flows through one consistent, tunable
    /// place, and the <see cref="NoiseSystem"/> only ever has to understand a single channel.
    /// <para>
    /// Place it on the player. It is a pure event-to-noise mapper: it reads positions straight from the
    /// events, so it never touches player internals.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerNoiseEmitter : MonoBehaviour
    {
        [Header("Footsteps")]
        [Tooltip("Range (metres) a footstep of loudness 1 carries. Footstep loudness is already scaled " +
                 "by stance and surface upstream, so this is the ceiling for a loud (sprint) step.")]
        [Range(1f, 40f)] [SerializeField] private float _footstepRange = 15f;

        [Header("Breathing")]
        [Tooltip("Breaths quieter than this intensity make no audible noise (calm breathing is silent).")]
        [Range(0f, 1f)] [SerializeField] private float _breathAudibleThreshold = 0.55f;

        [Tooltip("Range (metres) the heaviest breathing carries.")]
        [Range(0.5f, 12f)] [SerializeField] private float _breathRange = 5f;

        [Tooltip("Scales how loud heavy breathing is as a noise, in [0, 1].")]
        [Range(0f, 1f)] [SerializeField] private float _breathLoudnessScale = 0.5f;

        [Header("Jump")]
        [Tooltip("Loudness of the effort/cloth noise made when jumping.")]
        [Range(0f, 1f)] [SerializeField] private float _jumpLoudness = 0.25f;

        [Tooltip("Range (metres) the jump effort noise carries.")]
        [Range(0.5f, 12f)] [SerializeField] private float _jumpRange = 6f;

        private void OnEnable()
        {
            EventBus<FootstepEvent>.Subscribe(OnFootstep);
            EventBus<BreathEvent>.Subscribe(OnBreath);
            EventBus<PlayerJumpedEvent>.Subscribe(OnJumped);
        }

        private void OnDisable()
        {
            EventBus<FootstepEvent>.Unsubscribe(OnFootstep);
            EventBus<BreathEvent>.Unsubscribe(OnBreath);
            EventBus<PlayerJumpedEvent>.Unsubscribe(OnJumped);
        }

        private void OnFootstep(FootstepEvent evt)
        {
            SoundPriority priority = evt.Stance switch
            {
                LocomotionStance.Sprinting => SoundPriority.Alarming,
                LocomotionStance.Walking => SoundPriority.Notable,
                LocomotionStance.Crouching => SoundPriority.Minor,
                _ => SoundPriority.Ambient
            };

            EventBus<NoiseEvent>.Raise(new NoiseEvent(
                new NoiseStimulus(evt.Position, evt.Loudness, _footstepRange, priority, NoiseSourceKind.Footstep)));
        }

        private void OnBreath(BreathEvent evt)
        {
            // Only heavy exhales are audible; calm breathing gives you away to nothing.
            if (!evt.IsExhale || evt.Intensity < _breathAudibleThreshold)
            {
                return;
            }

            float loudness = evt.Intensity * _breathLoudnessScale;
            EventBus<NoiseEvent>.Raise(new NoiseEvent(
                new NoiseStimulus(evt.Position, loudness, _breathRange, SoundPriority.Minor, NoiseSourceKind.Voice)));
        }

        private void OnJumped(PlayerJumpedEvent evt)
        {
            EventBus<NoiseEvent>.Raise(new NoiseEvent(
                new NoiseStimulus(evt.Position, _jumpLoudness, _jumpRange, SoundPriority.Minor, NoiseSourceKind.Generic)));
        }
    }
}
