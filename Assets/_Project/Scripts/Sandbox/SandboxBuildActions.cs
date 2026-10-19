using System.Collections.Generic;
using SpaghettiWires.Map;
using SpaghettiWires.Tools;
using SpaghettiWires.Views;
using UnityEngine;

namespace SpaghettiWires.Sandbox
{
    /// <summary>
    /// TYMCZASOWE: najprostsza implementacja akcji gracza, żeby przetestować narzędzia i widoki,
    /// zanim powstaną sieć (etap 1 planu) i reguły budowy (etap 2).
    /// Trzyma tylko zajętość pól i listę kabli, bez rozmów, awarii, limitów słupów i pieniędzy.
    /// Do usunięcia, gdy prawdziwe reguły zaimplementują <see cref="IBuildActions"/>.
    /// </summary>
    public sealed class SandboxBuildActions : MonoBehaviour, IBuildActions
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

        private readonly Dictionary<Vector2Int, int> _nodeByCell = new Dictionary<Vector2Int, int>();
        private readonly Dictionary<int, Vector2Int> _cellByNode = new Dictionary<int, Vector2Int>();
        private readonly Dictionary<int, (int from, int to, int pairs)> _cables = new Dictionary<int, (int from, int to, int pairs)>();
        private int _nextNodeId = 1;
        private int _nextCableId = 1;

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
                return BuildCheck.Fail("Poza mapą.");
            if (_nodeByCell.ContainsKey(cell))
                return BuildCheck.Fail("To pole jest zajęte.");
            return BuildCheck.Ok;
        }

        public void PlacePole(Vector2Int cell) => AddNode(NodeVisualKind.Pole, cell);

        public BuildCheck CheckCable(int fromNodeId, int toNodeId, int pairs)
        {
            if (fromNodeId == toNodeId)
                return BuildCheck.Fail("Kabel musi łączyć dwa różne miejsca.");

            float span = pairs > 1 ? trunkSpan : cableSpan;
            if (MapGrid.CellDistance(_cellByNode[fromNodeId], _cellByNode[toNodeId]) > span)
                return BuildCheck.Fail("Za daleko. Postaw słup po drodze.");
            if (!CanAttach(fromNodeId, pairs) || !CanAttach(toNodeId, pairs))
                return BuildCheck.Fail("Słup jest pełny. Postaw kolejny słup.");
            return BuildCheck.Ok;
        }

        public void LayCable(int fromNodeId, int toNodeId, int pairs)
        {
            int id = _nextCableId++;
            _cables.Add(id, (fromNodeId, toNodeId, pairs));
            views.SpawnCable(id, fromNodeId, toNodeId, pairs);
        }

        public void RemoveCable(int cableId)
        {
            _cables.Remove(cableId);
            views.RemoveCable(cableId);
        }

        // Sandbox nie ma awarii, więc wszystkie żyły zawsze działają.
        public int MeasureWorkingPairs(int cableId) => _cables[cableId].pairs;

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

        private void AddNode(NodeVisualKind kind, Vector2Int cell)
        {
            int id = _nextNodeId++;
            _nodeByCell.Add(cell, id);
            _cellByNode.Add(id, cell);
            views.SpawnNode(id, kind, cell);
        }
    }
}
