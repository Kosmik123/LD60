using System;
using System.Collections.Generic;

namespace SpaghettiWires.Network
{
    /// <summary>
    /// Stan techniczny sieci (TECH 4.1): ile żył każdego kabla jest uszkodzonych.
    /// Uszkodzenia tylko przybywają, aż do usunięcia kabla. Nie pamiętamy, które żyły padły,
    /// bo wszystkie są równoważne.
    /// </summary>
    public sealed class CableCondition
    {
        private readonly CableNetwork _network;

        // Brak wpisu = kabel sprawny.
        private readonly Dictionary<int, int> _damagedPairs = new Dictionary<int, int>();

        /// <summary>Kabel stracił żyły. Parametry: Id kabla, liczba uszkodzonych żył po zmianie.</summary>
        public event Action<int, int> PairsDamaged;

        /// <summary>Id kabli z co najmniej jedną uszkodzoną żyłą (np. do wskaźnika chaosu).</summary>
        public IReadOnlyCollection<int> DamagedCableIds => _damagedPairs.Keys;

        public CableCondition(CableNetwork network)
        {
            _network = network;
            _network.CableRemoved += cable => _damagedPairs.Remove(cable.Id);
        }

        /// <summary>
        /// Uszkadza kolejne żyły kabla, najwyżej do <see cref="Cable.Pairs"/>.
        /// Zdarzenie jest wysyłane tylko wtedy, gdy coś się zmieniło.
        /// </summary>
        public void Damage(int cableId, int pairs)
        {
            if (pairs < 1)
                throw new ArgumentOutOfRangeException(nameof(pairs), pairs, "At least one pair must be damaged.");

            var cable = _network.GetCable(cableId);
            int before = GetDamagedPairs(cableId);
            int after = Math.Min(cable.Pairs, before + Math.Min(pairs, cable.Pairs));
            if (after == before)
                return;

            _damagedPairs[cableId] = after;
            PairsDamaged?.Invoke(cableId, after);
        }

        /// <summary>Całkowite zerwanie: uszkadza wszystkie żyły kabla.</summary>
        public void Break(int cableId) => Damage(cableId, _network.GetCable(cableId).Pairs);

        public int GetDamagedPairs(int cableId)
        {
            if (!_network.ContainsCable(cableId))
                throw new KeyNotFoundException($"Unknown cable #{cableId}.");
            return _damagedPairs.TryGetValue(cableId, out int damaged) ? damaged : 0;
        }

        /// <summary>Liczba sprawnych żył, czyli wynik pomiaru miernikiem.</summary>
        public int GetWorkingPairs(int cableId) => _network.GetCable(cableId).Pairs - GetDamagedPairs(cableId);

        public bool IsBroken(int cableId) => GetWorkingPairs(cableId) == 0;
    }
}
