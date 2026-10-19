using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpaghettiWires.Simulation;

namespace SpaghettiWires.Tests.Simulation
{
    public class EconomyTests
    {
        [Test]
        public void StartsWithGivenValues()
        {
            var economy = new Economy(500, 0.8f);

            Assert.AreEqual(500, economy.Money);
            Assert.AreEqual(0.8f, economy.Reputation);
            Assert.IsFalse(economy.IsLost);
            Assert.AreEqual(LossReason.None, economy.LossReason);
        }

        [Test]
        public void TrySpend_PaysWhenAffordable()
        {
            var economy = new Economy(100);

            Assert.IsTrue(economy.CanAfford(100));
            Assert.IsTrue(economy.TrySpend(60));
            Assert.AreEqual(40, economy.Money);
        }

        [Test]
        public void TrySpend_RefusesWhenTooExpensive()
        {
            var economy = new Economy(50);
            var changes = new List<int>();
            economy.MoneyChanged += changes.Add;

            Assert.IsFalse(economy.CanAfford(51));
            Assert.IsFalse(economy.TrySpend(51));
            Assert.AreEqual(50, economy.Money);
            CollectionAssert.IsEmpty(changes);
        }

        [Test]
        public void SpendingEverything_IsNotALoss()
        {
            var economy = new Economy(30);

            economy.TrySpend(30);

            Assert.AreEqual(0, economy.Money);
            Assert.IsFalse(economy.IsLost);
        }

        [Test]
        public void Earn_AddsMoneyAndRaisesEvent()
        {
            var economy = new Economy(10);
            var changes = new List<int>();
            economy.MoneyChanged += changes.Add;

            economy.Earn(15);
            economy.Earn(0);

            Assert.AreEqual(25, economy.Money);
            CollectionAssert.AreEqual(new[] { 25 }, changes);
        }

        [Test]
        public void Charge_BelowZero_LosesOnce()
        {
            var economy = new Economy(20);
            var losses = new List<LossReason>();
            economy.Lost += losses.Add;

            economy.Charge(20);
            Assert.IsFalse(economy.IsLost);

            economy.Charge(5);
            economy.Charge(5);

            Assert.AreEqual(-10, economy.Money);
            Assert.AreEqual(LossReason.Money, economy.LossReason);
            CollectionAssert.AreEqual(new[] { LossReason.Money }, losses);
        }

        [Test]
        public void ChangeReputation_IsClampedAndRaisesEventOnChange()
        {
            var economy = new Economy(0, 0.5f);
            var changes = new List<float>();
            economy.ReputationChanged += changes.Add;

            economy.ChangeReputation(0.2f);
            economy.ChangeReputation(1f);
            economy.ChangeReputation(0.1f);

            Assert.AreEqual(1f, economy.Reputation);
            Assert.AreEqual(2, changes.Count);
            Assert.AreEqual(0.7f, changes[0], 1e-6f);
            Assert.AreEqual(1f, changes[1]);
        }

        [Test]
        public void ReputationAtZero_Loses()
        {
            var economy = new Economy(100, 0.3f);
            var losses = new List<LossReason>();
            economy.Lost += losses.Add;

            economy.ChangeReputation(-0.2f);
            Assert.IsFalse(economy.IsLost);

            economy.ChangeReputation(-0.5f);
            economy.ChangeReputation(-0.1f);

            Assert.AreEqual(0f, economy.Reputation);
            Assert.AreEqual(LossReason.Reputation, economy.LossReason);
            CollectionAssert.AreEqual(new[] { LossReason.Reputation }, losses);
        }

        [Test]
        public void FirstLossReason_IsKept()
        {
            var economy = new Economy(0, 0.1f);

            economy.ChangeReputation(-1f);
            economy.Charge(10);

            Assert.AreEqual(LossReason.Reputation, economy.LossReason);
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Economy(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Economy(0, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Economy(0, 1.5f));

            var economy = new Economy(10);
            Assert.Throws<ArgumentOutOfRangeException>(() => economy.TrySpend(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => economy.Earn(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => economy.Charge(-1));
            Assert.Throws<ArgumentException>(() => economy.ChangeReputation(float.NaN));
            Assert.AreEqual(10, economy.Money);
        }
    }
}
