using NUnit.Framework;
using UnityEngine;
using Nordo.CameraFeel;

namespace Nordo.Tests
{
    /// <summary>
    /// Validates the additive-composition contract of <see cref="CameraEffectSample"/>. This is the
    /// invariant the whole camera-feel stack relies on: any number of effects can be summed and the
    /// result is the component-wise total, with <see cref="CameraEffectSample.Zero"/> as the identity.
    /// </summary>
    public sealed class CameraEffectSampleTests
    {
        [Test]
        public void Zero_IsAdditiveIdentity()
        {
            var sample = new CameraEffectSample(new Vector3(1f, 2f, 3f), new Vector3(4f, 5f, 6f));
            CameraEffectSample result = sample + CameraEffectSample.Zero;

            Assert.AreEqual(sample.PositionOffset, result.PositionOffset);
            Assert.AreEqual(sample.EulerOffset, result.EulerOffset);
        }

        [Test]
        public void Addition_SumsComponentWise()
        {
            var a = new CameraEffectSample(new Vector3(1f, 0f, -2f), new Vector3(10f, 0f, 0f));
            var b = new CameraEffectSample(new Vector3(0f, 3f, 2f), new Vector3(0f, -4f, 5f));

            CameraEffectSample sum = a + b;

            Assert.AreEqual(new Vector3(1f, 3f, 0f), sum.PositionOffset);
            Assert.AreEqual(new Vector3(10f, -4f, 5f), sum.EulerOffset);
        }

        [Test]
        public void Addition_IsCommutative()
        {
            var a = new CameraEffectSample(new Vector3(0.5f, 1.5f, 2.5f), new Vector3(1f, 2f, 3f));
            var b = new CameraEffectSample(new Vector3(-1f, 2f, -3f), new Vector3(-4f, 5f, -6f));

            CameraEffectSample ab = a + b;
            CameraEffectSample ba = b + a;

            Assert.AreEqual(ab.PositionOffset, ba.PositionOffset);
            Assert.AreEqual(ab.EulerOffset, ba.EulerOffset);
        }
    }
}
