using System;

namespace SpaghettiWires.Networking
{
    /// <summary>
    /// Id węzła. Osobny typ, żeby nie dało się go pomylić z innymi Id ani z liczbą.
    /// Nadaje go sieć. Wartość 0 (domyślna) nie wskazuje żadnego obiektu.
    /// </summary>
    public readonly struct NodeId : IEquatable<NodeId>
    {
        public int Value { get; }

        public NodeId(int value)
        {
            Value = value;
        }

        public bool Equals(NodeId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is NodeId other && Equals(other);
        public override int GetHashCode() => Value;

        public static bool operator ==(NodeId left, NodeId right) => left.Value == right.Value;
        public static bool operator !=(NodeId left, NodeId right) => left.Value != right.Value;

        public override string ToString() => $"Node#{Value}";
    }
}
