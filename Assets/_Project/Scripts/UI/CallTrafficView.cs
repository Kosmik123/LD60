using SpaghettiWires.Networking;
using SpaghettiWires.Simulation;
using SpaghettiWires.Views;
using UnityEngine;

namespace SpaghettiWires.UI
{
    /// <summary>Kable, którymi idzie rozmowa, świecą (PLAN etap 5). Reaguje na zmiany zajętości żył w sieci.</summary>
    public sealed class CallTrafficView : MonoBehaviour
    {
        [SerializeField] private CallSimulationRunner simulation;
        [SerializeField] private ViewRegistry views;

        private Traffic _traffic;

        // W Start, a nie w OnEnable: symulacja tworzy sieć dopiero przy pierwszym użyciu.
        private void Start()
        {
            _traffic = simulation.Simulation.Network.Traffic;
            _traffic.CableLoadChanged += OnCableLoadChanged;
        }

        private void OnDestroy()
        {
            if (_traffic != null)
                _traffic.CableLoadChanged -= OnCableLoadChanged;
        }

        private void OnCableLoadChanged(CableId cableId)
        {
            if (views.TryGetCable(cableId.Value, out var cable))
                cable.SetInCall(_traffic.GetBusyPairs(cableId) > 0);
        }
    }
}
