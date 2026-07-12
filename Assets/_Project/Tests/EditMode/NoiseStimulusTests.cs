using NUnit.Framework;
using UnityEngine;
using Nordo.Core;

namespace Nordo.Tests
{
    /// <summary>
    /// Validates the <see cref="NoiseStimulus"/> value semantics that the whole stealth layer relies
    /// on: loudness/range clamping and the effective-range calculation the noise service uses to
    /// decide who can hear what.
    /// </summary>
    public sealed class NoiseStimulusTests
    {
        [Test]
        public void EffectiveRange_IsRangeScaledByLoudness()
        {
            var stimulus = new NoiseStimulus(Vector3.zero, 0.5f, 20f, SoundPriority.Notable, NoiseSourceKind.Footstep);
            Assert.AreEqual(10f, stimulus.EffectiveRange, 0.0001f);
        }

        [Test]
        public void Loudness_IsClampedToUnitRange()
        {
            var over = new NoiseStimulus(Vector3.zero, 5f, 10f, SoundPriority.Minor, NoiseSourceKind.Impact);
            var under = new NoiseStimulus(Vector3.zero, -3f, 10f, SoundPriority.Minor, NoiseSourceKind.Impact);

            Assert.AreEqual(1f, over.Loudness);
            Assert.AreEqual(0f, under.Loudness);
        }

        [Test]
        public void Range_IsNeverNegative()
        {
            var stimulus = new NoiseStimulus(Vector3.zero, 1f, -5f, SoundPriority.Ambient, NoiseSourceKind.Generic);
            Assert.AreEqual(0f, stimulus.Range);
        }

        [Test]
        public void Priority_OrdersFromAmbientToAlarming()
        {
            Assert.Less((int)SoundPriority.Ambient, (int)SoundPriority.Minor);
            Assert.Less((int)SoundPriority.Minor, (int)SoundPriority.Notable);
            Assert.Less((int)SoundPriority.Notable, (int)SoundPriority.Alarming);
        }
    }
}
