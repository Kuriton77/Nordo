using NUnit.Framework;
using UnityEngine;
using Nordo.Noise;

namespace Nordo.Tests
{
    /// <summary>
    /// Validates the acoustic propagation model in <see cref="NoiseAttenuation"/> — the exact rules
    /// that decide how far, and how muffled, every sound in the game travels. Because the maths is a
    /// set of pure static functions, these run deterministically with no scene.
    /// </summary>
    public sealed class NoiseAttenuationTests
    {
        [Test]
        public void DistanceFalloff_IsFullAtSource_AndZeroAtEdge()
        {
            Assert.AreEqual(1f, NoiseAttenuation.DistanceFalloff(0f, 10f), 0.0001f);
            Assert.AreEqual(0f, NoiseAttenuation.DistanceFalloff(10f, 10f), 0.0001f);
            Assert.AreEqual(0f, NoiseAttenuation.DistanceFalloff(15f, 10f), 0.0001f);
        }

        [Test]
        public void DistanceFalloff_IsLinearByDefault()
        {
            // Halfway through the range = half loudness with the default linear curve.
            Assert.AreEqual(0.5f, NoiseAttenuation.DistanceFalloff(5f, 10f), 0.0001f);
        }

        [Test]
        public void DistanceFalloff_ZeroRange_IsSilent()
        {
            Assert.AreEqual(0f, NoiseAttenuation.DistanceFalloff(0f, 0f), 0.0001f);
        }

        [Test]
        public void Occlude_NoOccluders_IsUnchanged()
        {
            Assert.AreEqual(0.8f, NoiseAttenuation.Occlude(0.8f, 0, 0.4f), 0.0001f);
        }

        [Test]
        public void Occlude_CompoundsPerWall()
        {
            // Two walls at 0.5 transmission → a quarter survives.
            Assert.AreEqual(0.25f, NoiseAttenuation.Occlude(1f, 2, 0.5f), 0.0001f);
        }

        [Test]
        public void Perceive_CombinesDistanceAndOcclusion()
        {
            // loudness 1, halfway (0.5) through range, one wall at 0.5 → 1 * 0.5 * 0.5 = 0.25.
            float perceived = NoiseAttenuation.Perceive(5f, 10f, 1f, 1, 0.5f);
            Assert.AreEqual(0.25f, perceived, 0.0001f);
        }

        [Test]
        public void CustomFalloffCurve_IsRespected()
        {
            // A curve that is flat at full loudness across the whole range.
            var flat = AnimationCurve.Constant(0f, 1f, 1f);
            Assert.AreEqual(1f, NoiseAttenuation.DistanceFalloff(9f, 10f, flat), 0.0001f);
        }
    }
}
