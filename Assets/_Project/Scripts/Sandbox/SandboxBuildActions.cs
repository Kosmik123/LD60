using System.Collections.Generic;
using SpaghettiWires.Map;
using SpaghettiWires.Networking;
using SpaghettiWires.Simulation;
using SpaghettiWires.Tools;
using SpaghettiWires.Views;
using UnityEngine;

namespace SpaghettiWires.Sandbox
{
    /// <summary>
    /// TYMCZASOWE: najprostsza implementacja akcji gracza, zanim powstaną reguły budowy (PLAN etap 2).
    /// Prowadzi sieć gry (<see cref="PhoneNetwork"/>) i jej widoki: Id węzłów i kabli nadaje sieć,
    /// a widoki dostają te same Id. Sprawdza zajętość pól, zasięg kabla, miejsca na słupie i przecinanie budynków,
    /// ale nie liczy pieniędzy. Do usunięcia, gdy prawdziwe reguły zaimplementują <see cref="IBuildActions"/>.
    /// </summary>
    public sealed class SandboxBuildActions : MonoBehaviour, IBuildActions, IPhoneNetworkSource
    {
        [SerializeField] private MapGridBehaviour grid;
        [SerializeField] private ViewRegistry views;

        [Header("Start")]
        [SerializeField] private int startHouses = 8;
        [Tooltip("Promień (w polach) wokół środka mapy, w którym pojawiają się domy na start.")]
        [SerializeField] private int startRadius = 5;
        [SerializeField] private int seed = 60;

        [Header("Zasięg kabli (w polach)")]
        [SerializeField] private float cableSpan = 3f;
        [SerializeField] private float trunkSpan = 2.5f;

        private readonly Dictionary<Vector2Int, NodeId> _nodeByCell = new Dictionary<Vector2Int, NodeId>();
        private readonly Dictionary<NodeId, Vector2Int> _cellByNode = new Dictionary<NodeId, Vector2Int>();
        private PhoneNetwork _network;

        /// <summary>Sieć gry. Tworzona przy pierwszym użyciu, więc jest gotowa niezależnie od kolejności Awake/Start.</summary>
        public PhoneNetwork Network => _network ??= new PhoneNetwork();

        private CableNetwork Structure => Network.Structure;

        private void Start() => SpawnStartHouses();

        public void SpawnStartHouses()
        {
            var mapGrid = grid.Grid;
            var center = mapGrid.Size / 2;
            var random = new System.Random(seed);

            for (int attempts = 0, spawned = 0; spawned < startHouses && attempts < 1000; attempts++)
            {
                var cell = center + new Vector2Int(random.Next(-startRadius, startRadius + 1), random.Next(-startRadius, startRadius + 1));
                if (!mapGrid.Contains(cell) || _nodeByCell.ContainsKey(cell))
                    continue;

                AddNode(NodeVisualKind.House, cell);
                spawned++;
            }
        }

        public BuildCheck CheckPole(Vector2Int cell)
        {
            if (!grid.Grid.Contains(cell))
                return BuildCheck.Fail("Outside the map.");
            if (_nodeByCell.ContainsKey(cell))
                return BuildCheck.Fail("This cell is already taken.");
            return BuildCheck.Ok;
        }

        public void PlacePole(Vector2Int cell) => AddNode(NodeVisualKind.Pole, cell);

        public BuildCheck CheckCable(int fromNodeId, int toNodeId, int pairs)
        {
            if (fromNodeId == toNodeId)
                return BuildCheck.Fail("A cable must connect two different places.");

            if (MapGrid.CellDistance(CellOf(fromNodeId), CellOf(toNodeId)) > GetSpan(pairs))
                return BuildCheck.Fail("Too far. Place a pole in between.");
            if (!CanAttach(fromNodeId, pairs) || !CanAttach(toNodeId, pairs))
                return BuildCheck.Fail("The pole is full. Place another pole.");
            if (!views.IsCablePathClear(fromNodeId, toNodeId, NextCableId))
                return BuildCheck.Fail("The cable would go through a building.");
            return BuildCheck.Ok;
        }

        public float GetMaxCableLength(int pairs) => GetSpan(pairs) * grid.Grid.CellSize;

        private float GetSpan(int pairs) => pairs > 1 ? trunkSpan : cableSpan;

        public int NextCableId => Structure.NextCableId.Value;

        public void LayCable(int fromNodeId, int toNodeId, int pairs)
        {
            var mapGrid = grid.Grid;
            float length = Vector3.Distance(mapGrid.CellToWorld(CellOf(fromNodeId)), mapGrid.CellToWorld(CellOf(toNodeId)));
            var cable = Structure.AddCable(new NodeId(fromNodeId), new NodeId(toNodeId), pairs, length);
            views.SpawnCable(cable.Id.Value, fromNodeId, toNodeId, pairs);
        }

        public void RemoveCable(int cableId)
        {
            // Najpierw sieć: zrywa rozmowy na kablu, a widoki jeszcze istnieją, gdy dostają o tym zdarzenia.
            Structure.RemoveCable(new CableId(cableId));
            views.RemoveCable(cableId);
        }

        public int MeasureWorkingPairs(int cableId) => Network.Condition.GetWorkingPairs(new CableId(cableId));

        // Pojemność i punkty przyczepu zna model węzła (CableAttachments), więc sandbox tylko go pyta.
        public bool TryGetSlotUsage(int nodeId, out int used, out int capacity)
        {
            used = 0;
            capacity = 0;
            if (!views.TryGetNode(nodeId, out var node) || !node.Attachments.Slots.HasCapacityLimit)
                return false;

            used = node.Attachments.Slots.Used;
            capacity = node.Attachments.Slots.Capacity;
            return true;
        }

        private bool CanAttach(int nodeId, int pairs) =>
            views.TryGetNode(nodeId, out var node) && node.Attachments.CanAttach(pairs);

        private Vector2Int CellOf(int nodeId) => _cellByNode[new NodeId(nodeId)];

        private void AddNode(NodeVisualKind kind, Vector2Int cell)
        {
            var node = Structure.AddNode(ToNodeKind(kind));
            _nodeByCell.Add(cell, node.Id);
            _cellByNode.Add(node.Id, cell);
            views.SpawnNode(node.Id.Value, kind, cell);
        }

        private static NodeKind ToNodeKind(NodeVisualKind kind) => kind switch
        {
            NodeVisualKind.House => NodeKind.House,
            NodeVisualKind.Pole => NodeKind.Pole,
            NodeVisualKind.Exchange => NodeKind.Exchange,
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }
}
