using System.Collections.Generic;
using NUnit.Framework;
using SpaghettiWires.Networking;

namespace SpaghettiWires.Tests.Networking
{
    public class IdTests
    {
        [Test]
        public void IdsWithSameValue_AreEqual()
        {
            Assert.IsTrue(new NodeId(3) == new NodeId(3));
            Assert.IsTrue(new CableId(3) != new CableId(4));
            Assert.AreEqual(new CallId(7), new CallId(7));
        }

        [Test]
        public void Ids_WorkAsDictionaryKeys()
        {
            var names = new Dictionary<NodeId, string> { [new NodeId(1)] = "A" };

            Assert.AreEqual("A", names[new NodeId(1)]);
            Assert.IsFalse(names.ContainsKey(new NodeId(2)));
        }

        [Test]
        public void ToString_ShowsKindOfId()
        {
            Assert.AreEqual("Node#1", new NodeId(1).ToString());
            Assert.AreEqual("Cable#2", new CableId(2).ToString());
            Assert.AreEqual("Call#3", new CallId(3).ToString());
        }

        [Test]
        public void NetworkNeverAssignsDefaultId()
        {
            var network = new CableNetwork();
            var a = network.AddNode(NodeKind.House);
            var b = network.AddNode(NodeKind.Pole);

            Assert.AreNotEqual(default(NodeId), a.Id);
            Assert.AreNotEqual(default(CableId), network.AddCable(a.Id, b.Id, 1, 1f).Id);
        }
    }
}
