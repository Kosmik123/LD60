using System.Collections.Generic;
using NUnit.Framework;
using SpaghettiWires.Views;
using UnityEngine;

namespace SpaghettiWires.Tests
{
    public class CableClearanceTests
    {
        private const int CableId = 1;

        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var gameObject in _objects)
                Object.DestroyImmediate(gameObject);
            _objects.Clear();
        }

        // Słup 7 m, punkt przyczepu na szczycie, z colliderem jak prefab.
        private NodeView Pole(Vector3 position) =>
            Node(NodeVisualKind.Pole, position, anchor: new Vector3(0f, 6.8f, 0f), collider: new Vector3(0.8f, 7f, 0.8f));

        // Dom 3,2 × 4,2 m z uchwytem 0,16 m przed ścianą w stronę <paramref name="wall"/>.
        private NodeView House(Vector3 position, Vector3 wall) =>
            Node(NodeVisualKind.House, position, anchor: wall * 1.76f + Vector3.up * 3.6f, collider: new Vector3(3.2f, 4.2f, 3.2f));

        private NodeView Node(NodeVisualKind kind, Vector3 position, Vector3 anchor, Vector3 collider)
        {
            var root = new GameObject(kind.ToString());
            _objects.Add(root);
            root.transform.position = position;

            var anchorPoint = new GameObject("CableAnchor_0").transform;
            anchorPoint.SetParent(root.transform, false);
            anchorPoint.localPosition = anchor;

            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, collider.y / 2f, 0f);
            box.size = collider;

            var view = root.AddComponent<NodeView>();
            view.Initialize(_objects.Count, kind, Vector2Int.zero);
            Physics.SyncTransforms();
            return view;
        }

        [Test]
        public void PoleToPole_OverOpenGround_IsClear()
        {
            Assert.IsTrue(CableClearance.IsClear(Pole(Vector3.zero), Pole(new Vector3(12f, 0f, 0f)), CableId));
        }

        [Test]
        public void PoleToPole_ThroughHouseBetween_IsBlocked()
        {
            var a = Pole(Vector3.zero);
            var b = Pole(new Vector3(12f, 0f, 0f));

            // Dom wyższy niż kabel na tej wysokości: zwis sprowadza kabel do ok. 6,2 m.
            var tower = Node(NodeVisualKind.House, new Vector3(6f, 0f, 0f), Vector3.up * 10f, new Vector3(3.2f, 10f, 3.2f));

            Assert.IsFalse(CableClearance.IsClear(a, b, CableId));
            Assert.IsNotNull(tower);
        }

        [Test]
        public void HouseWithBracketFacingThePole_IsClear()
        {
            var house = House(Vector3.zero, wall: Vector3.right);

            Assert.IsTrue(CableClearance.IsClear(house, Pole(new Vector3(8f, 0f, 0f)), CableId));
        }

        [Test]
        public void HouseWithBracketFacingAwayFromThePole_IsBlockedByItself()
        {
            var house = House(Vector3.zero, wall: Vector3.left);

            Assert.IsFalse(CableClearance.IsClear(house, Pole(new Vector3(8f, 0f, 0f)), CableId));
        }

        [Test]
        public void PoleBetweenNodes_DoesNotBlock()
        {
            var a = Pole(Vector3.zero);
            var b = Pole(new Vector3(12f, 0f, 0f));
            Node(NodeVisualKind.Pole, new Vector3(6f, 0f, 0f), Vector3.up * 10f, new Vector3(0.8f, 10f, 0.8f));

            Assert.IsTrue(CableClearance.IsClear(a, b, CableId));
        }
    }
}
