using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.CameraFeel
{
    /// <summary>
    /// A weighty landing reaction. When the player hits the ground, the camera dips and pitches
    /// down, then springs back — the harder the fall (from <see cref="PlayerLandedEvent.FallSpeed"/>),
    /// the bigger the impact. Modelled as a damped spring so it always resolves smoothly to rest.
    /// <para>
    /// It listens on the <see cref="EventBus{T}"/> rather than polling the motor, so it neither
    /// knows nor cares which object jumped — any landing event triggers it.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LandingImpactEffect : MonoBehaviour, ICameraEffect
    {
        [Header("Enable")]
        [SerializeField] private bool _enabled = true;

        [Header("Impact Shaping")]
        [Tooltip("Fall speed (m/s) that produces a full-strength impact. Faster impacts are clamped to this.")]
        [Range(2f, 20f)] [SerializeField] private float _referenceFallSpeed = 9f;

        [Tooltip("Maximum downward camera dip at full strength, in metres.")]
        [Range(0f, 0.5f)] [SerializeField] private float _maxDip = 0.14f;

        [Tooltip("Maximum downward pitch kick at full strength, in degrees.")]
        [Range(0f, 15f)] [SerializeField] private float _maxPitchKick = 6f;

        [Header("Spring")]
        [Tooltip("Spring stiffness — higher returns to rest faster.")]
        [Range(20f, 400f)] [SerializeField] private float _stiffness = 140f;

        [Tooltip("Spring damping — higher settles with less bounce.")]
        [Range(2f, 40f)] [SerializeField] private float _damping = 16f;

        // Spring state for the vertical dip (metres) and the pitch kick (degrees).
        private float _dip;
        private float _dipVelocity;
        private float _pitch;
        private float _pitchVelocity;

        /// <inheritdoc />
        public bool IsActive => _enabled;

        private void OnEnable()
        {
            EventBus<PlayerLandedEvent>.Subscribe(OnLanded);
        }

        private void OnDisable()
        {
            EventBus<PlayerLandedEvent>.Unsubscribe(OnLanded);
        }

        private void OnLanded(PlayerLandedEvent evt)
        {
            // Scale the impulse by how hard we hit, clamped to [0, 1].
            float strength = Mathf.Clamp01(evt.FallSpeed / _referenceFallSpeed);

            // Give each spring an initial velocity sized so its peak displacement lands near the
            // configured maximum. For an undamped spring the peak is v0 / omega, where
            // omega = sqrt(stiffness); we invert that to pick v0 = targetPeak * omega. Damping then
            // makes the real peak a little smaller, which reads as natural.
            float omega = Mathf.Sqrt(_stiffness);
            _dipVelocity += strength * _maxDip * omega;
            _pitchVelocity += strength * _maxPitchKick * omega;
        }

        /// <inheritdoc />
        public CameraEffectSample Evaluate(float deltaTime, in CameraRigState state)
        {
            IntegrateSpring(ref _dip, ref _dipVelocity, deltaTime);
            IntegrateSpring(ref _pitch, ref _pitchVelocity, deltaTime);

            Vector3 position = new Vector3(0f, -Mathf.Max(0f, _dip), 0f);
            Vector3 euler = new Vector3(_pitch, 0f, 0f);
            return new CameraEffectSample(position, euler);
        }

        /// <summary>Advances a critically-dampable spring toward zero using semi-implicit Euler.</summary>
        private void IntegrateSpring(ref float value, ref float velocity, float dt)
        {
            float force = (-_stiffness * value) - (_damping * velocity);
            velocity += force * dt;
            value += velocity * dt;

            // Snap tiny residuals to zero so the spring truly rests (and stops costing us cycles).
            if (Mathf.Abs(value) < 0.0001f && Mathf.Abs(velocity) < 0.0001f)
            {
                value = 0f;
                velocity = 0f;
            }
        }
    }
}
