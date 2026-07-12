using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Lighting
{
    /// <summary>
    /// A violent, scripted flicker for scares and set-pieces: full strobing that can briefly kill the
    /// beam entirely. Triggered on demand — call <see cref="Trigger"/> directly, or raise an
    /// <see cref="EmergencyFlickerRequestEvent"/> from anywhere (a trigger volume, the enemy in M7).
    /// Self-contained: it listens for the event itself, so no wiring is needed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmergencyFlickerModulator : MonoBehaviour, ILightModulator
    {
        [Header("Flicker")]
        [Tooltip("Flicker frequency (state changes per second) during an emergency.")]
        [Range(4f, 40f)] [SerializeField] private float _frequency = 18f;

        [Tooltip("Chance each flicker step that the beam drops to near-black (vs. a partial dip).")]
        [Range(0f, 1f)] [SerializeField] private float _blackoutChance = 0.4f;

        private float _timeRemaining;
        private float _strength;
        private float _stepTimer;
        private float _currentMultiplier = 1f;

        /// <summary>True while an emergency flicker is playing.</summary>
        public bool IsActive => _timeRemaining > 0f;

        private void OnEnable()
        {
            EventBus<EmergencyFlickerRequestEvent>.Subscribe(OnRequest);
        }

        private void OnDisable()
        {
            EventBus<EmergencyFlickerRequestEvent>.Unsubscribe(OnRequest);
        }

        private void OnRequest(EmergencyFlickerRequestEvent evt) => Trigger(evt.Duration, evt.Strength);

        /// <summary>Starts (or extends) an emergency flicker.</summary>
        public void Trigger(float duration, float strength)
        {
            _timeRemaining = Mathf.Max(_timeRemaining, duration);
            _strength = Mathf.Clamp01(Mathf.Max(_strength, strength));
        }

        /// <inheritdoc />
        public float Evaluate(float deltaTime, in LightModulatorState state)
        {
            if (_timeRemaining <= 0f)
            {
                _currentMultiplier = 1f;
                return 1f;
            }

            _timeRemaining -= deltaTime;

            // Re-roll the flicker value at a fixed frequency rather than every frame, for a
            // crunchy, stroboscopic look that is frame-rate independent.
            _stepTimer -= deltaTime;
            if (_stepTimer <= 0f)
            {
                _stepTimer = 1f / _frequency;
                bool blackout = Random.value < _blackoutChance;
                float dip = blackout ? Random.Range(0f, 0.08f) : Random.Range(0.5f, 1f);
                _currentMultiplier = Mathf.Lerp(1f, dip, _strength);
            }

            return _currentMultiplier;
        }
    }
}
