using UnityEngine;

namespace Nordo.Core
{
    /// <summary>
    /// A source of light the game logic can query — currently the flashlight. It exposes just enough
    /// for systems (and the future enemy AI) to ask "is this point lit, and how strongly?" without
    /// knowing anything about Unity <c>Light</c> components, cones or shadows.
    /// <para>
    /// This is a <b>hook only</b> for Milestone 4: the interface and the service exist and are fed by
    /// the flashlight, but no AI consumes them yet. Milestone 7 will.
    /// </para>
    /// </summary>
    public interface ILightSource
    {
        /// <summary>True when the source is actively casting light this frame.</summary>
        bool IsEmitting { get; }

        /// <summary>World position of the source (the emitter origin).</summary>
        Vector3 Position { get; }

        /// <summary>
        /// Illumination this source contributes at <paramref name="worldPoint"/>, in [0, 1], after
        /// range/cone falloff and line-of-sight occlusion. Returns 0 when not emitting or the point
        /// is outside the beam / blocked.
        /// </summary>
        float GetIlluminationAt(Vector3 worldPoint);
    }
}
