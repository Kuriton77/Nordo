using UnityEngine;

namespace Nordo.Core.Events
{
    /// <summary>
    /// Raised once each time the player leaves the ground under their own power.
    /// Downstream systems (audio, the future noise system, animation) subscribe to react.
    /// </summary>
    public readonly struct PlayerJumpedEvent : IGameEvent
    {
        /// <summary>World-space position of the player at the moment of the jump.</summary>
        public readonly Vector3 Position;

        public PlayerJumpedEvent(Vector3 position)
        {
            Position = position;
        }
    }

    /// <summary>
    /// Raised the frame the player transitions from airborne to grounded.
    /// <see cref="FallSpeed"/> is the (positive) downward speed at the moment of impact,
    /// which later milestones use to scale landing sound / noise and camera dip.
    /// </summary>
    public readonly struct PlayerLandedEvent : IGameEvent
    {
        public readonly Vector3 Position;
        public readonly float FallSpeed;

        public PlayerLandedEvent(Vector3 position, float fallSpeed)
        {
            Position = position;
            FallSpeed = fallSpeed;
        }
    }

    /// <summary>
    /// Raised whenever the player's locomotion stance changes (e.g. standing → crouching,
    /// walking → sprinting). Central to Nordo's stealth: stance determines how much noise
    /// movement generates, so the noise and audio systems key off this event.
    /// </summary>
    public readonly struct PlayerStanceChangedEvent : IGameEvent
    {
        public readonly LocomotionStance Stance;

        public PlayerStanceChangedEvent(LocomotionStance stance)
        {
            Stance = stance;
        }
    }

    /// <summary>The player's current movement stance, ordered from quietest to loudest.</summary>
    public enum LocomotionStance
    {
        /// <summary>Not moving; effectively silent.</summary>
        Idle = 0,

        /// <summary>Moving while crouched — the quietest way to travel.</summary>
        Crouching = 1,

        /// <summary>Normal upright walking.</summary>
        Walking = 2,

        /// <summary>Sprinting — fast but the loudest and hungriest for stamina.</summary>
        Sprinting = 3
    }
}
