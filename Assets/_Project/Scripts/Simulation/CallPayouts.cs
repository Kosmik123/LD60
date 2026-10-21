using System;
using SpaghettiWires.Networking;

namespace SpaghettiWires.Simulation
{
    /// <summary>
    /// Łączy wyniki rozmów z ekonomią (PLAN etap 6): pieniądze i reputacja za udaną rozmowę,
    /// kary w reputacji za „zajęte”, brak połączenia i zerwanie.
    /// </summary>
    public sealed class CallPayouts : IDisposable
    {
        private readonly CallSimulation _simulation;
        private readonly Economy _economy;
        private readonly CallPayoutSettings _settings;

        public CallPayouts(CallSimulation simulation, Economy economy, CallPayoutSettings settings)
        {
            _simulation = simulation;
            _economy = economy;
            _settings = settings;

            _simulation.CallCompleted += OnCallCompleted;
            _simulation.CallFailed += OnCallFailed;
            _simulation.CallDropped += OnCallDropped;
        }

        public void Dispose()
        {
            _simulation.CallCompleted -= OnCallCompleted;
            _simulation.CallFailed -= OnCallFailed;
            _simulation.CallDropped -= OnCallDropped;
        }

        private void OnCallCompleted(Call call)
        {
            _economy.Earn(_settings.MoneyPerCompletedCallPerSecond * call.d);
            _economy.ChangeReputation(_settings.ReputationPerCompletedCall);
        }

        private void OnCallFailed(NodeId from, NodeId to, CallOutcome outcome) =>
            _economy.ChangeReputation(_settings.ReputationPerFailure(outcome));

        private void OnCallDropped(Call call) =>
            _economy.ChangeReputation(_settings.ReputationPerDroppedCall);
    }
}
