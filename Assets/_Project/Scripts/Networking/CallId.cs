using System;

namespace SpaghettiWires.Networking
{
    /// <summary>
    /// Id rozmowy. Osobny typ, żeby nie dało się go pomylić z innymi Id ani z liczbą.
    /// Nadaje go sieć. Wartość 0 (domyślna) nie wskazuje żadnego obiektu.
    /// </summary>
    public readonly struct CallId : IEquatable<CallId>
    {
        public int Value { get; }

        public CallId(int value)
        {
            Value = value;
        }

        public bool Equals(CallId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is CallId other && Equals(other);
        public override int GetHashCode() => Value;

        public static bool operator ==(CallId left, CallId right) => left.Value == right.Value;
        public static bool operator !=(CallId left, CallId right) => left.Value != right.Value;

        public override string ToString() => $"Call#{Value}";
    }
}
