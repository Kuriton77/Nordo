using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Noise
{
    /// <summary>
    /// Turns physical impacts into noise (and optional sound). Attach it to any <see cref="Rigidbody"/>
    /// prop — a thrown bottle, a knocked-over chair — and every hard collision emits a
    /// <see cref="NoiseEvent"/> scaled by the impact's energy. This is the "physics-driven noise
    /// emission" pillar: heavier, faster impacts are louder and carry further, so throwing a heavy
    /// object is a powerful (and risky) distraction.
    /// <para>
    /// Fully reusable and self-contained: no references to the AI, the player, or the interaction
    /// system. It simply reports what it feels.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class ImpactNoiseEmitter : MonoBehaviour
    {
        [Header("Sensitivity")]
        [Tooltip("Impacts below this collision impulse (kg·m/s) are ignored (settling, light brushes).")]
        [Min(0f)] [SerializeField] private float _minImpulse = 0.4f;

        [Tooltip("Collision impulse (kg·m/s) that produces a full-loudness (1.0) noise. Higher = less sensitive.")]
        [Min(0.1f)] [SerializeField] private float _impulseForFullLoudness = 8f;

        [Tooltip("Minimum seconds between emitted impacts, to avoid spamming on rolls/scrapes.")]
        [Range(0.02f, 1f)] [SerializeField] private float _minInterval = 0.12f;

        [Header("Propagation")]
        [Tooltip("Range (metres) a full-loudness impact carries before attenuation.")]
        [Range(1f, 40f)] [SerializeField] private float _maxRange = 18f;

        [Header("Audio (optional)")]
        [Tooltip("Impact clips; one is chosen per hit. Leave empty for silent (noise-only) props.")]
        [SerializeField] private AudioClip[] _impactClips = new AudioClip[0];

        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private Vector2 _pitchRange = new Vector2(0.9f, 1.1f);

        private float _lastEmitTime = -999f;

        private void OnCollisionEnter(Collision collision)
        {
            // collision.impulse already folds in both masses and the relative velocity, so it's the
            // natural measure of "how hard did this hit" — no manual mass/velocity maths needed.
            float impulse = collision.impulse.magnitude;
            if (impulse < _minImpulse || Time.time - _lastEmitTime < _minInterval)
            {
                return;
            }

            _lastEmitTime = Time.time;

            float loudness = Mathf.Clamp01(impulse / _impulseForFullLoudness);
            SoundPriority priority = loudness > 0.6f ? SoundPriority.Alarming
                                   : loudness > 0.3f ? SoundPriority.Notable
                                   : SoundPriority.Minor;

            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;

            EventBus<NoiseEvent>.Raise(new NoiseEvent(
                new NoiseStimulus(point, loudness, _maxRange, priority, NoiseSourceKind.Impact)));

            PlayImpactSound(loudness);
        }

        private void PlayImpactSound(float loudness)
        {
            if (_audioSource == null || _impactClips == null || _impactClips.Length == 0)
            {
                return;
            }

            AudioClip clip = _impactClips[Random.Range(0, _impactClips.Length)];
            if (clip == null)
            {
                return;
            }

            _audioSource.pitch = Random.Range(_pitchRange.x, _pitchRange.y);
            _audioSource.PlayOneShot(clip, Mathf.Clamp01(0.25f + 0.75f * loudness));
        }
    }
}
