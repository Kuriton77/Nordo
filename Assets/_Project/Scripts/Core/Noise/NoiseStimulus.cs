using UnityEngine;

namespace Nordo.Core
{
    /// <summary>
    /// A single unit of sound in the world: <em>where</em> it happened, <em>how loud</em> it is, how
    /// far it can carry, and how important it is. This is the common currency between everything that
    /// makes noise (footsteps, thrown props, doors, machines) and everything that hears
    /// (the enemy, debug listeners). Immutable and allocation-free (a readonly struct).
    /// </summary>
    public readonly struct NoiseStimulus
    {
        /// <summary>World-space origin of the sound.</summary>
        public readonly Vector3 Position;

        /// <summary>Base intensity in [0, 1] at the source before any distance attenuation.</summary>
        public readonly float Loudness;

        /// <summary>Maximum distance (metres) the sound carries at <see cref="Loudness"/> == 1.</summary>
        public readonly float Range;

        /// <summary>Importance used by the AI to arbitrate between stimuli.</summary>
        public readonly SoundPriority Priority;

        /// <summary>What produced the sound.</summary>
        public readonly NoiseSourceKind Kind;

        public NoiseStimulus(Vector3 position, float loudness, float range, SoundPriority priority, NoiseSourceKind kind)
        {
            Position = position;
            Loudness = Mathf.Clamp01(loudness);
            Range = Mathf.Max(0f, range);
            Priority = priority;
            Kind = kind;
        }

        /// <summary>
        /// The effective radius this stimulus actually reaches, i.e. range scaled by loudness.
        /// A quiet sound in a loud-capable emitter still only carries as far as its loudness allows.
        /// </summary>
        public float EffectiveRange => Range * Loudness;
    }
}
