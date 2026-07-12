using UnityEngine;

namespace Nordo.CameraFeel
{
    /// <summary>
    /// Natural camera sway from two sources:
    /// <list type="bullet">
    /// <item><b>Look sway</b> — when you turn, the view lags a touch and counter-rolls, giving the
    /// camera a sense of mass instead of feeling bolted to the mouse.</item>
    /// <item><b>Idle drift</b> — a slow Perlin-noise wander so the camera breathes and is never
    /// perfectly, unnervingly still (which reads as "dead" and breaks immersion).</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraSwayEffect : MonoBehaviour, ICameraEffect
    {
        [Header("Enable")]
        [SerializeField] private bool _enabled = true;

        [Header("Look Sway (reacts to turning)")]
        [Tooltip("Positional lag per degree of yaw, in metres. Higher = the view trails more when turning.")]
        [Range(0f, 0.02f)] [SerializeField] private float _positionalSwayPerDegree = 0.004f;

        [Tooltip("Counter-roll per degree of yaw, in degrees. Adds a lean into fast turns.")]
        [Range(0f, 0.5f)] [SerializeField] private float _rollPerYawDegree = 0.08f;

        [Tooltip("Counter-pitch per degree of pitch change, in degrees.")]
        [Range(0f, 0.5f)] [SerializeField] private float _pitchCounterPerDegree = 0.06f;

        [Tooltip("Clamp on the total look-sway magnitude to prevent extreme spikes on fast flicks.")]
        [Range(0.5f, 10f)] [SerializeField] private float _maxSwayDegrees = 4f;

        [Header("Idle Drift (always-on life)")]
        [Tooltip("Amplitude of the idle rotational wander, in degrees.")]
        [Range(0f, 2f)] [SerializeField] private float _idleAmplitude = 0.35f;

        [Tooltip("Speed of the idle wander.")]
        [Range(0.05f, 2f)] [SerializeField] private float _idleFrequency = 0.35f;

        private float _noiseSeedX;
        private float _noiseSeedY;

        private void Awake()
        {
            // Randomize noise sampling origins so multiple cameras (or restarts) don't sway in sync.
            _noiseSeedX = Random.value * 100f;
            _noiseSeedY = Random.value * 100f + 50f;
        }

        /// <inheritdoc />
        public bool IsActive => _enabled;

        /// <inheritdoc />
        public CameraEffectSample Evaluate(float deltaTime, in CameraRigState state)
        {
            // --- Look sway: view trails and counter-rotates against the turn. ---
            float swayRoll = Mathf.Clamp(state.YawDelta * _rollPerYawDegree, -_maxSwayDegrees, _maxSwayDegrees);
            float swayPitch = Mathf.Clamp(state.PitchDelta * _pitchCounterPerDegree, -_maxSwayDegrees, _maxSwayDegrees);
            float swayX = Mathf.Clamp(-state.YawDelta * _positionalSwayPerDegree, -0.05f, 0.05f);

            // --- Idle drift: signed Perlin noise (centered on zero) for a gentle wander. ---
            float t = Time.time * _idleFrequency;
            float driftPitch = (Mathf.PerlinNoise(_noiseSeedX, t) - 0.5f) * 2f * _idleAmplitude;
            float driftYaw = (Mathf.PerlinNoise(_noiseSeedY, t) - 0.5f) * 2f * _idleAmplitude;

            Vector3 position = new Vector3(swayX, 0f, 0f);
            Vector3 euler = new Vector3(swayPitch + driftPitch, driftYaw, swayRoll);
            return new CameraEffectSample(position, euler);
        }
    }
}
