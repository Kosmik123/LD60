using System;
using NUnit.Framework;
using SpaghettiWires.Views;

namespace SpaghettiWires.Tests
{
    public class AnchorSlotsTests
    {
        // Słup: 4 izolatory po 2 kable, pojemność 8.
        private static AnchorSlots Pole() => new AnchorSlots(anchorCount: 4, capacity: 8, placesPerAnchor: 2);

        [TestCase(1, 1)]
        [TestCase(4, 2)]
        [TestCase(9, 3)]
        [TestCase(16, 4)]
        [TestCase(6, 3)]
        public void CostOf_IsCeilingOfSquareRootOfPairs(int pairs, int expected)
        {
            Assert.AreEqual(expected, AnchorSlots.CostOf(pairs));
        }

        [Test]
        public void Pole_TakesEightCables_TwoPerAnchor()
        {
            var slots = Pole();
            for (int i = 0; i < 8; i++)
                slots.Attach(cableId: i, pairs: 1, anchor: i / 2);

            Assert.AreEqual(8, slots.Used);
            Assert.IsFalse(slots.CanAttach(1));
        }

        [Test]
        public void Trunk_UsesTwoCapacityButOnePlaceOnAnchor()
        {
            var slots = Pole();

            slots.Attach(cableId: 1, pairs: 4, anchor: 0);

            Assert.AreEqual(2, slots.Used);
            Assert.AreEqual(1, slots.PlacesUsedAt(0));
            Assert.IsTrue(slots.IsAnchorFree(0), "The anchor still has one free place");
        }

        [Test]
        public void FourTrunks_FillCapacity_WhileFourPlacesStayEmpty()
        {
            var slots = Pole();
            for (int i = 0; i < 4; i++)
                slots.Attach(cableId: i, pairs: 4, anchor: i);

            Assert.AreEqual(8, slots.Used);
            for (int anchor = 0; anchor < 4; anchor++)
                Assert.IsTrue(slots.IsAnchorFree(anchor));
            Assert.IsFalse(slots.CanAttach(1), "Capacity is full even though every anchor has a free place");
        }

        [Test]
        public void CanAttach_RejectsTrunkThatWouldExceedCapacity()
        {
            var slots = Pole();
            for (int i = 0; i < 7; i++)
                slots.Attach(cableId: i, pairs: 1, anchor: i % 4);

            Assert.IsTrue(slots.CanAttach(1));
            Assert.IsFalse(slots.CanAttach(4));
        }

        [Test]
        public void CanAttach_IsFalseWhenEveryAnchorIsFull()
        {
            var slots = new AnchorSlots(anchorCount: 2, capacity: AnchorSlots.Unlimited, placesPerAnchor: 1);
            slots.Attach(1, 1, anchor: 0);
            slots.Attach(2, 1, anchor: 1);

            Assert.IsFalse(slots.CanAttach(1));
        }

        [Test]
        public void Attach_ToFullAnchor_Throws()
        {
            var slots = Pole();
            slots.Attach(1, 1, anchor: 0);
            slots.Attach(2, 1, anchor: 0);

            Assert.Throws<InvalidOperationException>(() => slots.Attach(3, 1, anchor: 0));
        }

        [Test]
        public void Detach_FreesCapacityAndPlace()
        {
            var slots = Pole();
            slots.Attach(1, 4, anchor: 2);

            slots.Detach(1);

            Assert.AreEqual(0, slots.Used);
            Assert.AreEqual(0, slots.PlacesUsedAt(2));
            Assert.IsFalse(slots.Contains(1));
        }

        [Test]
        public void Unlimited_AcceptsAnyNumberOfCablesOnOneAnchor()
        {
            var slots = new AnchorSlots(anchorCount: 1, capacity: AnchorSlots.Unlimited, placesPerAnchor: AnchorSlots.Unlimited);
            for (int i = 0; i < 100; i++)
                slots.Attach(i, 16, anchor: 0);

            Assert.IsTrue(slots.CanAttach(16));
            Assert.IsFalse(slots.HasCapacityLimit);
        }
    }
}
