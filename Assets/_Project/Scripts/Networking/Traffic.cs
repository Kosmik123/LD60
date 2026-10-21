using System;
using System.Collections.Generic;

namespace SpaghettiWires.Networking
{
    /// <summary>
    /// Ruch w sieci (TECH 4.1): trwające rozmowy, zajęte żyły i zajęte domy, wyszukiwanie trasy.
    /// Źródłem prawdy są rozmowy, a zajętość kabli i węzłów to indeksy wyliczane z nich.
    /// </summary>
    public sealed class Traffic
    {
        private readonly CableNetwork _network;
        private readonly CableCondition _condition;

        private readonly Dictionary<CallId, Call> _calls = new Dictionary<CallId, Call>();

        // Rozmowy na kablu w kolejności rozpoczęcia: liczba = zajęte żyły, ostatnia = najnowsza.
        private readonly Dictionary<CableId, List<CallId>> _callsByCable = new Dictionary<CableId, List<CallId>>();

        // Liczba rozmów przechodzących przez węzeł (także jako początek lub koniec).
        private readonly Dictionary<NodeId, int> _callCountByNode = new Dictionary<NodeId, int>();

        private int _nextCallId = 1;

        public event Action<Call> CallStarted;
        public event Action<Call> CallEnded;

        /// <summary>Rozmowa zerwana przez uszkodzenie lub usunięcie kabla.</summary>
        public event Action<Call> CallDropped;

        /// <summary>Zmieniła się liczba zajętych żył kabla.</summary>
        public event Action<CableId> CableLoadChanged;

        /// <summary>Zmieniła się liczba rozmów przechodzących przez węzeł.</summary>
        public event Action<NodeId> NodeLoadChanged;

        public IReadOnlyCollection<Call> Calls => _calls.Values;

        public Traffic(CableNetwork network, CableCondition condition)
        {
            _network = network;
            _condition = condition;
            _network.CableRemoved += cable => DropCallsAbove(cable.Id, 0);
            _condition.PairsDamaged += (cableId, _) => DropCallsAbove(cableId, _condition.GetWorkingPairs(cableId));
        }

        /// <summary>
        /// Ile rozmów naraz może przechodzić przez węzeł danego typu (GDD 3.1).
        /// Dom ma jedną linię: swoją rozmowę albo cudzą. Słup i centrala ogranicza tylko liczba żył.
        /// </summary>
        public static int GetCallCapacity(NodeKind kind) => kind == NodeKind.House ? 1 : int.MaxValue;

        /// <summary>
        /// Szuka najkrótszej trasy z wolnymi żyłami i, jeśli jest, zajmuje ją na czas rozmowy.
        /// Zwraca false („zajęte”), gdy trasy nie ma albo węzeł początkowy lub docelowy jest zajęty.
        /// </summary>
        public bool TryStartCall(NodeId fromNodeId, NodeId toNodeId, float duration, out Call call)
        {
            call = null;
            if (fromNodeId == toNodeId)
                throw new ArgumentException($"Call cannot start and end at {fromNodeId}.", nameof(toNodeId));
            if (!CanPass(_network.GetNode(fromNodeId)) || !CanPass(_network.GetNode(toNodeId)))
                return false;
            if (!TryFindRoute(fromNodeId, toNodeId, out var nodes, out var cables, out float length))
                return false;

            call = new Call(new CallId(_nextCallId++), nodes, cables, length, duration);
            _calls.Add(call.Id, call);
            foreach (CableId cableId in cables)
            {
                if (!_callsByCable.TryGetValue(cableId, out var callIds))
                    _callsByCable[cableId] = callIds = new List<CallId>();
                callIds.Add(call.Id);
            }

            foreach (NodeId nodeId in nodes)
                _callCountByNode[nodeId] = GetNodeLoad(nodeId) + 1;

            CallStarted?.Invoke(call);
            NotifyLoadChanged(call);
            return true;
        }

        /// <summary>Normalne zakończenie rozmowy: zwalnia żyły i linie domów.</summary>
        public void EndCall(CallId callId)
        {
            var call = Release(callId);
            CallEnded?.Invoke(call);
            NotifyLoadChanged(call);
        }

        public bool TryGetCall(CallId callId, out Call call) => _calls.TryGetValue(callId, out call);

        public int GetBusyPairs(CableId cableId)
        {
            _network.GetCable(cableId);
            return _callsByCable.TryGetValue(cableId, out var callIds) ? callIds.Count : 0;
        }

        /// <summary>Żyły, którymi może pójść nowa rozmowa: wszystkie minus uszkodzone minus zajęte.</summary>
        public int GetFreePairs(CableId cableId) => _condition.GetWorkingPairs(cableId) - GetBusyPairs(cableId);

        /// <summary>Liczba rozmów przechodzących przez węzeł, także jako początek lub koniec.</summary>
        public int GetNodeLoad(NodeId nodeId)
        {
            _network.GetNode(nodeId);
            return _callCountByNode.TryGetValue(nodeId, out int count) ? count : 0;
        }

        private bool CanPass(Node node) => GetNodeLoad(node.Id) < GetCallCapacity(node.Kind);

        /// <summary>Zrywa najnowsze rozmowy na kablu, aż zostanie ich najwyżej <paramref name="maxCalls"/>.</summary>
        private void DropCallsAbove(CableId cableId, int maxCalls)
        {
            while (_callsByCable.TryGetValue(cableId, out var callIds) && callIds.Count > maxCalls)
            {
                var call = Release(callIds[callIds.Count - 1]);
                CallDropped?.Invoke(call);
                NotifyLoadChanged(call);
            }
        }

        private Call Release(CallId callId)
        {
            if (!_calls.TryGetValue(callId, out var call))
                throw new KeyNotFoundException($"Unknown {callId}.");

            _calls.Remove(callId);
            foreach (CableId cableId in call.Cables)
            {
                var callIds = _callsByCable[cableId];
                callIds.Remove(callId);
                if (callIds.Count == 0)
                    _callsByCable.Remove(cableId);
            }

            foreach (NodeId nodeId in call.Nodes)
            {
                int count = _callCountByNode[nodeId] - 1;
                if (count == 0)
                    _callCountByNode.Remove(nodeId);
                else
                    _callCountByNode[nodeId] = count;
            }

            return call;
        }

        private void NotifyLoadChanged(Call call)
        {
            // Kabel lub węzeł mógł już zniknąć ze struktury (rozmowa zerwana przy usuwaniu).
            if (CableLoadChanged != null)
            {
                foreach (CableId cableId in call.Cables)
                {
                    if (_network.ContainsCable(cableId))
                        CableLoadChanged(cableId);
                }
            }

            if (NodeLoadChanged != null)
            {
                foreach (NodeId nodeId in call.Nodes)
                {
                    if (_network.ContainsNode(nodeId))
                        NodeLoadChanged(nodeId);
                }
            }
        }

        /// <summary>
        /// Najkrótsza trasa (suma <see cref="Cable.Length"/>, algorytm Dijkstry) po kablach z wolną żyłą
        /// i przez węzły, które mają wolną linię. Końce trasy sprawdza wcześniej <see cref="TryStartCall"/>.
        /// </summary>
        private bool TryFindRoute(NodeId fromNodeId, NodeId toNodeId,
            out List<NodeId> nodes, out List<CableId> cables, out float length)
        {
            nodes = null;
            cables = null;
            length = 0f;

            var distances = new Dictionary<NodeId, float> { [fromNodeId] = 0f };
            var cableToNode = new Dictionary<NodeId, CableId>();
            var visited = new HashSet<NodeId>();
            // Zwykła lista zamiast kolejki priorytetowej: przy setkach węzłów to wystarczy (TECH 8).
            var open = new List<NodeId> { fromNodeId };

            while (open.Count > 0)
            {
                NodeId current = TakeClosest(open, distances);
                if (current == toNodeId)
                {
                    BuildRoute(fromNodeId, toNodeId, cableToNode, out nodes, out cables);
                    length = distances[toNodeId];
                    return true;
                }

                visited.Add(current);
                foreach (CableId cableId in _network.GetCableIds(current))
                {
                    if (GetFreePairs(cableId) < 1)
                        continue;

                    var cable = _network.GetCable(cableId);
                    NodeId next = cable.GetOtherEnd(current);
                    if (visited.Contains(next) || !CanPass(_network.GetNode(next)))
                        continue;

                    float distance = distances[current] + cable.Length;
                    if (!distances.TryGetValue(next, out float known))
                        open.Add(next);
                    else if (distance >= known)
                        continue;

                    distances[next] = distance;
                    cableToNode[next] = cableId;
                }
            }

            return false;
        }

        private static NodeId TakeClosest(List<NodeId> open, Dictionary<NodeId, float> distances)
        {
            int best = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (distances[open[i]] < distances[open[best]])
                    best = i;
            }

            var nodeId = open[best];
            open.RemoveAt(best);
            return nodeId;
        }

        private void BuildRoute(NodeId fromNodeId, NodeId toNodeId, Dictionary<NodeId, CableId> cableToNode,
            out List<NodeId> nodes, out List<CableId> cables)
        {
            nodes = new List<NodeId> { toNodeId };
            cables = new List<CableId>();
            NodeId current = toNodeId;
            while (current != fromNodeId)
            {
                CableId cableId = cableToNode[current];
                cables.Add(cableId);
                current = _network.GetCable(cableId).GetOtherEnd(current);
                nodes.Add(current);
            }

            nodes.Reverse();
            cables.Reverse();
        }
    }
}
