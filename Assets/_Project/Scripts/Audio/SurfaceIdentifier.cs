using UnityEngine;

namespace Nordo.Audio
{
    /// <summary>
    /// Tags a collider (a floor, a rug, a metal catwalk) with its <see cref="SurfaceDefinition"/>.
    /// This is the most explicit and highest-priority way the footstep system identifies what the
    /// player is standing on — it wins over physics-material lookups. Attach it to the object the
    /// floor collider lives on (or any parent of that collider).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SurfaceIdentifier : MonoBehaviour
    {
        [Tooltip("Which surface this object's collider represents for footstep audio and noise.")]
        [SerializeField] private SurfaceDefinition _surface;

        /// <summary>The surface this collider represents. May be null if left unassigned.</summary>
        public SurfaceDefinition Surface => _surface;

        /// <summary>Runtime wiring (used by the level builder).</summary>
        public void SetSurface(SurfaceDefinition surface) => _surface = surface;
    }
}
