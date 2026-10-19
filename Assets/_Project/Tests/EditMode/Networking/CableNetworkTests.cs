using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpaghettiWires.Networking;

namespace SpaghettiWires.Tests.Networking
{
    public class CableNetworkTests
    {
        private CableNetwork _network;

        [SetUp]
        public void SetUp()
        {
            _network = new CableNetwork();
        }

        [Test]
        public void AddNode_AssignsUniqueIdsAndKind()
        {
            var house = _network.AddNode(NodeKind.House);
            var pole = _network.AddNode(NodeKind.Pole);

            Assert.AreNotEqual(house.Id, pole.Id);
            Assert.AreEqual(NodeKind.House, house.Kind);
            Assert.AreSame(pole, _network.GetNode(pole.Id));
            Assert.AreEqual(2, _network.Nodes.Count);
            CollectionAssert.IsEmpty(_network.GetCableIds(house.Id));
        }

        [Test]
        public void AddCable_StoresDataAndIndexesBothEnds()
        {
            var a = _network.AddNode(NodeKind.House);
            var b = _network.AddNode(NodeKind.Pole);

            var cable = _network.AddCable(a.Id, b.Id, 4, 12.5f);

            Assert.AreEqual(a.Id, cable.A);
            Assert.AreEqual(b.Id, cable.B);
            Assert.AreEqual(4, cable.Pairs);
            Assert.AreEqual(12.5f, cable.Length);
            Assert.AreSame(cable, _network.GetCable(cable.Id));
            CollectionAssert.AreEqual(new[] { cable.Id }, _network.GetCableIds(a.Id));
            CollectionAssert.AreEqual(new[] { cable.Id }, _network.GetCableIds(b.Id));
        }

        [Test]
        public void AddCable_AllowsParallelCablesBetweenSameNodes()
        {
            var a = _network.AddNode(NodeKind.Pole);
            var b = _network.AddNode(NodeKind.Pole);

            var first = _network.AddCable(a.Id, b.Id, 1, 5f);
            var second = _network.AddCable(b.Id, a.Id, 1, 5f);

            Assert.AreNotEqual(first.Id, second.Id);
            CollectionAssert.AreEqual(new[] { first.Id, second.Id }, _network.GetCableIds(a.Id));
        }

        [Test]
        public void AddCable_RejectsInvalidArguments()
        {
            var a = _network.AddNode(NodeKind.Pole);
            var b = _network.AddNode(NodeKind.Pole);

            Assert.Throws<ArgumentException>(() => _network.AddCable(a.Id, 999, 1, 1f));
            Assert.Throws<ArgumentException>(() => _network.AddCable(a.Id, a.Id, 1, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => _network.AddCable(a.Id, b.Id, 0, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => _network.AddCable(a.Id, b.Id, 1, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => _network.AddCable(a.Id, b.Id, 1, float.NaN));
            Assert.AreEqual(0, _network.Cables.Count);
        }

        [Test]
        public void RemoveCable_RemovesFromIndexAndRaisesEvent()
        {
            var a = _network.AddNode(NodeKind.House);
            var b = _network.AddNode(NodeKind.Pole);
            var cable = _network.AddCable(a.Id, b.Id, 1, 3f);
            Cable removed = null;
            _network.CableRemoved += c => removed = c;

            _network.RemoveCable(cable.Id);

            Assert.AreSame(cable, removed);
            Assert.IsFalse(_network.ContainsCable(cable.Id));
            CollectionAssert.IsEmpty(_network.GetCableIds(a.Id));
            CollectionAssert.IsEmpty(_network.GetCableIds(b.Id));
            Assert.Throws<KeyNotFoundException>(() => _network.RemoveCable(cable.Id));
        }

        [Test]
        public void RemoveNode_RemovesItsCablesBeforeTheNode()
        {
            var a = _network.AddNode(NodeKind.House);
            var pole = _network.AddNode(NodeKind.Pole);
            var c = _network.AddNode(NodeKind.House);
            var toA = _network.AddCable(a.Id, pole.Id, 1, 1f);
            var toC = _network.AddCable(pole.Id, c.Id, 1, 1f);
            var events = new List<string>();
            _network.CableRemoved += cable => events.Add("cable " + cable.Id);
            _network.NodeRemoved += node => events.Add("node " + node.Id);

            _network.RemoveNode(pole.Id);

            CollectionAssert.AreEquivalent(
                new[] { "cable " + toA.Id, "cable " + toC.Id },
                events.GetRange(0, 2));
            Assert.AreEqual("node " + pole.Id, events[2]);
            Assert.IsFalse(_network.ContainsNode(pole.Id));
            Assert.AreEqual(0, _network.Cables.Count);
            CollectionAssert.IsEmpty(_network.GetCableIds(a.Id));
            CollectionAssert.IsEmpty(_network.GetCableIds(c.Id));
        }

        [Test]
        public void Ids_AreNotReusedAfterRemoval()
        {
            var a = _network.AddNode(NodeKind.Pole);
            var b = _network.AddNode(NodeKind.Pole);
            var cable = _network.AddCable(a.Id, b.Id, 1, 1f);

            _network.RemoveCable(cable.Id);
            _network.RemoveNode(b.Id);
            var newNode = _network.AddNode(NodeKind.Pole);
            var newCable = _network.AddCable(a.Id, newNode.Id, 1, 1f);

            Assert.AreNotEqual(b.Id, newNode.Id);
            Assert.AreNotEqual(cable.Id, newCable.Id);
        }

        [Test]
        public void AddEvents_AreRaised()
        {
            Node addedNode = null;
            Cable addedCable = null;
            _network.NodeAdded += n => addedNode = n;
            _network.CableAdded += c => addedCable = c;

            var a = _network.AddNode(NodeKind.Exchange);
            var b = _network.AddNode(NodeKind.Pole);
            var cable = _network.AddCable(a.Id, b.Id, 9, 2f);

            Assert.AreSame(b, addedNode);
            Assert.AreSame(cable, addedCable);
        }

        [Test]
        public void GetOtherEnd_ReturnsOppositeNode()
        {
            var cable = new Cable(1, 10, 20, 1, 1f);

            Assert.AreEqual(20, cable.GetOtherEnd(10));
            Assert.AreEqual(10, cable.GetOtherEnd(20));
            Assert.Throws<ArgumentException>(() => cable.GetOtherEnd(30));
        }

        [Test]
        public void UnknownIds_Throw()
        {
            Assert.Throws<KeyNotFoundException>(() => _network.GetNode(1));
            Assert.Throws<KeyNotFoundException>(() => _network.GetCable(1));
            Assert.Throws<KeyNotFoundException>(() => _network.GetCableIds(1));
            Assert.Throws<KeyNotFoundException>(() => _network.RemoveNode(1));
            Assert.IsFalse(_network.TryGetNode(1, out _));
            Assert.IsFalse(_network.TryGetCable(1, out _));
        }
    }
}
