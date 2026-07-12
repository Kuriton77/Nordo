using UnityEngine;

namespace Nordo.Interaction
{
    /// <summary>
    /// Everything an <see cref="IInteractable"/> needs to know about the interaction taking place:
    /// who is interacting, through which camera, and where the look ray struck. Passed by
    /// <c>in</c> reference as an immutable, allocation-free readonly struct.
    /// </summary>
    public readonly struct InteractionContext
    {
        /// <summary>Root GameObject of the interacting actor (the player), used to find its sub-systems.</summary>
        public readonly GameObject InteractorRoot;

        /// <summary>Camera the interaction is viewed through (its forward is the look ray).</summary>
        public readonly Camera Camera;

        /// <summary>World-space point the look ray hit on the interactable.</summary>
        public readonly Vector3 Point;

        /// <summary>Surface normal at <see cref="Point"/>.</summary>
        public readonly Vector3 Normal;

        /// <summary>Distance from the camera to <see cref="Point"/>.</summary>
        public readonly float Distance;

        public InteractionContext(GameObject interactorRoot, Camera camera, Vector3 point, Vector3 normal, float distance)
        {
            InteractorRoot = interactorRoot;
            Camera = camera;
            Point = point;
            Normal = normal;
            Distance = distance;
        }
    }
}
