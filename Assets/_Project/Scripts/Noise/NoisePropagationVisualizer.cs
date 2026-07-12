using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Noise
{
    /// <summary>
    /// A development visualizer that makes noise <em>propagation</em> visible in the Scene view: every
    /// <see cref="NoiseEvent"/> is drawn as an expanding wavefront ring (growing to its effective
    /// range), plus a static range sphere, coloured by priority and fading over a short lifetime.
    /// Where <c>DebugNoiseListener</c> shows what a listener <i>heard</i>, this shows every sound the
    /// world <i>made</i> — invaluable for tuning ranges and spotting runaway emitters.
    /// <para>
    /// Records are kept in a fixed ring buffer, so it never allocates at runtime.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NoisePropagationVisualizer : MonoBehaviour
    {
        [Header("Display")]
        [Tooltip("Seconds a noise ring stays visible before fading out.")]
        [Range(0.25f, 4f)] [SerializeField] private float _lifetime = 1.25f;

        [Tooltip("Seconds for a wavefront to expand from the source to its full effective range.")]
        [Range(0.1f, 2f)] [SerializeField] private float _frontTravelTime = 0.6f;

        [Tooltip("Also draw the faint outer sphere at each sound's full effective range.")]
        [SerializeField] private bool _drawRangeSphere = true;

        [Tooltip("Maximum simultaneous rings retained (older ones are overwritten).")]
        [Range(8, 256)] [SerializeField] private int _capacity = 64;

        private struct Record
        {
            public Vector3 Position;
            public float EffectiveRange;
            public float Loudness;
            public SoundPriority Priority;
            public float Time;
        }

        private Record[] _records;
        private int _head;

        private void Awake()
        {
            _records = new Record[Mathf.Max(8, _capacity)];
        }

        private void OnEnable()
        {
            EventBus<NoiseEvent>.Subscribe(OnNoise);
        }

        private void OnDisable()
        {
            EventBus<NoiseEvent>.Unsubscribe(OnNoise);
        }

        private void OnNoise(NoiseEvent evt)
        {
            if (_records == null)
            {
                return;
            }

            NoiseStimulus s = evt.Stimulus;
            _records[_head] = new Record
            {
                Position = s.Position,
                EffectiveRange = s.EffectiveRange,
                Loudness = s.Loudness,
                Priority = s.Priority,
                Time = Time.time
            };
            _head = (_head + 1) % _records.Length;
        }

        private void OnDrawGizmos()
        {
            if (_records == null)
            {
                return;
            }

            for (int i = 0; i < _records.Length; i++)
            {
                Record r = _records[i];
                float age = Time.time - r.Time;
                if (r.Time <= 0f || age > _lifetime)
                {
                    continue;
                }

                float lifeFraction = 1f - age / _lifetime; // 1 = fresh, 0 = gone
                Color baseColor = PriorityColor(r.Priority);

                // Expanding wavefront ring.
                float front = r.EffectiveRange * Mathf.Clamp01(age / _frontTravelTime);
                Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.8f * lifeFraction);
                Gizmos.DrawWireSphere(r.Position, Mathf.Max(0.05f, front));

                // Source marker sized by loudness.
                Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, lifeFraction);
                Gizmos.DrawSphere(r.Position, Mathf.Lerp(0.05f, 0.35f, r.Loudness));

                // Faint full-range sphere.
                if (_drawRangeSphere)
                {
                    Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.12f * lifeFraction);
                    Gizmos.DrawWireSphere(r.Position, r.EffectiveRange);
                }
            }
        }

        private static Color PriorityColor(SoundPriority priority) => priority switch
        {
            SoundPriority.Alarming => Color.red,
            SoundPriority.Notable => new Color(1f, 0.6f, 0f),
            SoundPriority.Minor => Color.yellow,
            _ => new Color(0.5f, 0.8f, 1f)
        };
    }
}
