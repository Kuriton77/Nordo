using UnityEngine;
using Nordo.Core.Events;

namespace Nordo.CameraFeel
{
    /// <summary>
    /// An immutable snapshot of everything the camera effects need to know about the player and
    /// aiming this frame. The <see cref="CameraRig"/> builds one per frame and passes it to every
    /// effect, so effects stay stateless with respect to <em>where</em> the data comes from.
    /// </summary>
    public readonly struct CameraRigState
    {
        /// <summary>Current movement stance.</summary>
        public readonly LocomotionStance Stance;

        /// <summary>Horizontal speed in metres/second.</summary>
        public readonly float PlanarSpeed;

        /// <summary>Horizontal speed as a 0–1 fraction of sprint speed.</summary>
        public readonly float NormalizedSpeed;

        /// <summary>Whether the player is grounded this frame.</summary>
        public readonly bool IsGrounded;

        /// <summary>Movement intent this frame (X = strafe, Y = forward).</summary>
        public readonly Vector2 MoveInput;

        /// <summary>Change in body yaw this frame, in degrees (drives look sway).</summary>
        public readonly float YawDelta;

        /// <summary>Change in camera pitch this frame, in degrees (drives look sway).</summary>
        public readonly float PitchDelta;

        public CameraRigState(
            LocomotionStance stance,
            float planarSpeed,
            float normalizedSpeed,
            bool isGrounded,
            Vector2 moveInput,
            float yawDelta,
            float pitchDelta)
        {
            Stance = stance;
            PlanarSpeed = planarSpeed;
            NormalizedSpeed = normalizedSpeed;
            IsGrounded = isGrounded;
            MoveInput = moveInput;
            YawDelta = yawDelta;
            PitchDelta = pitchDelta;
        }
    }
}
