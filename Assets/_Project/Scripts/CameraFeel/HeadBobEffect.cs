using System;
using UnityEngine;
using Nordo.Core.Events;

namespace Nordo.CameraFeel
{
    /// <summary>
    /// Procedural head-bob. Produces the gentle vertical/horizontal oscillation of walking, with
    /// distinct amplitude and cadence per stance (a fast, punchy sprint bob; a slow, shallow
    /// crouch bob). Cadence is tied to actual movement speed, so the bob naturally speeds up and
    /// slows down with the player rather than running on a fixed clock.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeadBobEffect : MonoBehaviour, ICameraEffect
    {
        /// <summary>Tuning for the bob in a single stance.</summary>
        [Serializable]
        public struct BobProfile
        {
            [Tooltip("Cadence multiplier — higher means more steps per metre travelled.")]
            [Range(0.1f, 4f)] public float Frequency;

            [Tooltip("Vertical bob amplitude in metres (the up/down of each step).")]
            [Range(0f, 0.2f)] public float VerticalAmplitude;

            [Tooltip("Horizontal sway amplitude in metres (the side-to-side weight shift).")]
            [Range(0f, 0.2f)] public float HorizontalAmplitude;

            [Tooltip("Roll amplitude in degrees (the subtle head tilt with each step).")]
            [Range(0f, 6f)] public float RollAmplitude;
        }

        [Header("Enable")]
        [Tooltip("Master toggle — an accessibility option can flip this off at runtime.")]
        [SerializeField] private bool _enabled = true;

        [Header("Per-Stance Profiles")]
        [SerializeField]
        private BobProfile _walk = new() { Frequency = 0.9f, VerticalAmplitude = 0.045f, HorizontalAmplitude = 0.035f, RollAmplitude = 1.1f };

        [SerializeField]
        private BobProfile _sprint = new() { Frequency = 1.1f, VerticalAmplitude = 0.075f, HorizontalAmplitude = 0.05f, RollAmplitude = 2.0f };

        [SerializeField]
        private BobProfile _crouch = new() { Frequency = 0.75f, VerticalAmplitude = 0.025f, HorizontalAmplitude = 0.02f, RollAmplitude = 0.6f };

        [Header("Blending")]
        [Tooltip("How quickly the bob fades in when starting to move and out when stopping (per second).")]
        [Range(1f, 20f)] [SerializeField] private float _weightBlendSpeed = 8f;

        private float _phase;
        private float _weight;

        /// <inheritdoc />
        public bool IsActive => _enabled;

        /// <inheritdoc />
        public CameraEffectSample Evaluate(float deltaTime, in CameraRigState state)
        {
            BobProfile profile = ResolveProfile(state.Stance);

            // The bob is only present while actually moving on the ground.
            bool moving = state.IsGrounded && state.PlanarSpeed > 0.1f && state.MoveInput.sqrMagnitude > 0.01f;
            float targetWeight = moving ? 1f : 0f;
            _weight = Mathf.MoveTowards(_weight, targetWeight, _weightBlendSpeed * deltaTime);

            // Advance the phase in proportion to speed → cadence tracks how fast we travel.
            _phase += deltaTime * profile.Frequency * state.PlanarSpeed * Mathf.PI;

            // Vertical oscillates at twice the horizontal frequency: two footfalls per full sway.
            float vertical = Mathf.Sin(_phase * 2f) * profile.VerticalAmplitude * _weight;
            float horizontal = Mathf.Cos(_phase) * profile.HorizontalAmplitude * _weight;
            float roll = Mathf.Cos(_phase) * profile.RollAmplitude * _weight;

            Vector3 position = new Vector3(horizontal, vertical, 0f);
            Vector3 euler = new Vector3(0f, 0f, roll);
            return new CameraEffectSample(position, euler);
        }

        private BobProfile ResolveProfile(LocomotionStance stance)
        {
            return stance switch
            {
                LocomotionStance.Sprinting => _sprint,
                LocomotionStance.Crouching => _crouch,
                _ => _walk
            };
        }
    }
}
