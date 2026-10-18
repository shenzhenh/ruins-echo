using NUnit.Framework;
using RuinsEcho.Core;
using RuinsEcho.Player;

namespace RuinsEcho.Tests
{
    public sealed class EchoEnergyTests
    {
        [Test]
        public void Tick_RegeneratesAndClampsToMax()
        {
            var energy = new EchoEnergy(100f, 0f, 8f);
            energy.Tick(1f);
            Assert.AreEqual(8f, energy.CurrentEnergy, 0.0001f);

            energy.Tick(100f);
            Assert.AreEqual(100f, energy.CurrentEnergy, 0.0001f);
        }

        [Test]
        public void TryConsume_DoesNotSpendWhenInsufficient()
        {
            var energy = new EchoEnergy(100f, 10f, 0f);

            Assert.IsFalse(energy.TryConsume(20f));
            Assert.AreEqual(10f, energy.CurrentEnergy, 0.0001f);
        }

        [Test]
        public void SummonCost_RespectsRelicMultiplier()
        {
            var energy = new EchoEnergy();
            energy.CostMultiplier = 2f;

            Assert.AreEqual(GameConfig.BaseSummonCost * 2f, energy.SummonCost, 0.0001f);
        }

        [Test]
        public void Drain_ClampsAtZero()
        {
            var energy = new EchoEnergy(100f, 10f, 0f);
            energy.Drain(15f);
            Assert.AreEqual(0f, energy.CurrentEnergy, 0.0001f);
        }

        [Test]
        public void ResetForNewRun_Refills()
        {
            var energy = new EchoEnergy(100f, 5f, 0f);
            energy.ResetForNewRun();
            Assert.AreEqual(100f, energy.CurrentEnergy, 0.0001f);
        }
    }
}
