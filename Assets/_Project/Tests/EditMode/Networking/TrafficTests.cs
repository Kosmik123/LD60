using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpaghettiWires.Networking;

namespace SpaghettiWires.Tests.Networking
{
    public class TrafficTests
    {
        // Warstwa ruchu tylko zapamiętuje czas trwania (rozmowy kończy symulacja), więc w tych testach jest dowolny.
        private const float CallDuration = 5f;

        private CableNetwork _structure;
        private CableCondition _condition;
        private Traffic _traffic;

        [SetUp]
        public void SetUp()
        {
            var network = new PhoneNetwork();
            _structure = network.Structure;
            _condition = network.Condition;
            _traffic = network.Traffic;
        }

        private NodeId House() => _structure.AddNode(NodeKind.House).Id;
        private NodeId Pole() => _structure.AddNode(NodeKind.Pole).Id;
        private CableId Cable(NodeId a, NodeId b, float length = 1f, int pairs = 1) => _structure.AddCable(a, b, pairs, length).Id;

        private Call Start(NodeId from, NodeId to)
        {
            Assert.IsTrue(_traffic.TryStartCall(from, to, CallDuration, out var call), $"Call {from}->{to} should start.");
            return call;
        }

        [Test]
        public void Call_TakesRouteAndOccupiesOnePairPerCable()
        {
            NodeId a = House(), pole = Pole(), b = House();
            CableId toPole = Cable(a, pole, 2f), toB = Cable(pole, b, 3f);

            var call = Start(a, b);

            Assert.AreEqual(a, call.From);
            Assert.AreEqual(b, call.To);
            CollectionAssert.AreEqual(new[] { a, pole, b }, call.Nodes);
            CollectionAssert.AreEqual(new[] { toPole, toB }, call.Cables);
            Assert.AreEqual(5f, call.Length);
            Assert.AreEqual(1, _traffic.GetBusyPairs(toPole));
            Assert.AreEqual(0, _traffic.GetFreePairs(toPole));
            Assert.AreEqual(1, _traffic.GetNodeLoad(pole));
            CollectionAssert.AreEqual(new[] { call }, _traffic.Calls);
        }

        [Test]
        public void Route_MinimisesTotalLengthNotCableCount()
        {
            NodeId a = Pole(), b = Pole(), c = Pole(), d = Pole();
            CableId direct = Cable(a, d, 10f);
            CableId ab = Cable(a, b, 2f), bc = Cable(b, c, 2f), cd = Cable(c, d, 2f);

            CollectionAssert.AreEqual(new[] { ab, bc, cd }, Start(a, d).Cables);
            CollectionAssert.AreEqual(new[] { direct }, Start(a, d).Cables);
        }

        [Test]
        public void Route_PrefersFewerLongerCablesWhenShorterInTotal()
        {
            NodeId a = Pole(), b = Pole(), c = Pole();
            CableId direct = Cable(a, c, 4f);
            Cable(a, b, 3f);
            Cable(b, c, 3f);

            CollectionAssert.AreEqual(new[] { direct }, Start(a, c).Cables);
        }

        [Test]
        public void Trunk_CarriesOneCallPerPair()
        {
            NodeId a = Pole(), b = Pole();
            CableId trunk = Cable(a, b, pairs: 4);

            for (int i = 0; i < 4; i++)
                Start(a, b);

            Assert.IsFalse(_traffic.TryStartCall(a, b, CallDuration, out var busy));
            Assert.IsNull(busy);
            Assert.AreEqual(4, _traffic.GetBusyPairs(trunk));
        }

        [Test]
        public void DamagedPairs_ReduceCapacity()
        {
            NodeId a = Pole(), b = Pole();
            CableId trunk = Cable(a, b, pairs: 4);
            _condition.Damage(trunk, 3);

            Start(a, b);

            Assert.IsFalse(_traffic.TryStartCall(a, b, CallDuration, out _));
            Assert.AreEqual(0, _traffic.GetFreePairs(trunk));
        }

        [Test]
        public void BrokenOrFullCable_IsBypassed()
        {
            NodeId a = Pole(), b = Pole(), c = Pole();
            CableId shortCable = Cable(a, c, 1f);
            CableId viaB1 = Cable(a, b, 5f), viaB2 = Cable(b, c, 5f);
            _condition.Break(shortCable);

            CollectionAssert.AreEqual(new[] { viaB1, viaB2 }, Start(a, c).Cables);
            Assert.IsFalse(_traffic.TryStartCall(a, c, CallDuration, out _));
        }

        [Test]
        public void NoConnection_IsBusy()
        {
            NodeId a = House(), b = House();

            Assert.IsFalse(_traffic.TryStartCall(a, b, CallDuration, out _));
        }

        [Test]
        public void BusyHouse_CannotCallOrBeCalled()
        {
            NodeId a = House(), b = House(), c = House(), pole = Pole();
            Cable(a, pole);
            Cable(b, pole);
            Cable(c, pole);
            var call = Start(a, b);

            Assert.IsFalse(_traffic.TryStartCall(c, a, CallDuration, out _));
            Assert.IsFalse(_traffic.TryStartCall(b, c, CallDuration, out _));

            _traffic.EndCall(call.Id);

            Start(c, a);
        }

        [Test]
        public void House_RelaysCallAndIsBlockedMeanwhile()
        {
            NodeId a = House(), relay = House(), b = House(), other = House(), pole = Pole();
            Cable(a, relay);
            Cable(relay, b);
            Cable(other, pole);
            Cable(relay, pole);

            var call = Start(a, b);

            CollectionAssert.AreEqual(new[] { a, relay, b }, call.Nodes);
            Assert.AreEqual(1, _traffic.GetNodeLoad(relay));
            Assert.IsFalse(_traffic.TryStartCall(other, relay, CallDuration, out _));
        }

        [Test]
        public void Route_AvoidsBusyHouse()
        {
            NodeId a = House(), busy = House(), b = House(), pole = Pole(), x = House(), y = House();
            Cable(a, busy, 1f);
            Cable(busy, b, 1f);
            Cable(a, pole, 5f);
            Cable(pole, b, 5f);
            Cable(x, busy);
            Cable(busy, y);
            Start(x, y);

            CollectionAssert.AreEqual(new[] { a, pole, b }, Start(a, b).Nodes);
        }

        [Test]
        public void EndCall_ReleasesPairsAndNodes()
        {
            NodeId a = House(), pole = Pole(), b = House();
            CableId toPole = Cable(a, pole), toB = Cable(pole, b);
            var call = Start(a, b);
            Call ended = null;
            _traffic.CallEnded += c => ended = c;

            _traffic.EndCall(call.Id);

            Assert.AreSame(call, ended);
            Assert.AreEqual(0, _traffic.GetBusyPairs(toPole));
            Assert.AreEqual(1, _traffic.GetFreePairs(toB));
            Assert.AreEqual(0, _traffic.GetNodeLoad(a));
            Assert.AreEqual(0, _traffic.GetNodeLoad(pole));
            CollectionAssert.IsEmpty(_traffic.Calls);
            Assert.Throws<KeyNotFoundException>(() => _traffic.EndCall(call.Id));
        }

        [Test]
        public void Damage_DropsNewestCallsAboveWorkingPairs()
        {
            NodeId a = Pole(), b = Pole(), c = Pole();
            CableId trunk = Cable(a, b, pairs: 4);
            CableId tail = Cable(b, c, pairs: 4);
            var first = Start(a, b);
            var second = Start(a, c);
            var third = Start(a, c);
            var dropped = new List<Call>();
            _traffic.CallDropped += dropped.Add;

            _condition.Damage(trunk, 3);

            CollectionAssert.AreEqual(new[] { third, second }, dropped);
            CollectionAssert.AreEqual(new[] { first }, _traffic.Calls);
            Assert.AreEqual(1, _traffic.GetBusyPairs(trunk));
            Assert.AreEqual(0, _traffic.GetBusyPairs(tail));
        }

        [Test]
        public void Damage_KeepsCallsThatStillFit()
        {
            NodeId a = Pole(), b = Pole();
            CableId trunk = Cable(a, b, pairs: 4);
            Start(a, b);
            Start(a, b);
            var dropped = new List<Call>();
            _traffic.CallDropped += dropped.Add;

            _condition.Damage(trunk, 2);

            CollectionAssert.IsEmpty(dropped);
            Assert.AreEqual(0, _traffic.GetFreePairs(trunk));
        }

        [Test]
        public void RemovingCable_DropsItsCallsAndFreesTheRestOfTheirRoutes()
        {
            NodeId a = House(), pole = Pole(), b = House(), c = House(), d = House();
            CableId toPole = Cable(a, pole), toB = Cable(pole, b);
            Cable(c, pole);
            Cable(pole, d);
            var kept = Start(c, d);
            var lost = Start(a, b);
            var dropped = new List<Call>();
            _traffic.CallDropped += dropped.Add;

            _structure.RemoveCable(toPole);

            CollectionAssert.AreEqual(new[] { lost }, dropped);
            CollectionAssert.AreEqual(new[] { kept }, _traffic.Calls);
            Assert.AreEqual(0, _traffic.GetBusyPairs(toB));
            Assert.AreEqual(0, _traffic.GetNodeLoad(b));
            Assert.AreEqual(1, _traffic.GetNodeLoad(pole));
        }

        [Test]
        public void RemovingNode_DropsCallsThroughIt()
        {
            NodeId a = House(), pole = Pole(), b = House();
            Cable(a, pole);
            Cable(pole, b);
            var call = Start(a, b);
            var dropped = new List<Call>();
            _traffic.CallDropped += dropped.Add;

            _structure.RemoveNode(pole);

            CollectionAssert.AreEqual(new[] { call }, dropped);
            Assert.AreEqual(0, _traffic.GetNodeLoad(a));
            Assert.AreEqual(0, _traffic.GetNodeLoad(b));
        }

        [Test]
        public void LoadChangedEvents_CoverWholeRoute()
        {
            NodeId a = House(), pole = Pole(), b = House();
            CableId toPole = Cable(a, pole), toB = Cable(pole, b);
            var cables = new List<CableId>();
            var nodes = new List<NodeId>();
            _traffic.CableLoadChanged += cables.Add;
            _traffic.NodeLoadChanged += nodes.Add;

            _traffic.EndCall(Start(a, b).Id);

            CollectionAssert.AreEqual(new[] { toPole, toB, toPole, toB }, cables);
            CollectionAssert.AreEqual(new[] { a, pole, b, a, pole, b }, nodes);
        }

        [Test]
        public void RemovedCable_IsNotReportedInLoadChange()
        {
            NodeId a = Pole(), b = Pole(), c = Pole();
            CableId ab = Cable(a, b), bc = Cable(b, c);
            Start(a, c);
            var cables = new List<CableId>();
            _traffic.CableLoadChanged += cables.Add;

            _structure.RemoveCable(ab);

            CollectionAssert.AreEqual(new[] { bc }, cables);
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            NodeId a = House();

            Assert.Throws<ArgumentException>(() => _traffic.TryStartCall(a, a, CallDuration, out _));
            Assert.Throws<KeyNotFoundException>(() => _traffic.TryStartCall(a, new NodeId(999), CallDuration, out _));
            Assert.Throws<KeyNotFoundException>(() => _traffic.GetBusyPairs(new CableId(999)));
            Assert.Throws<KeyNotFoundException>(() => _traffic.GetNodeLoad(new NodeId(999)));
        }

        [Test]
        public void CallCapacity_DependsOnNodeKind()
        {
            Assert.AreEqual(1, Traffic.GetCallCapacity(NodeKind.House));
            Assert.AreEqual(int.MaxValue, Traffic.GetCallCapacity(NodeKind.Pole));
            Assert.AreEqual(int.MaxValue, Traffic.GetCallCapacity(NodeKind.Exchange));
        }
    }
}
