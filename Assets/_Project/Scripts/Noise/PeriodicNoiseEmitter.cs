using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Noise
{
    /// <summary>
    /// Emits a noise at a (jittered) interval while active — the reusable building block for
    /// environmental and ambient sound: a dripping pipe, a creaking beam, a flapping tarp, wind
    /// gusting through a vent. Each emit optionally plays a one-shot clip and always raises a
    /// <see cref="NoiseEvent"/>, so ambient life participates in the same stealth model as everything
    /// else (a listener can even be lured by a distraction that sounds like the environment).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PeriodicNoiseEmitter : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("Minimum seconds between emissions.")]
        [Min(0.05f)] [SerializeField] private float _minInterval = 4f;

        [Tooltip("Maximum seconds between emissions (randomised per cycle for a natural feel).")]
        [Min(0.05f)] [SerializeField] private float _maxInterval = 9f;

        [Tooltip("Start emitting automatically on enable.")]
        [SerializeField] private bool _startActive = true;

        [Header("Noise")]
        [Range(0f, 1f)] [SerializeField] private float _loudness = 0.35f;
        [Range(1f, 40f)] [SerializeField] private float _range = 10f;
        [SerializeField] private SoundPriority _priority = SoundPriority.Ambient;
        [SerializeField] private NoiseSourceKind _kind = NoiseSourceKind.Generic;

        [Header("Audio (optional)")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip[] _clips = new AudioClip[0];
        [SerializeField] private Vector2 _pitchRange = new Vector2(0.95f, 1.05f);

        private bool _active;
        private float _timer;

        /// <summary>Whether the emitter is currently running.</summary>
        public bool IsActive => _active;

        private void OnEnable()
        {
            SetActive(_startActive);
        }

        /// <summary>Starts or stops emission (e.g. turning a machine on, or muting a zone).</summary>
        public void SetActive(bool active)
        {
            _active = active;
            if (active)
            {
                _timer = NextInterval();
            }
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                Emit();
                _timer = NextInterval();
            }
        }

        /// <summary>Emits one noise immediately (also callable on demand).</summary>
        public void Emit()
        {
            EventBus<NoiseEvent>.Raise(new NoiseEvent(
                new NoiseStimulus(transform.position, _loudness, _range, _priority, _kind)));

            PlayClip();
        }

        private void PlayClip()
        {
            if (_audioSource == null || _clips == null || _clips.Length == 0)
            {
                return;
            }

            AudioClip clip = _clips[Random.Range(0, _clips.Length)];
            if (clip != null)
            {
                _audioSource.pitch = Random.Range(_pitchRange.x, _pitchRange.y);
                _audioSource.PlayOneShot(clip);
            }
        }

        private float NextInterval()
        {
            float min = Mathf.Min(_minInterval, _maxInterval);
            float max = Mathf.Max(_minInterval, _maxInterval);
            return Random.Range(min, max);
        }
    }
}
