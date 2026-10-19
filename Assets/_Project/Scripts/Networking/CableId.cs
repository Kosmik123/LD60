using System;

namespace SpaghettiWires.Networking
{
    /// <summary>
    /// Id kabla. Osobny typ, żeby nie dało się go pomylić z innymi Id ani z liczbą.
    /// Nadaje go sieć. Wartość 0 (domyślna) nie wskazuje żadnego obiektu.
    /// </summary>
    public readonly struct CableId : IEquatable<CableId>
    {
        public int Value { get; }

        public CableId(int value)
        {
            Value = value;
        }

        public bool Equals(CableId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is CableId other && Equals(other);
        public override int GetHashCode() => Value;

        public static bool operator ==(CableId left, CableId right) => left.Value == right.Value;
        public static bool operator !=(CableId left, CableId right) => left.Value != right.Value;

        public override string ToString() => $"Cable#{Value}";
    }
}
