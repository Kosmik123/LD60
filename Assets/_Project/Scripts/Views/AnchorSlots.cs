using System;
using System.Collections.Generic;

namespace SpaghettiWires.Views
{
    /// <summary>
    /// Miejsca na kable w węźle (GDD 3.3). Dwa niezależne limity:
    /// <list type="bullet">
    /// <item><b>pojemność węzła</b>: suma kosztów kabli, gdzie kabel kosztuje ⌈√(liczba żył)⌉,</item>
    /// <item><b>miejsca na punkcie przyczepu</b>: każdy kabel, także magistrala, zajmuje jedno miejsce na jednym punkcie.</item>
    /// </list>
    /// Magistrala kosztuje więcej niż jedno miejsce na punkcie, więc pojemność może się skończyć,
    /// zanim zapełnią się wszystkie punkty.
    /// </summary>
    public sealed class AnchorSlots
    {
        /// <summary>Wartość limitu oznaczająca brak limitu.</summary>
        public const int Unlimited = 0;

        private readonly int[] _placesUsed;
        private readonly Dictionary<int, (int anchor, int cost)> _cables = new Dictionary<int, (int, int)>();

        public int AnchorCount => _placesUsed.Length;
        public int Capacity { get; }
        public int PlacesPerAnchor { get; }
        public bool HasCapacityLimit => Capacity != Unlimited;

        /// <summary>Suma kosztów przyczepionych kabli.</summary>
        public int Used { get; private set; }

        public AnchorSlots(int anchorCount, int capacity, int placesPerAnchor)
        {
            if (anchorCount < 1)
                throw new ArgumentOutOfRangeException(nameof(anchorCount), anchorCount, "Node needs at least one anchor.");

            _placesUsed = new int[anchorCount];
            Capacity = Math.Max(capacity, Unlimited);
            PlacesPerAnchor = Math.Max(placesPerAnchor, Unlimited);
        }

        /// <summary>Ile miejsc pojemności zajmuje kabel: ⌈√(liczba żył)⌉ (zwykły kabel 1, magistrala 4 → 2).</summary>
        public static int CostOf(int pairs) => (int)Math.Ceiling(Math.Sqrt(pairs));

        public bool CanAttach(int pairs) =>
            (!HasCapacityLimit || Used + CostOf(pairs) <= Capacity) && HasFreeAnchor();

        public bool IsAnchorFree(int anchor) => PlacesPerAnchor == Unlimited || _placesUsed[anchor] < PlacesPerAnchor;

        public int PlacesUsedAt(int anchor) => _placesUsed[anchor];

        public bool Contains(int cableId) => _cables.ContainsKey(cableId);

        public void Attach(int cableId, int pairs, int anchor)
        {
            if (_cables.ContainsKey(cableId))
                throw new InvalidOperationException($"Cable #{cableId} is already attached.");
            if (!CanAttach(pairs))
                throw new InvalidOperationException($"No room for cable #{cableId} ({pairs} pairs).");
            if (!IsAnchorFree(anchor))
                throw new InvalidOperationException($"Anchor {anchor} has no free place.");

            int cost = CostOf(pairs);
            _cables.Add(cableId, (anchor, cost));
            _placesUsed[anchor]++;
            Used += cost;
        }

        public void Detach(int cableId)
        {
            if (!_cables.Remove(cableId, out var attachment))
                return;

            _placesUsed[attachment.anchor]--;
            Used -= attachment.cost;
        }

        public bool TryGetAnchor(int cableId, out int anchor)
        {
            bool found = _cables.TryGetValue(cableId, out var attachment);
            anchor = attachment.anchor;
            return found;
        }

        private bool HasFreeAnchor()
        {
            for (int i = 0; i < _placesUsed.Length; i++)
            {
                if (IsAnchorFree(i))
                    return true;
            }
            return false;
        }
    }
}
