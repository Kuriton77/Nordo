using UnityEngine;

namespace Nordo.Core
{
    /// <summary>
    /// Aggregates all <see cref="ILightSource"/>s so gameplay can ask a single question — "how lit is
    /// this world point?" — without iterating lights itself. Registered in the
    /// <see cref="ServiceLocator"/>. This is the seam the Milestone-7 enemy will use to know whether
    /// the player's flashlight is illuminating something; for now it is populated but unconsumed.
    /// </summary>
    public interface ILightVisibilityService
    {
        /// <summary>Registers a light source. Idempotent.</summary>
        void RegisterSource(ILightSource source);

        /// <summary>Removes a light source. Safe if not registered.</summary>
        void UnregisterSource(ILightSource source);

        /// <summary>The strongest illumination any registered source casts at the point, in [0, 1].</summary>
        float GetIlluminationAt(Vector3 worldPoint);

        /// <summary>Convenience: whether the point is lit above <paramref name="threshold"/>.</summary>
        bool IsPointLit(Vector3 worldPoint, float threshold);
    }
}
