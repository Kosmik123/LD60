using System;
using System.Collections.Generic;

namespace SpaghettiWires.Network
{
    /// <summary>
    /// Szukanie najkrótszej trasy po kablach (algorytm Dijkstry, TECH 4.3), czyli o najmniejszej
    /// sumie <see cref="Cable.Length"/>. Sieć nie zna żył ani zajętości, więc to,
    /// które kable i węzły są dostępne, przychodzi z zewnątrz.
    /// </summary>
    public static class CablePathfinder
    {
        /// <param name="canUseCable">Czy rozmowa może iść tym kablem (np. ma wolną żyłę). Brak = każdy.</param>
        /// <param name="canUseNode">
        /// Czy trasa może zawierać ten węzeł (np. dom nie jest zajęty).
        /// Sprawdzany także dla węzła początkowego i docelowego. Brak = każdy.
        /// </param>
        public static bool TryFindPath(
            CableNetwork network,
            int fromNodeId,
            int toNodeId,
            out NetworkPath path,
            Func<Cable, bool> canUseCable = null,
            Func<Node, bool> canUseNode = null)
        {
            path = null;
            var fromNode = network.GetNode(fromNodeId);
            var toNode = network.GetNode(toNodeId);
            if (canUseNode != null && (!canUseNode(fromNode) || !canUseNode(toNode)))
                return false;

            var distances = new Dictionary<int, float> { [fromNodeId] = 0f };
            var cableToNode = new Dictionary<int, int>();
            var visited = new HashSet<int>();
            // Zwykła lista zamiast kolejki priorytetowej: przy setkach węzłów to wystarczy (TECH 8).
            var open = new List<int> { fromNodeId };

            while (open.Count > 0)
            {
                int current = TakeClosest(open, distances);
                if (current == toNodeId)
                {
                    path = BuildPath(network, fromNodeId, toNodeId, cableToNode, distances[toNodeId]);
                    return true;
                }

                visited.Add(current);
                foreach (int cableId in network.GetCableIds(current))
                {
                    var cable = network.GetCable(cableId);
                    if (canUseCable != null && !canUseCable(cable))
                        continue;

                    int next = cable.GetOtherEnd(current);
                    if (visited.Contains(next))
                        continue;
                    if (canUseNode != null && !canUseNode(network.GetNode(next)))
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

        private static int TakeClosest(List<int> open, Dictionary<int, float> distances)
        {
            int best = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (distances[open[i]] < distances[open[best]])
                    best = i;
            }

            int nodeId = open[best];
            open.RemoveAt(best);
            return nodeId;
        }

        private static NetworkPath BuildPath(
            CableNetwork network, int fromNodeId, int toNodeId, Dictionary<int, int> cableToNode, float length)
        {
            var nodes = new List<int> { toNodeId };
            var cables = new List<int>();
            int current = toNodeId;
            while (current != fromNodeId)
            {
                int cableId = cableToNode[current];
                cables.Add(cableId);
                current = network.GetCable(cableId).GetOtherEnd(current);
                nodes.Add(current);
            }

            nodes.Reverse();
            cables.Reverse();
            return new NetworkPath(nodes, cables, length);
        }
    }
}
