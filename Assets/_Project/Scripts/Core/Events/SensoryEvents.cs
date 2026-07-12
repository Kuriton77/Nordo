using UnityEngine;

namespace Nordo.Core.Events
{
    /// <summary>
    /// Raised each time a footstep is planted. Carries everything downstream systems need:
    /// audio uses it for logging/mix, and from Milestone 5 the noise system treats
    /// <see cref="Loudness"/> at <see cref="Position"/> as a stimulus the enemy can hear.
    /// This is the reason footsteps are event-driven rather than a self-contained sound player.
    /// </summary>
    public readonly struct FootstepEvent : IGameEvent
    {
        /// <summary>World-space position of the footfall.</summary>
        public readonly Vector3 Position;

        /// <summary>The surface that was stepped on.</summary>
        public readonly SurfaceKind Surface;

        /// <summary>The stance the step was taken in.</summary>
        public readonly LocomotionStance Stance;

        /// <summary>Perceived loudness in the range [0, 1]; feeds the future hearing system.</summary>
        public readonly float Loudness;

        public FootstepEvent(Vector3 position, SurfaceKind surface, LocomotionStance stance, float loudness)
        {
            Position = position;
            Surface = surface;
            Stance = stance;
            Loudness = loudness;
        }
    }

    /// <summary>
    /// Raised on each breath phase change. Cold-breath VFX emit on exhale; audio/AI can scale
    /// reactions by <see cref="Intensity"/> (heavier breathing after exertion is louder and, in
    /// the cold, more visible).
    /// </summary>
    public readonly struct BreathEvent : IGameEvent
    {
        /// <summary>Exertion-driven strength of this breath, in the range [0, 1].</summary>
        public readonly float Intensity;

        /// <summary>True for the exhale phase, false for the inhale phase.</summary>
        public readonly bool IsExhale;

        /// <summary>World-space position the breath originates from (the player's head).</summary>
        public readonly Vector3 Position;

        public BreathEvent(float intensity, bool isExhale, Vector3 position)
        {
            Intensity = intensity;
            IsExhale = isExhale;
            Position = position;
        }
    }
}
