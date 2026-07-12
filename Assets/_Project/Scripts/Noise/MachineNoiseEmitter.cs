using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Noise
{
    /// <summary>
    /// A running machine — a generator, a ventilation unit, a transformer — that hums loudly while on.
    /// While running it emits a steady stream of <see cref="NoiseSourceKind.Machine"/> noise (a
    /// constant beacon that masks the player's own sounds nearby, or draws a hunter to it) and loops a
    /// hum clip. Starting it produces a loud one-shot spike (the roar of a generator kicking over).
    /// <para>
    /// Built for the Milestone-8 generator/fuse puzzles: call <see cref="StartMachine"/> /
    /// <see cref="StopMachine"/> from puzzle logic. Reusable and self-contained.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MachineNoiseEmitter : MonoBehaviour
    {
        [Header("State")]
        [Tooltip("Whether the machine is running at scene start.")]
        [SerializeField] private bool _startRunning;

        [Header("Running Hum")]
        [Tooltip("Seconds between steady hum noise pulses while running.")]
        [Min(0.1f)] [SerializeField] private float _humInterval = 0.6f;

        [Range(0f, 1f)] [SerializeField] private float _humLoudness = 0.6f;
        [Range(1f, 60f)] [SerializeField] private float _humRange = 22f;
        [SerializeField] private SoundPriority _humPriority = SoundPriority.Notable;

        [Header("Start Spike")]
        [Tooltip("Loudness of the one-shot noise when the machine starts.")]
        [Range(0f, 1f)] [SerializeField] private float _startLoudness = 0.9f;
        [Range(1f, 60f)] [SerializeField] private float _startRange = 30f;

        [Header("Audio")]
        [Tooltip("Looping hum played while running.")]
        [SerializeField] private AudioSource _loopSource;
        [SerializeField] private AudioClip _startClip;
        [SerializeField] private AudioClip _stopClip;
        [SerializeField] private AudioSource _oneShotSource;

        private bool _running;
        private float _humTimer;

        /// <summary>Whether the machine is currently running.</summary>
        public bool IsRunning => _running;

        private void Start()
        {
            if (_startRunning)
            {
                SetRunning(true, playStartSpike: false);
            }
        }

        /// <summary>Starts the machine (roar + hum). No-op if already running.</summary>
        public void StartMachine() => SetRunning(true, playStartSpike: true);

        /// <summary>Stops the machine. No-op if already stopped.</summary>
        public void StopMachine() => SetRunning(false, playStartSpike: false);

        private void SetRunning(bool running, bool playStartSpike)
        {
            if (running == _running)
            {
                return;
            }

            _running = running;

            if (running)
            {
                _humTimer = _humInterval;
                if (_loopSource != null && !_loopSource.isPlaying)
                {
                    _loopSource.loop = true;
                    _loopSource.Play();
                }

                if (playStartSpike)
                {
                    PlayOneShot(_startClip);
                    EventBus<NoiseEvent>.Raise(new NoiseEvent(
                        new NoiseStimulus(transform.position, _startLoudness, _startRange, SoundPriority.Alarming, NoiseSourceKind.Machine)));
                }
            }
            else
            {
                if (_loopSource != null && _loopSource.isPlaying)
                {
                    _loopSource.Stop();
                }

                PlayOneShot(_stopClip);
            }
        }

        private void Update()
        {
            if (!_running)
            {
                return;
            }

            _humTimer -= Time.deltaTime;
            if (_humTimer <= 0f)
            {
                _humTimer = _humInterval;
                EventBus<NoiseEvent>.Raise(new NoiseEvent(
                    new NoiseStimulus(transform.position, _humLoudness, _humRange, _humPriority, NoiseSourceKind.Machine)));
            }
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (_oneShotSource != null && clip != null)
            {
                _oneShotSource.PlayOneShot(clip);
            }
        }
    }
}
