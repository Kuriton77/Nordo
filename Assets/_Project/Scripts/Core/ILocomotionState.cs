using UnityEngine;
using Nordo.Core.Events;

namespace Nordo.Core
{
    /// <summary>
    /// A read-only view of the player's movement state, published by the locomotion motor and
    /// consumed by presentation systems (camera feel, footsteps, breathing).
    /// <para>
    /// This interface is the Dependency-Inversion seam of Milestone 2: the camera and audio
    /// assemblies depend on <em>this abstraction</em> in Core, never on the concrete
    /// <c>FirstPersonMotor</c> in the Player assembly. That keeps the dependency graph acyclic
    /// and makes every consumer trivially mockable in tests.
    /// </para>
    /// </summary>
    public interface ILocomotionState
    {
        /// <summary>Current movement stance (idle / crouch / walk / sprint).</summary>
        LocomotionStance Stance { get; }

        /// <summary>Horizontal speed in metres per second.</summary>
        float CurrentSpeed { get; }

        /// <summary>Horizontal speed as a 0–1 fraction of the maximum (sprint) speed.</summary>
        float NormalizedPlanarSpeed { get; }

        /// <summary>True when the controller is standing on ground this frame.</summary>
        bool IsGrounded { get; }

        /// <summary>Raw movement intent this frame. X = strafe, Y = forward.</summary>
        Vector2 MoveInput { get; }

        /// <summary>Remaining stamina as a 0–1 fraction; drives breathing/exertion effects.</summary>
        float StaminaNormalized { get; }
    }
}
