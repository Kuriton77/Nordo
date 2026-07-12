using NUnit.Framework;
using Nordo.Lighting;

namespace Nordo.Tests
{
    /// <summary>
    /// Unit tests for the <see cref="Battery"/> model. Because the battery is a pure C# class with no
    /// Unity dependencies, its drain/recharge/clamp/deplete behaviour — the rules the whole flashlight
    /// mechanic rests on — can be verified deterministically without a scene.
    /// </summary>
    public sealed class BatteryTests
    {
        [Test]
        public void Drain_ReducesCharge()
        {
            var battery = new Battery(100f, 100f);
            battery.Drain(30f);
            Assert.AreEqual(70f, battery.Charge, 0.0001f);
        }

        [Test]
        public void Drain_ClampsAtZero_AndReportsEmpty()
        {
            var battery = new Battery(100f, 20f);
            battery.Drain(50f);
            Assert.AreEqual(0f, battery.Charge, 0.0001f);
            Assert.IsTrue(battery.IsEmpty);
        }

        [Test]
        public void Recharge_ClampsAtCapacity()
        {
            var battery = new Battery(100f, 80f);
            battery.Recharge(50f);
            Assert.AreEqual(100f, battery.Charge, 0.0001f);
        }

        [Test]
        public void Fraction_IsChargeOverCapacity()
        {
            var battery = new Battery(200f, 50f);
            Assert.AreEqual(0.25f, battery.Fraction, 0.0001f);
        }

        [Test]
        public void DepletedEvent_FiresExactlyOnceOnReachingZero()
        {
            var battery = new Battery(100f, 10f);
            int depletedCount = 0;
            battery.Depleted += () => depletedCount++;

            battery.Drain(10f); // -> 0, fires
            battery.Drain(5f);  // already empty, must not fire again

            Assert.AreEqual(1, depletedCount);
        }

        [Test]
        public void ChargeChanged_ReportsNewFraction()
        {
            var battery = new Battery(100f, 100f);
            float reported = -1f;
            battery.ChargeChanged += f => reported = f;

            battery.Drain(25f);

            Assert.AreEqual(0.75f, reported, 0.0001f);
        }

        [Test]
        public void SetCharge_ClampsIntoRange()
        {
            var battery = new Battery(100f, 50f);
            battery.SetCharge(999f);
            Assert.AreEqual(100f, battery.Charge, 0.0001f);
            battery.SetCharge(-5f);
            Assert.AreEqual(0f, battery.Charge, 0.0001f);
        }
    }
}
