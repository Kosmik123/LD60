using System.Linq;
using NUnit.Framework;
using SpaghettiWires.Views;
using UnityEngine;

namespace SpaghettiWires.Tests
{
    public class CableAttachmentsTests
    {
        private GameObject _node;

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_node);

        private CableAttachments CreateNode(CableAttachments.AnchorChoice choice, int capacity, int places, params Vector3[] anchors)
        {
            _node = new GameObject("Node");
            for (int i = 0; i < anchors.Length; i++)
            {
                var anchor = new GameObject($"CableAnchor_{i}").transform;
                anchor.SetParent(_node.transform, false);
                anchor.localPosition = anchors[i];
            }

            var attachments = _node.AddComponent<CableAttachments>();
            attachments.Configure(choice, capacity, places);
            return attachments;
        }

        private CableAttachments CreatePole() => CreateNode(CableAttachments.AnchorChoice.Random, 8, 2,
            new Vector3(-0.6f, 7f, 0f), new Vector3(-0.2f, 7f, 0f), new Vector3(0.2f, 7f, 0f), new Vector3(0.6f, 7f, 0f));

        [Test]
        public void Pole_SpreadsEightCablesTwoPerAnchor()
        {
            var pole = CreatePole();

            var positions = Enumerable.Range(1, 8).Select(id => pole.Attach(id, 1, Vector3.zero)).ToList();

            Assert.IsFalse(pole.CanAttach(1));
            foreach (var group in positions.GroupBy(position => position))
                Assert.AreEqual(2, group.Count(), $"Anchor at {group.Key} holds two cables");
        }

        [Test]
        public void Pole_KeepsTheAnchorChosenForACable()
        {
            var pole = CreatePole();
            var attached = pole.Attach(cableId: 5, pairs: 1, towards: Vector3.right * 100f);

            pole.Attach(6, 1, Vector3.zero);
            pole.Attach(7, 1, Vector3.zero);

            Assert.AreEqual(attached, pole.GetAnchorPosition(5));
        }

        [Test]
        public void Pole_RandomChoiceDependsOnCableId()
        {
            var first = CreatePole().Attach(cableId: 42, pairs: 1, towards: Vector3.zero);
            Object.DestroyImmediate(_node);
            var second = CreatePole().Attach(cableId: 42, pairs: 1, towards: Vector3.zero);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void Preview_ShowsTheAnchorThatAttachWillUse()
        {
            var pole = CreatePole();
            for (int id = 1; id <= 8; id++)
            {
                var preview = pole.PreviewPosition(id, Vector3.zero);
                Assert.AreEqual(preview, pole.Attach(id, 1, Vector3.zero), $"Cable #{id}");
            }
        }

        [Test]
        public void Pole_AfterDetach_FreesAnchorForANewCable()
        {
            var pole = CreatePole();
            for (int id = 1; id <= 8; id++)
                pole.Attach(id, 1, Vector3.zero);
            var freed = pole.GetAnchorPosition(3);

            pole.Detach(3);

            Assert.IsTrue(pole.CanAttach(1));
            Assert.AreEqual(freed, pole.Attach(9, 1, Vector3.zero), "The only free place is where cable 3 was");
        }

        [Test]
        public void Nearest_PicksAnchorClosestToTheOtherEnd()
        {
            var exchange = CreateNode(CableAttachments.AnchorChoice.Nearest, AnchorSlots.Unlimited, AnchorSlots.Unlimited,
                new Vector3(2f, 5f, 0f), new Vector3(-2f, 5f, 0f), new Vector3(0f, 5f, 2f));

            Assert.AreEqual(new Vector3(-2f, 5f, 0f), exchange.Attach(1, 1, new Vector3(-50f, 0f, 0f)));
            Assert.AreEqual(new Vector3(0f, 5f, 2f), exchange.Attach(2, 1, new Vector3(0f, 0f, 50f)));
        }

        [Test]
        public void SingleWallAnchor_TakesEveryCable()
        {
            var house = CreateNode(CableAttachments.AnchorChoice.Nearest, AnchorSlots.Unlimited, AnchorSlots.Unlimited,
                new Vector3(0f, 3.6f, 1.76f));

            for (int id = 1; id <= 10; id++)
                Assert.AreEqual(new Vector3(0f, 3.6f, 1.76f), house.Attach(id, 1, Random.insideUnitSphere * 50f));
        }
    }
}
