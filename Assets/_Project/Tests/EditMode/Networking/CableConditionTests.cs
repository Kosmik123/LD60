using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpaghettiWires.Networking;

namespace SpaghettiWires.Tests.Networking
{
    public class CableConditionTests
    {
        private CableNetwork _network;
        private CableCondition _condition;
        private NodeId _a;
        private NodeId _b;

        [SetUp]
        public void SetUp()
        {
            _network = new CableNetwork();
            _condition = new CableCondition(_network);
            _a = _network.AddNode(NodeKind.Pole).Id;
            _b = _network.AddNode(NodeKind.Pole).Id;
        }

        private CableId Cable(int pairs) => _network.AddCable(_a, _b, pairs, 1f).Id;

        [Test]
        public void NewCable_IsFullyWorking()
        {
            var trunk = Cable(4);

            Assert.AreEqual(0, _condition.GetDamagedPairs(trunk));
            Assert.AreEqual(4, _condition.GetWorkingPairs(trunk));
            Assert.IsFalse(_condition.IsBroken(trunk));
            CollectionAssert.IsEmpty(_condition.DamagedCableIds);
        }

        [Test]
        public void Damage_Accumulates()
        {
            var trunk = Cable(9);

            _condition.Damage(trunk, 2);
            _condition.Damage(trunk, 3);

            Assert.AreEqual(5, _condition.GetDamagedPairs(trunk));
            Assert.AreEqual(4, _condition.GetWorkingPairs(trunk));
            CollectionAssert.AreEqual(new[] { trunk }, _condition.DamagedCableIds);
        }

        [Test]
        public void Damage_IsClampedToPairs()
        {
            var trunk = Cable(4);

            _condition.Damage(trunk, 3);
            _condition.Damage(trunk, 3);

            Assert.AreEqual(4, _condition.GetDamagedPairs(trunk));
            Assert.IsTrue(_condition.IsBroken(trunk));
        }

        [Test]
        public void Damage_DoesNotOverflowForHugeValues()
        {
            var trunk = Cable(4);
            _condition.Damage(trunk, 1);

            _condition.Damage(trunk, int.MaxValue);

            Assert.AreEqual(4, _condition.GetDamagedPairs(trunk));
        }

        [Test]
        public void Break_DamagesAllPairs()
        {
            var single = Cable(1);
            var trunk = Cable(16);

            _condition.Break(single);
            _condition.Break(trunk);

            Assert.IsTrue(_condition.IsBroken(single));
            Assert.AreEqual(0, _condition.GetWorkingPairs(trunk));
        }

        [Test]
        public void PairsDamaged_IsRaisedOnlyOnChange()
        {
            var trunk = Cable(4);
            var events = new List<(CableId cableId, int damaged)>();
            _condition.PairsDamaged += (cableId, damaged) => events.Add((cableId, damaged));

            _condition.Damage(trunk, 1);
            _condition.Break(trunk);
            _condition.Damage(trunk, 1);

            CollectionAssert.AreEqual(new[] { (trunk, 1), (trunk, 4) }, events);
        }

        [Test]
        public void RemovedCable_LosesItsDamageEntry()
        {
            var trunk = Cable(4);
            var other = Cable(1);
            _condition.Damage(trunk, 2);
            _condition.Break(other);

            _network.RemoveCable(trunk);

            CollectionAssert.AreEqual(new[] { other }, _condition.DamagedCableIds);
            Assert.Throws<KeyNotFoundException>(() => _condition.GetDamagedPairs(trunk));
        }

        [Test]
        public void RemovingNode_ClearsDamageOfItsCables()
        {
            var trunk = Cable(4);
            _condition.Damage(trunk, 2);

            _network.RemoveNode(_a);

            CollectionAssert.IsEmpty(_condition.DamagedCableIds);
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            var trunk = Cable(4);

            Assert.Throws<ArgumentOutOfRangeException>(() => _condition.Damage(trunk, 0));
            Assert.Throws<KeyNotFoundException>(() => _condition.Damage(new CableId(999), 1));
            Assert.Throws<KeyNotFoundException>(() => _condition.Break(new CableId(999)));
            Assert.Throws<KeyNotFoundException>(() => _condition.GetWorkingPairs(new CableId(999)));
            Assert.AreEqual(0, _condition.GetDamagedPairs(trunk));
        }
    }
}
