using System;

namespace SpaghettiWires.Network
{
    /// <summary>
    /// Kabel między dwoma węzłami. Niezmienny po położeniu (TECH 4.2).
    /// Magistrala to kabel z <see cref="Pairs"/> &gt; 1.
    /// </summary>
    public sealed class Cable
    {
        public int Id { get; }
        public int A { get; }
        public int B { get; }

        /// <summary>Liczba żył, czyli ile rozmów kabel przenosi naraz.</summary>
        public int Pairs { get; }

        /// <summary>Waga do wyboru najkrótszej trasy. Ustawiana przez reguły z pozycji węzłów.</summary>
        public float Length { get; }

        public Cable(int id, int a, int b, int pairs, float length)
        {
            Id = id;
            A = a;
            B = b;
            Pairs = pairs;
            Length = length;
        }

        public bool IsConnectedTo(int nodeId) => A == nodeId || B == nodeId;

        /// <summary>Zwraca węzeł na drugim końcu kabla.</summary>
        public int GetOtherEnd(int nodeId)
        {
            if (nodeId == A)
                return B;
            if (nodeId == B)
                return A;
            throw new ArgumentException($"Cable #{Id} is not connected to node #{nodeId}.", nameof(nodeId));
        }

        public override string ToString() => $"Cable#{Id} ({A}-{B}, pairs: {Pairs}, length: {Length})";
    }
}
