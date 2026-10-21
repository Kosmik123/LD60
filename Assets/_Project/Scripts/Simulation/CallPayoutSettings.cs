using System;
using UnityEngine;

namespace SpaghettiWires.Simulation
{
    /// <summary>
    /// Ile gracz zarabia i ile reputacji zyskuje lub traci za wynik rozmowy (GDD 3.8).
    /// Wartości robocze, do ustalenia w testach. Reputacja jest w skali 0–1.
    /// </summary>
    [Serializable]
    public sealed class CallPayoutSettings
    {
        [Tooltip("Pieniądze za rozmowę zakończoną normalnie.")]
        [Min(0)] [SerializeField] private int moneyPerCompletedCallPerSecond = 3;
        [Tooltip("Zmiana reputacji za rozmowę zakończoną normalnie.")]
        [SerializeField] private float reputationPerCompletedCall = 0.01f;
        [Tooltip("Zmiana reputacji, gdy nie ma trasy z wolnymi żyłami.")]
        [SerializeField] private float reputationPerNoRoute = -0.02f;
        [Tooltip("Zmiana reputacji, gdy linia odbiorcy jest zajęta.")]
        [SerializeField] private float reputationPerCalleeBusy = -0.01f;
        [Tooltip("Zmiana reputacji, gdy linia dzwoniącego jest zajęta (np. przechodzi przez niego cudza rozmowa).")]
        [SerializeField] private float reputationPerCallerBusy = -0.01f;
        [Tooltip("Zmiana reputacji za rozmowę zerwaną przez awarię lub usunięcie kabla.")]
        [SerializeField] private float reputationPerDroppedCall = -0.04f;

        public int MoneyPerCompletedCallPerSecond => moneyPerCompletedCallPerSecond;
        public float ReputationPerCompletedCall => reputationPerCompletedCall;
        public float ReputationPerDroppedCall => reputationPerDroppedCall;

        public CallPayoutSettings() { }

        public CallPayoutSettings(int moneyPerCompletedCallPerSecond, float reputationPerCompletedCall, float reputationPerNoRoute,
            float reputationPerCalleeBusy, float reputationPerCallerBusy, float reputationPerDroppedCall)
        {
            this.moneyPerCompletedCallPerSecond = moneyPerCompletedCallPerSecond;
            this.reputationPerCompletedCall = reputationPerCompletedCall;
            this.reputationPerNoRoute = reputationPerNoRoute;
            this.reputationPerCalleeBusy = reputationPerCalleeBusy;
            this.reputationPerCallerBusy = reputationPerCallerBusy;
            this.reputationPerDroppedCall = reputationPerDroppedCall;
        }

        /// <summary>Zmiana reputacji za nieudaną próbę połączenia.</summary>
        public float ReputationPerFailure(CallOutcome outcome) => outcome switch
        {
            CallOutcome.NoRoute => reputationPerNoRoute,
            CallOutcome.CalleeBusy => reputationPerCalleeBusy,
            CallOutcome.CallerBusy => reputationPerCallerBusy,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Not a failed call."),
        };
    }
}
