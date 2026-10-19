using System;
using System.Collections.Generic;

namespace SpaghettiWires.Networking
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
        private readonly Dictionary<CableId, int> _damagedPairs = new Dictionary<CableId, int>();

        /// <summary>Kabel stracił żyły. Parametry: Id kabla, liczba uszkodzonych żył po zmianie.</summary>
        public event Action<CableId, int> PairsDamaged;

        /// <summary>Id kabli z co najmniej jedną uszkodzoną żyłą (np. do wskaźnika chaosu).</summary>
        public IReadOnlyCollection<CableId> DamagedCableIds => _damagedPairs.Keys;

        public CableCondition(CableNetwork network)
        {
            _network = network;
            _network.CableRemoved += cable => _damagedPairs.Remove(cable.Id);
        }

        /// <summary>
        /// Uszkadza kolejne żyły kabla, najwyżej do <see cref="Cable.Pairs"/>.
        /// Zdarzenie jest wysyłane tylko wtedy, gdy coś się zmieniło.
        /// </summary>
        public void Damage(CableId cableId, int pairs)
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
        public void Break(CableId cableId) => Damage(cableId, _network.GetCable(cableId).Pairs);

        public int GetDamagedPairs(CableId cableId)
        {
            if (!_network.ContainsCable(cableId))
                throw new KeyNotFoundException($"Unknown {cableId}.");
            return _damagedPairs.TryGetValue(cableId, out int damaged) ? damaged : 0;
        }

        /// <summary>Liczba sprawnych żył, czyli wynik pomiaru miernikiem.</summary>
        public int GetWorkingPairs(CableId cableId) => _network.GetCable(cableId).Pairs - GetDamagedPairs(cableId);

        public bool IsBroken(CableId cableId) => GetWorkingPairs(cableId) == 0;
    }
}
