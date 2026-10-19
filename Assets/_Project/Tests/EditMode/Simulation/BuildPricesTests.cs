using System;
using NUnit.Framework;
using SpaghettiWires.Simulation;

namespace SpaghettiWires.Tests.Simulation
{
    public class BuildPricesTests
    {
        private readonly BuildPrices _prices = new BuildPrices(pole: 100, cablePerMeter: 5f, trunkPerPairMeter: 3.5f);

        [Test]
        public void Cable_CostsLengthTimesPricePerMeter()
        {
            Assert.AreEqual(60, _prices.Cable(12f, 1));
            Assert.AreEqual(0, _prices.Cable(0f, 1));
        }

        [Test]
        public void Cable_IsRoundedUp()
        {
            Assert.AreEqual(45, _prices.Cable(8.944f, 1));
        }

        [Test]
        public void Trunk_PaysPerPair()
        {
            Assert.AreEqual(14f, _prices.PerMeter(4));
            Assert.AreEqual(168, _prices.Cable(12f, 4));
            Assert.AreEqual(378, _prices.Cable(12f, 9));
        }

        [Test]
        public void LongerCable_CostsMore()
        {
            Assert.Less(_prices.Cable(4f, 1), _prices.Cable(8f, 1));
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BuildPrices(-1, 1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BuildPrices(1, -1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BuildPrices(1, 1f, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => _prices.Cable(-1f, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _prices.Cable(1f, 0));
        }
    }
}
