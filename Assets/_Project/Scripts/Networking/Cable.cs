using System;

namespace SpaghettiWires.Networking
{
    /// <summary>
    /// Kabel między dwoma węzłami. Niezmienny po położeniu (TECH 4.2).
    /// Magistrala to kabel z <see cref="Pairs"/> &gt; 1.
    /// </summary>
    public sealed class Cable
    {
        public CableId Id { get; }
        public NodeId A { get; }
        public NodeId B { get; }

        /// <summary>Liczba żył, czyli ile rozmów kabel przenosi naraz.</summary>
        public int Pairs { get; }

        /// <summary>Waga do wyboru najkrótszej trasy. Ustawiana przez reguły z pozycji węzłów.</summary>
        public float Length { get; }

        public Cable(CableId id, NodeId a, NodeId b, int pairs, float length)
        {
            Id = id;
            A = a;
            B = b;
            Pairs = pairs;
            Length = length;
        }

        public bool IsConnectedTo(NodeId nodeId) => A == nodeId || B == nodeId;

        /// <summary>Zwraca węzeł na drugim końcu kabla.</summary>
        public NodeId GetOtherEnd(NodeId nodeId)
        {
            if (nodeId == A)
                return B;
            if (nodeId == B)
                return A;
            throw new ArgumentException($"{Id} is not connected to {nodeId}.", nameof(nodeId));
        }

        public override string ToString() => $"{Id} ({A}-{B}, pairs: {Pairs}, length: {Length})";
    }
}
