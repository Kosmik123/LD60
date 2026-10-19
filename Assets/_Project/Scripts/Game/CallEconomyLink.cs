using SpaghettiWires.Simulation;
using UnityEngine;

namespace SpaghettiWires.Game
{
    /// <summary>
    /// Na scenie łączy symulację rozmów z ekonomią gry (<see cref="CallPayouts"/>)
    /// i zatrzymuje symulację w chwili przegranej, razem z zegarem <see cref="GameSession"/>.
    /// </summary>
    public sealed class CallEconomyLink : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private CallSimulationRunner simulation;
        [SerializeField] private CallPayoutSettings payouts = new CallPayoutSettings();

        private CallPayouts _payouts;

        // W Start, bo ekonomia powstaje w Awake sesji.
        private void Start()
        {
            _payouts = new CallPayouts(simulation.Simulation, session.Economy, payouts);
            session.Economy.Lost += OnLost;
        }

        private void OnDestroy()
        {
            _payouts?.Dispose();
            if (session != null && session.Economy != null)
                session.Economy.Lost -= OnLost;
        }

        // Po przegranej nikt już nie dzwoni, a trwające rozmowy zamierają razem z grą.
        private void OnLost(LossReason reason) => simulation.enabled = false;
    }
}
