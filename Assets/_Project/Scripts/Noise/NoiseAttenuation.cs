using UnityEngine;

namespace Nordo.Noise
{
    /// <summary>
    /// The pure mathematics of acoustic propagation — distance falloff and occlusion — extracted from
    /// <see cref="NoiseSystem"/> as stateless static functions. Isolating the model here means the
    /// stealth rules can be <b>unit-tested deterministically</b> (no scene, no raycasts) and reused,
    /// which is how Milestone 5 "validates" the acoustic system.
    /// </summary>
    public static class NoiseAttenuation
    {
        /// <summary>
        /// Attenuation from distance alone, in [0, 1]. Returns 0 at/after the effective range.
        /// </summary>
        /// <param name="distance">Distance from source to listener, in metres.</param>
        /// <param name="effectiveRange">How far the sound carries (range × loudness), in metres.</param>
        /// <param name="falloffCurve">
        /// Optional curve mapping normalized distance (0 at source → 1 at edge) to a multiplier.
        /// When null, a linear <c>1 − x</c> falloff is used.
        /// </param>
        public static float DistanceFalloff(float distance, float effectiveRange, AnimationCurve falloffCurve = null)
        {
            if (effectiveRange <= 0f || distance >= effectiveRange)
            {
                return 0f;
            }

            if (distance <= 0f)
            {
                return 1f;
            }

            float x = distance / effectiveRange; // 0 at source, 1 at the edge
            float value = falloffCurve != null ? falloffCurve.Evaluate(x) : 1f - x;
            return Mathf.Clamp01(value);
        }

        /// <summary>
        /// Applies occlusion: each blocker between source and listener multiplies the surviving
        /// loudness by <paramref name="transmission"/>, so two walls attenuate as transmission².
        /// </summary>
        public static float Occlude(float perceivedLoudness, int occluderCount, float transmission)
        {
            if (occluderCount <= 0 || perceivedLoudness <= 0f)
            {
                return Mathf.Max(0f, perceivedLoudness);
            }

            return perceivedLoudness * Mathf.Pow(Mathf.Clamp01(transmission), occluderCount);
        }

        /// <summary>
        /// The full model: loudness attenuated by distance falloff and then by occlusion.
        /// </summary>
        public static float Perceive(
            float distance,
            float effectiveRange,
            float loudness,
            int occluderCount,
            float transmission,
            AnimationCurve falloffCurve = null)
        {
            float afterDistance = loudness * DistanceFalloff(distance, effectiveRange, falloffCurve);
            return Occlude(afterDistance, occluderCount, transmission);
        }
    }
}
