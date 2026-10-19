using System.Collections.Generic;

namespace SpaghettiWires.Networking
{
    /// <summary>
    /// Trwająca rozmowa. Trasa jest wybierana raz, przy rozpoczęciu, i nie zmienia się (TECH 4.3).
    /// Id rośnie z każdą rozmową, więc wyznacza też kolejność rozpoczęcia.
    /// </summary>
    public sealed class Call
    {
        public CallId Id { get; }
        public NodeId From { get; }
        public NodeId To { get; }

        /// <summary>Węzły trasy po kolei, od <see cref="From"/> do <see cref="To"/> (oba włącznie).</summary>
        public IReadOnlyList<NodeId> Nodes { get; }

        /// <summary>Kable trasy po kolei. Kabel <c>i</c> łączy węzły <c>i</c> oraz <c>i + 1</c>.</summary>
        public IReadOnlyList<CableId> Cables { get; }

        /// <summary>Suma <see cref="Cable.Length"/> na trasie. Symulacja przelicza ją na jakość rozmowy.</summary>
        public float Length { get; }

        public Call(CallId id, IReadOnlyList<NodeId> nodes, IReadOnlyList<CableId> cables, float length)
        {
            Id = id;
            Nodes = nodes;
            Cables = cables;
            Length = length;
            From = nodes[0];
            To = nodes[nodes.Count - 1];
        }

        public override string ToString() => $"{Id} ({From}->{To}, cables: {Cables.Count}, length: {Length})";
    }
}
