using Bipolar;
using SpaghettiWires.Map;
using SpaghettiWires.Simulation;
using SpaghettiWires.Tools;
using SpaghettiWires.UI;
using SpaghettiWires.Views;
using UnityEngine;

namespace SpaghettiWires.Game
{
    /// <summary>
    /// Płatne akcje gracza: nakładka na inne <see cref="IBuildActions"/> (dziś sandbox, później reguły budowy).
    /// Sprawdza, czy gracza stać, a przy zakupie pobiera pieniądze z <see cref="GameSession.Economy"/>.
    /// Długość kabla to odległość między polami węzłów, bez zwisu.
    /// </summary>
    public sealed class PaidBuildActions : MonoBehaviour, IBuildActions, IBuildPricing
    {
        [SerializeField] private Serialized<IBuildActions> inner;
        [SerializeField] private GameSession session;
        [SerializeField] private MapGridBehaviour grid;
        [SerializeField] private ViewRegistry views;

        [Header("Ceny")]
        [SerializeField] private int polePrice = 100;
        [Tooltip("Cena za metr zwykłego kabla (1 żyła).")]
        [SerializeField] private float cablePricePerMeter = 5f;
        [Tooltip("Cena za metr i żyłę magistrali. Mniejsza niż kabla, więc magistrala jest tańsza w przeliczeniu na żyłę.")]
        [SerializeField] private float trunkPricePerPairMeter = 3.5f;

        private BuildPrices _prices;

        public IBuildActions Inner
        {
            get => inner.Value;
            set => inner.Value = value;
        }

        public BuildPrices Prices => _prices ??= new BuildPrices(polePrice, cablePricePerMeter, trunkPricePerPairMeter);

        private Economy Economy => session.Economy;

        public int NextCableId => Inner.NextCableId;

        public float GetMaxCableLength(int pairs) => Inner.GetMaxCableLength(pairs);

        public BuildCheck CheckPole(Vector2Int cell)
        {
            var check = Inner.CheckPole(cell);
            return check.IsValid ? CheckMoney(Prices.Pole) : check;
        }

        public void PlacePole(Vector2Int cell)
        {
            if (Economy.TrySpend(Prices.Pole))
                Inner.PlacePole(cell);
        }

        public BuildCheck CheckCable(int fromNodeId, int toNodeId, int pairs)
        {
            var check = Inner.CheckCable(fromNodeId, toNodeId, pairs);
            return check.IsValid ? CheckMoney(GetCablePrice(fromNodeId, toNodeId, pairs)) : check;
        }

        public void LayCable(int fromNodeId, int toNodeId, int pairs)
        {
            if (Economy.TrySpend(GetCablePrice(fromNodeId, toNodeId, pairs)))
                Inner.LayCable(fromNodeId, toNodeId, pairs);
        }

        public int GetCablePrice(int fromNodeId, int toNodeId, int pairs) =>
            Prices.Cable(GetCableLength(fromNodeId, toNodeId), pairs);

        public int PolePrice => Prices.Pole;
        public float GetCablePricePerMeter(int pairs) => Prices.PerMeter(pairs);
        public int GetCablePrice(float lengthMeters, int pairs) => Prices.Cable(lengthMeters, pairs);
        public bool CanAfford(int price) => Economy.CanAfford(price);

        /// <summary>Odległość w metrach między polami dwóch węzłów.</summary>
        public float GetCableLength(int fromNodeId, int toNodeId)
        {
            if (!views.TryGetNode(fromNodeId, out var from) || !views.TryGetNode(toNodeId, out var to))
                return 0f;
            return MapGrid.CellDistance(from.Cell, to.Cell) * grid.Grid.CellSize;
        }

        // Usuwanie kabla i pomiar są darmowe.
        public void RemoveCable(int cableId) => Inner.RemoveCable(cableId);
        public int MeasureWorkingPairs(int cableId) => Inner.MeasureWorkingPairs(cableId);
        public bool TryGetSlotUsage(int nodeId, out int used, out int capacity) => Inner.TryGetSlotUsage(nodeId, out used, out capacity);

        private BuildCheck CheckMoney(int price) =>
            Economy.CanAfford(price) ? BuildCheck.Ok : BuildCheck.Fail($"Not enough money. You need {HudFormat.Money(price)}.");

        private void OnValidate() => _prices = null;
    }
}
