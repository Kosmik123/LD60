using System;
using System.Collections.Generic;

namespace SpaghettiWires.Network
{
    /// <summary>
    /// Struktura sieci (TECH 4.1): węzły i kable między nimi.
    /// Nie wie o pozycjach, długościach kabli, limitach słupów, uszkodzeniach ani rozmowach.
    /// Błędne wywołania (nieznane Id, pętla, zła liczba żył) rzucają wyjątek:
    /// akcje gracza są walidowane wcześniej, w warstwie reguł.
    /// </summary>
    public sealed class CableNetwork
    {
        private readonly Dictionary<int, Node> _nodes = new Dictionary<int, Node>();
        private readonly Dictionary<int, Cable> _cables = new Dictionary<int, Cable>();

        // Indeks tylko do szybkiego przeszukiwania grafu: węzeł → Id jego kabli.
        private readonly Dictionary<int, List<int>> _cablesByNode = new Dictionary<int, List<int>>();

        // Id nie są używane ponownie, żeby stare odwołania nie trafiły w nowy obiekt.
        private int _nextNodeId = 1;
        private int _nextCableId = 1;

        public event Action<Node> NodeAdded;
        public event Action<Node> NodeRemoved;
        public event Action<Cable> CableAdded;
        public event Action<Cable> CableRemoved;

        public IReadOnlyCollection<Node> Nodes => _nodes.Values;
        public IReadOnlyCollection<Cable> Cables => _cables.Values;

        public Node AddNode(NodeKind kind)
        {
            var node = new Node(_nextNodeId++, kind);
            _nodes.Add(node.Id, node);
            _cablesByNode.Add(node.Id, new List<int>());
            NodeAdded?.Invoke(node);
            return node;
        }

        /// <summary>Usuwa węzeł razem ze wszystkimi jego kablami (najpierw zdarzenia usunięcia kabli).</summary>
        public void RemoveNode(int nodeId)
        {
            var node = GetNode(nodeId);
            var cableIds = _cablesByNode[nodeId];
            while (cableIds.Count > 0)
                RemoveCable(cableIds[cableIds.Count - 1]);

            _cablesByNode.Remove(nodeId);
            _nodes.Remove(nodeId);
            NodeRemoved?.Invoke(node);
        }

        /// <summary>
        /// Kładzie nowy kabel. Między tymi samymi węzłami może wisieć wiele kabli.
        /// </summary>
        public Cable AddCable(int nodeA, int nodeB, int pairs)
        {
            if (!_nodes.ContainsKey(nodeA))
                throw new ArgumentException($"Unknown node #{nodeA}.", nameof(nodeA));
            if (!_nodes.ContainsKey(nodeB))
                throw new ArgumentException($"Unknown node #{nodeB}.", nameof(nodeB));
            if (nodeA == nodeB)
                throw new ArgumentException($"Cable cannot connect node #{nodeA} to itself.", nameof(nodeB));
            if (pairs < 1)
                throw new ArgumentOutOfRangeException(nameof(pairs), pairs, "Cable needs at least one pair.");

            var cable = new Cable(_nextCableId++, nodeA, nodeB, pairs);
            _cables.Add(cable.Id, cable);
            _cablesByNode[nodeA].Add(cable.Id);
            _cablesByNode[nodeB].Add(cable.Id);
            CableAdded?.Invoke(cable);
            return cable;
        }

        public void RemoveCable(int cableId)
        {
            var cable = GetCable(cableId);
            _cables.Remove(cableId);
            _cablesByNode[cable.A].Remove(cableId);
            _cablesByNode[cable.B].Remove(cableId);
            CableRemoved?.Invoke(cable);
        }

        public bool ContainsNode(int nodeId) => _nodes.ContainsKey(nodeId);
        public bool ContainsCable(int cableId) => _cables.ContainsKey(cableId);

        public bool TryGetNode(int nodeId, out Node node) => _nodes.TryGetValue(nodeId, out node);
        public bool TryGetCable(int cableId, out Cable cable) => _cables.TryGetValue(cableId, out cable);

        public Node GetNode(int nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out var node))
                throw new KeyNotFoundException($"Unknown node #{nodeId}.");
            return node;
        }

        public Cable GetCable(int cableId)
        {
            if (!_cables.TryGetValue(cableId, out var cable))
                throw new KeyNotFoundException($"Unknown cable #{cableId}.");
            return cable;
        }

        /// <summary>
        /// Id kabli podpiętych do węzła, w kolejności położenia.
        /// Lista jest na żywo: przy usuwaniu kabli w pętli najpierw ją skopiuj.
        /// </summary>
        public IReadOnlyList<int> GetCableIds(int nodeId)
        {
            if (!_cablesByNode.TryGetValue(nodeId, out var cableIds))
                throw new KeyNotFoundException($"Unknown node #{nodeId}.");
            return cableIds;
        }
    }
}
