using System.Collections.Generic;

namespace SpaghettiWires.Network
{
    /// <summary>Trasa między dwoma węzłami znaleziona przez <see cref="CablePathfinder"/>.</summary>
    public sealed class NetworkPath
    {
        /// <summary>Węzły po kolei, od początkowego do docelowego (oba włącznie).</summary>
        public IReadOnlyList<int> Nodes { get; }

        /// <summary>Kable po kolei. Kabel <c>i</c> łączy węzły <c>i</c> oraz <c>i + 1</c>.</summary>
        public IReadOnlyList<int> Cables { get; }

        /// <summary>Suma długości kabli na trasie.</summary>
        public float Length { get; }

        public NetworkPath(IReadOnlyList<int> nodes, IReadOnlyList<int> cables, float length)
        {
            Nodes = nodes;
            Cables = cables;
            Length = length;
        }
    }
}
