using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Audio
{
    /// <summary>
    /// Authorable data for one physical surface (wood, concrete, metal, snow…): the footstep and
    /// landing clips to play on it, pitch/volume shaping, and how loud it is for the stealth layer.
    /// <para>
    /// Because it's a <see cref="ScriptableObject"/>, adding a new surface type is pure content —
    /// create the asset, drop in clips, register it in a <see cref="SurfaceLibrary"/>. No code.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "Nordo/Audio/Surface Definition", fileName = "Surface_")]
    public sealed class SurfaceDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("The material category this surface represents. Shared with the stealth/noise layer.")]
        [SerializeField] private SurfaceKind _kind = SurfaceKind.Generic;

        [Header("Footstep Clips")]
        [Tooltip("Clips for normal walking. One is chosen at random per step (no immediate repeats).")]
        [SerializeField] private AudioClip[] _walkFootsteps = new AudioClip[0];

        [Tooltip("Clips for sprinting. Falls back to the walk set if empty.")]
        [SerializeField] private AudioClip[] _runFootsteps = new AudioClip[0];

        [Tooltip("Clips for crouched movement. Falls back to the walk set if empty.")]
        [SerializeField] private AudioClip[] _crouchFootsteps = new AudioClip[0];

        [Tooltip("Clips played when the player lands on this surface.")]
        [SerializeField] private AudioClip[] _landingSounds = new AudioClip[0];

        [Header("Shaping")]
        [Tooltip("Random pitch range applied per step to avoid a robotic, repetitive cadence.")]
        [SerializeField] private Vector2 _pitchRange = new Vector2(0.92f, 1.08f);

        [Tooltip("Base playback volume for footsteps on this surface.")]
        [Range(0f, 1f)] [SerializeField] private float _baseVolume = 0.7f;

        [Header("Stealth")]
        [Tooltip("How loud this surface is for the enemy hearing system (0 = silent like carpet, " +
                 "1 = ringing like metal grating). Multiplied by the stance loudness.")]
        [Range(0f, 1f)] [SerializeField] private float _noiseLoudness = 0.6f;

        /// <summary>The material category this surface represents.</summary>
        public SurfaceKind Kind => _kind;

        /// <summary>Random pitch range applied per step.</summary>
        public Vector2 PitchRange => _pitchRange;

        /// <summary>Base footstep volume for this surface.</summary>
        public float BaseVolume => _baseVolume;

        /// <summary>Stealth loudness multiplier for this surface, in [0, 1].</summary>
        public float NoiseLoudness => _noiseLoudness;

        /// <summary>
        /// Returns the appropriate footstep clip set for the given stance, gracefully falling back
        /// to the walk set when a stance-specific set has not been authored.
        /// </summary>
        public AudioClip[] GetFootstepClips(LocomotionStance stance)
        {
            switch (stance)
            {
                case LocomotionStance.Sprinting:
                    return HasClips(_runFootsteps) ? _runFootsteps : _walkFootsteps;
                case LocomotionStance.Crouching:
                    return HasClips(_crouchFootsteps) ? _crouchFootsteps : _walkFootsteps;
                default:
                    return _walkFootsteps;
            }
        }

        /// <summary>Returns the landing clip set (may be empty).</summary>
        public AudioClip[] GetLandingClips() => _landingSounds;

        private static bool HasClips(AudioClip[] clips) => clips != null && clips.Length > 0;

        /// <summary>
        /// Creates a fully-configured surface at runtime (used by builders that author audio in code).
        /// Prefer authored assets for hand-placed shipping content.
        /// </summary>
        public static SurfaceDefinition CreateRuntime(SurfaceKind kind, AudioClip[] walkClips,
            AudioClip[] landingClips, float noiseLoudness = 0.6f, float baseVolume = 0.7f)
        {
            var def = CreateInstance<SurfaceDefinition>();
            def._kind = kind;
            def._walkFootsteps = walkClips ?? new AudioClip[0];
            def._landingSounds = landingClips ?? new AudioClip[0];
            def._noiseLoudness = Mathf.Clamp01(noiseLoudness);
            def._baseVolume = Mathf.Clamp01(baseVolume);
            def.name = $"Surface_{kind}(runtime)";
            return def;
        }
    }
}
