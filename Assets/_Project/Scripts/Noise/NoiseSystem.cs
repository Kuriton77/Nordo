using System.Collections.Generic;
using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Noise
{
    /// <summary>
    /// The central sound-propagation service and the beating heart of Nordo's stealth. It listens
    /// for <see cref="NoiseEvent"/>s (raised by anything that makes a sound) and
    /// <see cref="FootstepEvent"/>s (from the footstep system), attenuates each stimulus by distance
    /// and optional occlusion, and delivers only the perceivable ones to registered
    /// <see cref="INoiseListener"/>s (the enemy's ears in Milestone 7).
    /// <para>
    /// Registered in the <see cref="ServiceLocator"/> as <see cref="INoiseService"/> so emitters and
    /// listeners never reference each other. Optimised for large levels: a cheap distance early-out
    /// runs before any occlusion raycast, and there is zero per-noise allocation.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-50)] // register before most gameplay Awakes/OnEnables that might query it
    [DisallowMultipleComponent]
    public sealed class NoiseSystem : MonoBehaviour, INoiseService
    {
        [Header("Occlusion")]
        [Tooltip("If enabled, walls between a sound and a listener muffle it via a single raycast.")]
        [SerializeField] private bool _useOcclusion = true;

        [Tooltip("Layers that block/muffle sound (walls, doors). Should exclude the player and enemies.")]
        [SerializeField] private LayerMask _occluderMask = ~0;

        [Tooltip("Fraction of loudness that survives passing through an occluder (0 = silenced, 1 = no effect).")]
        [Range(0f, 1f)] [SerializeField] private float _occlusionTransmission = 0.35f;

        [Header("Footstep → Noise Mapping")]
        [Tooltip("Range (metres) a footstep of loudness 1 carries before attenuation.")]
        [Range(1f, 40f)] [SerializeField] private float _footstepBaseRange = 14f;

        [Header("Debug")]
        [Tooltip("Log every perceived noise to the console (very verbose; QA only).")]
        [SerializeField] private bool _logHeardNoises;

        // Live listeners. A List keeps iteration allocation-free and cache-friendly for the
        // small counts we expect (a handful of AI agents), which beats a HashSet here.
        private readonly List<INoiseListener> _listeners = new();

        /// <summary>The most recent stimulus reported, exposed for debug/telemetry.</summary>
        public NoiseStimulus LastStimulus { get; private set; }

        private void Awake()
        {
            ServiceLocator.Register<INoiseService>(this);
        }

        private void OnEnable()
        {
            EventBus<NoiseEvent>.Subscribe(OnNoiseEvent);
            EventBus<FootstepEvent>.Subscribe(OnFootstepEvent);
        }

        private void OnDisable()
        {
            EventBus<NoiseEvent>.Unsubscribe(OnNoiseEvent);
            EventBus<FootstepEvent>.Unsubscribe(OnFootstepEvent);
        }

        private void OnDestroy()
        {
            // Only clear the registration if we're still the active service.
            if (ServiceLocator.TryGet(out INoiseService current) && ReferenceEquals(current, this))
            {
                ServiceLocator.Unregister<INoiseService>();
            }
        }

        /// <inheritdoc />
        public void RegisterListener(INoiseListener listener)
        {
            if (listener != null && !_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        /// <inheritdoc />
        public void UnregisterListener(INoiseListener listener)
        {
            _listeners.Remove(listener);
        }

        /// <inheritdoc />
        public void ReportNoise(in NoiseStimulus stimulus)
        {
            LastStimulus = stimulus;

            float effectiveRange = stimulus.EffectiveRange;
            for (int i = 0; i < _listeners.Count; i++)
            {
                INoiseListener listener = _listeners[i];
                if (listener == null)
                {
                    continue;
                }

                Vector3 listenerPos = listener.Position;
                float distance = Vector3.Distance(stimulus.Position, listenerPos);

                // Cheap early-out: if the sound cannot reach even the listener's own hearing radius,
                // skip it before doing any raycast. This is the hot path in a busy level.
                float reach = Mathf.Max(effectiveRange, listener.HearingRange);
                if (distance > reach || reach <= 0f)
                {
                    continue;
                }

                // Linear distance falloff over the stimulus's effective range.
                float falloff = effectiveRange > 0f ? Mathf.Clamp01(1f - distance / effectiveRange) : 0f;
                float perceived = stimulus.Loudness * falloff;

                // Occlusion: a wall between source and listener muffles the sound.
                if (_useOcclusion && perceived > 0f && IsOccluded(stimulus.Position, listenerPos))
                {
                    perceived *= _occlusionTransmission;
                }

                if (perceived <= 0.001f)
                {
                    continue;
                }

                if (_logHeardNoises)
                {
                    Debug.Log($"[NoiseSystem] {stimulus.Kind} heard @ {perceived:0.00} (priority {stimulus.Priority}).");
                }

                listener.OnHeardNoise(in stimulus, perceived);
            }
        }

        /// <summary>Single raycast test for a blocker between a sound and a listener.</summary>
        private bool IsOccluded(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            float distance = direction.magnitude;
            if (distance <= 0.01f)
            {
                return false;
            }

            return Physics.Raycast(from, direction / distance, distance, _occluderMask, QueryTriggerInteraction.Ignore);
        }

        private void OnNoiseEvent(NoiseEvent evt) => ReportNoise(evt.Stimulus);

        /// <summary>Translates a footstep into a stimulus with a stance-appropriate priority.</summary>
        private void OnFootstepEvent(FootstepEvent evt)
        {
            SoundPriority priority = evt.Stance switch
            {
                LocomotionStance.Sprinting => SoundPriority.Alarming,
                LocomotionStance.Walking => SoundPriority.Notable,
                LocomotionStance.Crouching => SoundPriority.Minor,
                _ => SoundPriority.Ambient
            };

            ReportNoise(new NoiseStimulus(evt.Position, evt.Loudness, _footstepBaseRange, priority, NoiseSourceKind.Footstep));
        }
    }
}
