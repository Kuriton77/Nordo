using UnityEngine;
using Nordo.Core;

namespace Nordo.Enemy
{
    /// <summary>
    /// The Listener's voice: a looping presence sound (breathing/rasp) whose volume and pitch rise
    /// with aggression, plus one-shot stingers on key transitions (alerted, lunging into a chase,
    /// striking, losing you). These are the player's <em>only</em> read on a creature they cannot see —
    /// so the audio is the interface, not decoration.
    /// <para>
    /// Driven by the controller through simple method calls; fully null-safe so it can be added
    /// incrementally as clips are authored.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ListenerAudio : MonoBehaviour
    {
        [Header("Presence Loop")]
        [Tooltip("Looping breathing/rasp source that always plays; its volume/pitch scale with aggression.")]
        [SerializeField] private AudioSource _presenceLoop;

        [Range(0f, 1f)] [SerializeField] private float _calmVolume = 0.25f;
        [Range(0f, 1f)] [SerializeField] private float _huntVolume = 0.8f;
        [Range(0.5f, 1.5f)] [SerializeField] private float _calmPitch = 0.9f;
        [Range(0.5f, 2f)] [SerializeField] private float _huntPitch = 1.15f;

        [Header("Stingers")]
        [SerializeField] private AudioSource _oneShotSource;
        [SerializeField] private AudioClip _alertedClip;   // patrol/return -> investigate/search
        [SerializeField] private AudioClip _chaseClip;     // -> chase
        [SerializeField] private AudioClip _attackClip;    // strike
        [SerializeField] private AudioClip _lostClip;      // chase -> search/return

        private float _aggression; // 0 calm .. 1 hunting

        /// <summary>Runtime audio wiring (used by the level builder).</summary>
        public void ConfigureAudio(AudioSource presenceLoop, AudioSource oneShotSource,
            AudioClip alerted, AudioClip chase, AudioClip attack, AudioClip lost)
        {
            _presenceLoop = presenceLoop;
            _oneShotSource = oneShotSource;
            _alertedClip = alerted;
            _chaseClip = chase;
            _attackClip = attack;
            _lostClip = lost;
        }

        private void Start()
        {
            if (_presenceLoop != null)
            {
                _presenceLoop.loop = true;
                if (!_presenceLoop.isPlaying)
                {
                    _presenceLoop.Play();
                }
            }
        }

        /// <summary>Continuously updates the presence loop toward a target aggression (0–1).</summary>
        public void SetAggression(float target)
        {
            _aggression = Mathf.MoveTowards(_aggression, Mathf.Clamp01(target), Time.deltaTime * 1.5f);
            if (_presenceLoop != null)
            {
                _presenceLoop.volume = Mathf.Lerp(_calmVolume, _huntVolume, _aggression);
                _presenceLoop.pitch = Mathf.Lerp(_calmPitch, _huntPitch, _aggression);
            }
        }

        /// <summary>Plays the appropriate stinger for a state transition.</summary>
        public void OnStateEntered(ListenerStateId state, ListenerStateId previous)
        {
            switch (state)
            {
                case ListenerStateId.Investigate when previous == ListenerStateId.Patrol || previous == ListenerStateId.Return:
                    PlayOneShot(_alertedClip);
                    break;
                case ListenerStateId.Chase:
                    PlayOneShot(_chaseClip);
                    break;
                case ListenerStateId.Attack:
                    PlayOneShot(_attackClip);
                    break;
                case ListenerStateId.Search when previous == ListenerStateId.Chase:
                    PlayOneShot(_lostClip);
                    break;
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
