using UnityEngine;

namespace SpaghettiWires.Tools
{
    /// <summary>
    /// Wynik sprawdzenia akcji gracza: czy jest dozwolona, a jeśli nie, to dlaczego.
    /// </summary>
    public readonly struct BuildCheck
    {
        public bool IsValid { get; }

        /// <summary>Powód odmowy do pokazania graczowi. Pusty, gdy akcja jest dozwolona.</summary>
        public string Reason { get; }

        private BuildCheck(bool isValid, string reason)
        {
            IsValid = isValid;
            Reason = reason;
        }

        public static BuildCheck Ok => new BuildCheck(true, string.Empty);
        public static BuildCheck Fail(string reason) => new BuildCheck(false, reason);
    }

    /// <summary>
    /// Akcje, które narzędzia gracza zlecają grze. To granica między narzędziami a warstwą reguł (TECH 3, 5):
    /// narzędzia tylko pytają i zlecają, a o tym, co jest dozwolone i co się dzieje w sieci, decyduje implementacja.
    /// Metody Check* nie mogą zmieniać stanu gry, bo są wołane co klatkę do podglądu.
    /// </summary>
    public interface IBuildActions
    {
        BuildCheck CheckPole(Vector2Int cell);
        void PlacePole(Vector2Int cell);

        BuildCheck CheckCable(int fromNodeId, int toNodeId, int pairs);
        void LayCable(int fromNodeId, int toNodeId, int pairs);

        /// <summary>
        /// Id, które dostanie kabel położony jako następny. Gra jest dla jednego gracza, więc to Id kabla,
        /// który gracz właśnie przeciąga. Podgląd używa go, żeby pokazać punkt przyczepu, w którym kabel wyląduje.
        /// </summary>
        int NextCableId { get; }

        void RemoveCable(int cableId);

        /// <summary>Miernik: liczba sprawnych żył kabla.</summary>
        int MeasureWorkingPairs(int cableId);

        /// <summary>
        /// Zajętość miejsc na słupie (GDD 3.3): ile miejsc zajmują kable i ile ich jest.
        /// Zwraca false dla węzłów bez limitu miejsc (dom, centrala).
        /// </summary>
        bool TryGetSlotUsage(int nodeId, out int used, out int capacity);
    }
}
