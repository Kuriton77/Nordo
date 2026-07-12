using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Audio
{
    /// <summary>
    /// Plays surface-aware footsteps with correct, speed-driven timing, and broadcasts a
    /// <see cref="FootstepEvent"/> for every step so the stealth/noise layer (Milestone 5) can hear
    /// the player. Steps are triggered by <em>distance travelled</em>, not a timer, so cadence is
    /// automatically correct at every speed and stays in sync with the actual movement.
    /// <para>
    /// Reads player state through the <see cref="ILocomotionState"/> abstraction (found on a parent
    /// by default), so it has no dependency on the concrete player controller.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FootstepController : MonoBehaviour
    {
        [Header("Locomotion Source")]
        [Tooltip("Component implementing ILocomotionState. If empty, a parent is searched automatically.")]
        [SerializeField] private MonoBehaviour _locomotionSource;

        [Header("Surfaces")]
        [Tooltip("Library that maps what the player stands on to a SurfaceDefinition.")]
        [SerializeField] private SurfaceLibrary _surfaceLibrary;

        /// <summary>Runtime wiring (used by the bootstrap; call before the object is activated).</summary>
        public void SetSurfaceLibrary(SurfaceLibrary library) => _surfaceLibrary = library;

        [Header("Audio")]
        [Tooltip("AudioSource used for footsteps. If empty, a 3D source is created at runtime.")]
        [SerializeField] private AudioSource _audioSource;

        [Header("Ground Probe")]
        [Tooltip("Layers the ground raycast can hit when identifying the surface underfoot.")]
        [SerializeField] private LayerMask _groundMask = ~0;

        [Tooltip("How far down to probe from the player's origin to find the floor.")]
        [Range(0.2f, 3f)] [SerializeField] private float _probeDistance = 1.4f;

        [Header("Stride Length (metres between footfalls)")]
        [Tooltip("Distance between steps while walking.")]
        [Range(0.5f, 4f)] [SerializeField] private float _walkStride = 1.9f;

        [Tooltip("Distance between steps while sprinting (longer stride, but far more frequent due to speed).")]
        [Range(0.5f, 4f)] [SerializeField] private float _sprintStride = 2.4f;

        [Tooltip("Distance between steps while crouched (careful, deliberate placement).")]
        [Range(0.5f, 4f)] [SerializeField] private float _crouchStride = 1.4f;

        [Header("Per-Stance Volume (× surface base volume)")]
        [Range(0f, 1f)] [SerializeField] private float _walkVolume = 0.7f;
        [Range(0f, 1f)] [SerializeField] private float _sprintVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float _crouchVolume = 0.35f;

        [Header("Per-Stance Stealth Loudness (× surface loudness)")]
        [Tooltip("How far these footsteps carry to the enemy, per stance. Crouch is near-silent; sprint is a beacon.")]
        [Range(0f, 1f)] [SerializeField] private float _walkLoudness = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float _sprintLoudness = 1f;
        [Range(0f, 1f)] [SerializeField] private float _crouchLoudness = 0.2f;

        [Header("Landing")]
        [Tooltip("Loudness of a landing footfall for the stealth layer (landings are loud).")]
        [Range(0f, 1f)] [SerializeField] private float _landingLoudness = 0.9f;

        private ILocomotionState _locomotion;
        private Vector3 _lastPosition;
        private float _accumulatedDistance;
        private AudioClip _lastClip;

        private void Awake()
        {
            _locomotion = _locomotionSource as ILocomotionState ?? GetComponentInParent<ILocomotionState>();
            if (_locomotion == null)
            {
                Debug.LogError($"[FootstepController] No ILocomotionState found for '{name}'. Assign a FirstPersonMotor.", this);
                enabled = false;
                return;
            }

            if (_surfaceLibrary == null)
            {
                Debug.LogError($"[FootstepController] No SurfaceLibrary assigned on '{name}'.", this);
                enabled = false;
                return;
            }

            EnsureAudioSource();
        }

        private void OnEnable()
        {
            _lastPosition = transform.position;
            EventBus<PlayerLandedEvent>.Subscribe(OnLanded);
        }

        private void OnDisable()
        {
            EventBus<PlayerLandedEvent>.Unsubscribe(OnLanded);
        }

        private void Update()
        {
            // Measure planar distance travelled since last frame.
            Vector3 position = transform.position;
            Vector3 delta = position - _lastPosition;
            delta.y = 0f;
            _lastPosition = position;

            // Only accumulate while grounded and actually moving under a locomotion stance.
            float stride = ResolveStride(_locomotion.Stance);
            if (!_locomotion.IsGrounded || stride <= 0f)
            {
                return;
            }

            _accumulatedDistance += delta.magnitude;

            // Emit as many steps as the travelled distance warrants (handles very fast frames too).
            while (_accumulatedDistance >= stride)
            {
                _accumulatedDistance -= stride;
                PlayFootstep(_locomotion.Stance);
            }
        }

        /// <summary>Resolves the surface underfoot and plays a randomized, non-repeating footstep.</summary>
        private void PlayFootstep(LocomotionStance stance)
        {
            if (!TryProbeSurface(out RaycastHit hit, out SurfaceDefinition surface) || surface == null)
            {
                return;
            }

            AudioClip clip = SelectClip(surface.GetFootstepClips(stance));
            float volume = surface.BaseVolume * ResolveVolume(stance);
            PlayClip(clip, surface.PitchRange, volume);

            float loudness = ResolveLoudness(stance) * surface.NoiseLoudness;
            EventBus<FootstepEvent>.Raise(new FootstepEvent(hit.point, surface.Kind, stance, loudness));
        }

        private void OnLanded(PlayerLandedEvent evt)
        {
            if (!TryProbeSurface(out RaycastHit hit, out SurfaceDefinition surface) || surface == null)
            {
                return;
            }

            AudioClip clip = SelectClip(surface.GetLandingClips());
            if (clip == null)
            {
                // No dedicated landing clip authored — fall back to a walk footstep so landings are audible.
                clip = SelectClip(surface.GetFootstepClips(LocomotionStance.Walking));
            }

            PlayClip(clip, surface.PitchRange, surface.BaseVolume);
            EventBus<FootstepEvent>.Raise(new FootstepEvent(hit.point, surface.Kind, LocomotionStance.Walking, _landingLoudness * surface.NoiseLoudness));

            // A landing resets the stride accumulator so the next step is a full stride away.
            _accumulatedDistance = 0f;
        }

        /// <summary>Casts down to find the floor and its surface definition.</summary>
        private bool TryProbeSurface(out RaycastHit hit, out SurfaceDefinition surface)
        {
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            if (Physics.Raycast(origin, Vector3.down, out hit, _probeDistance, _groundMask, QueryTriggerInteraction.Ignore))
            {
                surface = _surfaceLibrary.Resolve(in hit);
                return true;
            }

            surface = null;
            return false;
        }

        /// <summary>Picks a random clip, avoiding an immediate repeat when more than one exists.</summary>
        private AudioClip SelectClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            if (clips.Length == 1)
            {
                _lastClip = clips[0];
                return _lastClip;
            }

            AudioClip clip;
            do
            {
                clip = clips[Random.Range(0, clips.Length)];
            }
            while (clip == _lastClip);

            _lastClip = clip;
            return clip;
        }

        private void PlayClip(AudioClip clip, Vector2 pitchRange, float volume)
        {
            if (clip == null || _audioSource == null)
            {
                return;
            }

            _audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
            _audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private float ResolveStride(LocomotionStance stance) => stance switch
        {
            LocomotionStance.Sprinting => _sprintStride,
            LocomotionStance.Walking => _walkStride,
            LocomotionStance.Crouching => _crouchStride,
            _ => 0f // Idle: no steps.
        };

        private float ResolveVolume(LocomotionStance stance) => stance switch
        {
            LocomotionStance.Sprinting => _sprintVolume,
            LocomotionStance.Crouching => _crouchVolume,
            _ => _walkVolume
        };

        private float ResolveLoudness(LocomotionStance stance) => stance switch
        {
            LocomotionStance.Sprinting => _sprintLoudness,
            LocomotionStance.Crouching => _crouchLoudness,
            _ => _walkLoudness
        };

        /// <summary>Ensures a spatialized AudioSource exists, creating a sensible one if needed.</summary>
        private void EnsureAudioSource()
        {
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = 1f;      // fully 3D
            _audioSource.dopplerLevel = 0f;      // footsteps shouldn't Doppler-shift
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.maxDistance = 20f;
        }
    }
}
