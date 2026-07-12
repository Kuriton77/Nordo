using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Player
{
    /// <summary>
    /// The cold-breath visual hook. Listens for <see cref="BreathEvent"/>s and, while the player is
    /// in a cold environment, emits a puff of fog from an assigned <see cref="ParticleSystem"/> on
    /// each exhale — sized by the breath's intensity so panting after a sprint fogs the air heavily.
    /// <para>
    /// The "am I cold?" input is exposed as <see cref="ColdEnvironment"/> / <see cref="SetColdEnvironment"/>
    /// so the Milestone-2 build can toggle it, and a later cold-zone/temperature system can drive it
    /// without any change here. Entirely null-safe: with no particle system assigned it simply does
    /// nothing, so it can be added to the player rig before art exists.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ColdBreathVisualizer : MonoBehaviour
    {
        [Header("Visuals")]
        [Tooltip("Particle system that emits the breath fog (usually a child at head height). Optional.")]
        [SerializeField] private ParticleSystem _breathParticles;

        [Tooltip("Particles emitted for the faintest breath.")]
        [Range(1, 40)] [SerializeField] private int _minParticles = 4;

        [Tooltip("Particles emitted for the heaviest breath.")]
        [Range(1, 120)] [SerializeField] private int _maxParticles = 30;

        [Header("Environment")]
        [Tooltip("Whether the player is currently somewhere cold enough for breath to fog. " +
                 "A temperature/cold-zone system will drive this in a later milestone.")]
        [SerializeField] private bool _coldEnvironment = true;

        /// <summary>Whether breath currently fogs. Set by cold-zone logic or gameplay events.</summary>
        public bool ColdEnvironment
        {
            get => _coldEnvironment;
            set => _coldEnvironment = value;
        }

        /// <summary>Hook for external systems (e.g. entering/leaving a warm room) to toggle fogging.</summary>
        public void SetColdEnvironment(bool isCold) => _coldEnvironment = isCold;

        private void OnEnable()
        {
            EventBus<BreathEvent>.Subscribe(OnBreath);
        }

        private void OnDisable()
        {
            EventBus<BreathEvent>.Unsubscribe(OnBreath);
        }

        private void OnBreath(BreathEvent evt)
        {
            // Only exhales fog the air, and only when it's cold.
            if (!evt.IsExhale || !_coldEnvironment || _breathParticles == null)
            {
                return;
            }

            int count = Mathf.RoundToInt(Mathf.Lerp(_minParticles, _maxParticles, Mathf.Clamp01(evt.Intensity)));
            _breathParticles.Emit(count);
        }
    }
}
