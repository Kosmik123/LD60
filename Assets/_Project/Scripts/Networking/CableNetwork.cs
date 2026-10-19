using System;
using System.Collections.Generic;

namespace SpaghettiWires.Networking
{
    /// <summary>
    /// Struktura sieci (TECH 4.1): węzły i kable między nimi.
    /// Nie wie o pozycjach, limitach słupów, uszkodzeniach ani rozmowach.
    /// Błędne wywołania (nieznane Id, pętla, zła liczba żył) rzucają wyjątek:
    /// akcje gracza są walidowane wcześniej, w warstwie reguł.
    /// </summary>
    public sealed class CableNetwork
    {
        private readonly Dictionary<NodeId, Node> _nodes = new Dictionary<NodeId, Node>();
        private readonly Dictionary<CableId, Cable> _cables = new Dictionary<CableId, Cable>();

        // Indeks tylko do szybkiego przeszukiwania grafu: węzeł → Id jego kabli.
        private readonly Dictionary<NodeId, List<CableId>> _cablesByNode = new Dictionary<NodeId, List<CableId>>();

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
            var node = new Node(new NodeId(_nextNodeId++), kind);
            _nodes.Add(node.Id, node);
            _cablesByNode.Add(node.Id, new List<CableId>());
            NodeAdded?.Invoke(node);
            return node;
        }

        /// <summary>Usuwa węzeł razem ze wszystkimi jego kablami (najpierw zdarzenia usunięcia kabli).</summary>
        public void RemoveNode(NodeId nodeId)
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
        public Cable AddCable(NodeId nodeA, NodeId nodeB, int pairs, float length)
        {
            if (!_nodes.ContainsKey(nodeA))
                throw new ArgumentException($"Unknown {nodeA}.", nameof(nodeA));
            if (!_nodes.ContainsKey(nodeB))
                throw new ArgumentException($"Unknown {nodeB}.", nameof(nodeB));
            if (nodeA == nodeB)
                throw new ArgumentException($"Cable cannot connect {nodeA} to itself.", nameof(nodeB));
            if (pairs < 1)
                throw new ArgumentOutOfRangeException(nameof(pairs), pairs, "Cable needs at least one pair.");
            if (float.IsNaN(length) || length < 0f)
                throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be non-negative.");

            var cable = new Cable(new CableId(_nextCableId++), nodeA, nodeB, pairs, length);
            _cables.Add(cable.Id, cable);
            _cablesByNode[nodeA].Add(cable.Id);
            _cablesByNode[nodeB].Add(cable.Id);
            CableAdded?.Invoke(cable);
            return cable;
        }

        public void RemoveCable(CableId cableId)
        {
            var cable = GetCable(cableId);
            _cables.Remove(cableId);
            _cablesByNode[cable.A].Remove(cableId);
            _cablesByNode[cable.B].Remove(cableId);
            CableRemoved?.Invoke(cable);
        }

        public bool ContainsNode(NodeId nodeId) => _nodes.ContainsKey(nodeId);
        public bool ContainsCable(CableId cableId) => _cables.ContainsKey(cableId);

        public bool TryGetNode(NodeId nodeId, out Node node) => _nodes.TryGetValue(nodeId, out node);
        public bool TryGetCable(CableId cableId, out Cable cable) => _cables.TryGetValue(cableId, out cable);

        public Node GetNode(NodeId nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out var node))
                throw new KeyNotFoundException($"Unknown {nodeId}.");
            return node;
        }

        public Cable GetCable(CableId cableId)
        {
            if (!_cables.TryGetValue(cableId, out var cable))
                throw new KeyNotFoundException($"Unknown {cableId}.");
            return cable;
        }

        /// <summary>
        /// Id kabli podpiętych do węzła, w kolejności położenia.
        /// Lista jest na żywo: przy usuwaniu kabli w pętli najpierw ją skopiuj.
        /// </summary>
        public IReadOnlyList<CableId> GetCableIds(NodeId nodeId)
        {
            if (!_cablesByNode.TryGetValue(nodeId, out var cableIds))
                throw new KeyNotFoundException($"Unknown {nodeId}.");
            return cableIds;
        }
    }
}
