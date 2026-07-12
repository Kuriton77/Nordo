using UnityEngine;
using Nordo.Core;

namespace Nordo.Noise
{
    /// <summary>
    /// A development-only listener that makes the invisible audible: it draws a gizmo at the last
    /// heard sound (sized by perceived loudness, coloured by priority) and optionally logs it. Drop
    /// it on an empty GameObject to "stand in" for the enemy while tuning emitters and propagation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebugNoiseListener : NoiseListenerBase
    {
        [Header("Debug")]
        [SerializeField] private bool _logToConsole = true;

        [Tooltip("How long (seconds) the last-heard gizmo persists.")]
        [Range(0.2f, 5f)] [SerializeField] private float _gizmoHold = 1.5f;

        private Vector3 _lastPosition;
        private float _lastLoudness;
        private SoundPriority _lastPriority;
        private float _lastTime = -999f;

        /// <inheritdoc />
        public override void OnHeardNoise(in NoiseStimulus stimulus, float perceivedLoudness)
        {
            _lastPosition = stimulus.Position;
            _lastLoudness = perceivedLoudness;
            _lastPriority = stimulus.Priority;
            _lastTime = Time.time;

            if (_logToConsole)
            {
                Debug.Log($"[DebugNoiseListener] Heard {stimulus.Kind} ({stimulus.Priority}) " +
                          $"loudness {perceivedLoudness:0.00} at {stimulus.Position}.");
            }
        }

        private void OnDrawGizmos()
        {
            // Always show the hearing radius so placement is easy to reason about.
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, _hearingRange);

            if (Time.time - _lastTime > _gizmoHold)
            {
                return;
            }

            Gizmos.color = PriorityColor(_lastPriority);
            float radius = Mathf.Lerp(0.15f, 1.5f, _lastLoudness);
            Gizmos.DrawSphere(_lastPosition, radius);
            Gizmos.DrawLine(transform.position, _lastPosition);
        }

        private static Color PriorityColor(SoundPriority priority) => priority switch
        {
            SoundPriority.Alarming => Color.red,
            SoundPriority.Notable => new Color(1f, 0.6f, 0f),
            SoundPriority.Minor => Color.yellow,
            _ => Color.gray
        };
    }
}
