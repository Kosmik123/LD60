using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpaghettiWires.Network;

namespace SpaghettiWires.Tests.Network
{
    public class CablePathfinderTests
    {
        private CableNetwork _network;
        private Dictionary<int, float> _lengths;

        [SetUp]
        public void SetUp()
        {
            _network = new CableNetwork();
            _lengths = new Dictionary<int, float>();
        }

        private int Node(NodeKind kind = NodeKind.Pole) => _network.AddNode(kind).Id;

        private int Cable(int a, int b, float length = 1f)
        {
            var cable = _network.AddCable(a, b, 1);
            _lengths[cable.Id] = length;
            return cable.Id;
        }

        private float Length(Cable cable) => _lengths[cable.Id];

        [Test]
        public void FindsPathThroughChain()
        {
            int a = Node(), b = Node(), c = Node();
            int ab = Cable(a, b, 2f), bc = Cable(b, c, 3f);

            Assert.IsTrue(CablePathfinder.TryFindPath(_network, a, c, out var path, Length));

            CollectionAssert.AreEqual(new[] { a, b, c }, path.Nodes);
            CollectionAssert.AreEqual(new[] { ab, bc }, path.Cables);
            Assert.AreEqual(5f, path.Length);
        }

        [Test]
        public void PathCanBeFoundInBothDirections()
        {
            int a = Node(), b = Node();
            int ab = Cable(a, b);

            Assert.IsTrue(CablePathfinder.TryFindPath(_network, b, a, out var path));

            CollectionAssert.AreEqual(new[] { b, a }, path.Nodes);
            CollectionAssert.AreEqual(new[] { ab }, path.Cables);
        }

        [Test]
        public void PrefersShorterRouteOverFewerCables()
        {
            int a = Node(), b = Node(), c = Node();
            Cable(a, c, 10f);
            int ab = Cable(a, b, 2f), bc = Cable(b, c, 3f);

            Assert.IsTrue(CablePathfinder.TryFindPath(_network, a, c, out var path, Length));

            CollectionAssert.AreEqual(new[] { ab, bc }, path.Cables);
            Assert.AreEqual(5f, path.Length);
        }

        [Test]
        public void WithoutWeights_PrefersFewestCables()
        {
            int a = Node(), b = Node(), c = Node();
            int ac = Cable(a, c, 10f);
            Cable(a, b, 2f);
            Cable(b, c, 3f);

            Assert.IsTrue(CablePathfinder.TryFindPath(_network, a, c, out var path));

            CollectionAssert.AreEqual(new[] { ac }, path.Cables);
            Assert.AreEqual(1f, path.Length);
        }

        [Test]
        public void PicksLighterOfParallelCables()
        {
            int a = Node(), b = Node();
            Cable(a, b, 4f);
            int light = Cable(a, b, 1f);

            Assert.IsTrue(CablePathfinder.TryFindPath(_network, a, b, out var path, Length));

            CollectionAssert.AreEqual(new[] { light }, path.Cables);
        }

        [Test]
        public void ReturnsFalseWhenNodesAreNotConnected()
        {
            int a = Node(), b = Node(), c = Node();
            Cable(a, b);

            Assert.IsFalse(CablePathfinder.TryFindPath(_network, a, c, out var path));
            Assert.IsNull(path);
        }

        [Test]
        public void ReturnsFalseAfterOnlyCableIsRemoved()
        {
            int a = Node(), b = Node();
            int ab = Cable(a, b);
            _network.RemoveCable(ab);

            Assert.IsFalse(CablePathfinder.TryFindPath(_network, a, b, out _));
        }

        [Test]
        public void SkipsCablesRejectedByFilter()
        {
            int a = Node(), b = Node(), c = Node();
            int blocked = Cable(a, c, 1f);
            int ab = Cable(a, b, 5f), bc = Cable(b, c, 5f);

            Assert.IsTrue(CablePathfinder.TryFindPath(
                _network, a, c, out var path, Length, canUseCable: cable => cable.Id != blocked));

            CollectionAssert.AreEqual(new[] { ab, bc }, path.Cables);
        }

        [Test]
        public void SkipsNodesRejectedByFilter()
        {
            int a = Node(), busyHouse = Node(NodeKind.House), pole = Node(), c = Node();
            Cable(a, busyHouse, 1f);
            Cable(busyHouse, c, 1f);
            Cable(a, pole, 5f);
            Cable(pole, c, 5f);

            Assert.IsTrue(CablePathfinder.TryFindPath(
                _network, a, c, out var path, Length, canUseNode: node => node.Id != busyHouse));

            CollectionAssert.AreEqual(new[] { a, pole, c }, path.Nodes);
        }

        [Test]
        public void ReturnsFalseWhenEndpointRejectedByFilter()
        {
            int a = Node(), b = Node();
            Cable(a, b);

            Assert.IsFalse(CablePathfinder.TryFindPath(_network, a, b, out _, canUseNode: node => node.Id != a));
            Assert.IsFalse(CablePathfinder.TryFindPath(_network, a, b, out _, canUseNode: node => node.Id != b));
        }

        [Test]
        public void SameStartAndEnd_ReturnsEmptyPath()
        {
            int a = Node();

            Assert.IsTrue(CablePathfinder.TryFindPath(_network, a, a, out var path));

            CollectionAssert.AreEqual(new[] { a }, path.Nodes);
            CollectionAssert.IsEmpty(path.Cables);
            Assert.AreEqual(0f, path.Length);
        }

        [Test]
        public void InvalidInput_Throws()
        {
            int a = Node(), b = Node();
            Cable(a, b);

            Assert.Throws<KeyNotFoundException>(() => CablePathfinder.TryFindPath(_network, a, 999, out _));
            Assert.Throws<InvalidOperationException>(
                () => CablePathfinder.TryFindPath(_network, a, b, out _, cableWeight: _ => -1f));
        }
    }
}
