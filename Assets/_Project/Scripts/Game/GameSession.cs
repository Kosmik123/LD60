using SpaghettiWires.Simulation;
using SpaghettiWires.UI;
using UnityEngine;

namespace SpaghettiWires.Game
{
    /// <summary>
    /// Jedna rozgrywka: tworzy ekonomię, liczy czas gry i łączy oba z HUD i ekranem przegranej.
    /// Inne systemy (koszty budowy, rozmowy) biorą stąd <see cref="Economy"/>.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        [Header("Start")]
        [SerializeField] private int startMoney = 1000;
        [Range(0.01f, 1f)] [SerializeField] private float startReputation = 1f;

        [Header("UI")]
        [SerializeField] private HudView hud;
        [SerializeField] private GameOverView gameOver;

        private int _shownSeconds = -1;

        public Economy Economy { get; private set; }

        /// <summary>Czas od początku gry. Zatrzymuje się w chwili przegranej.</summary>
        public float ElapsedTime { get; private set; }

        private void Awake()
        {
            Economy = new Economy(startMoney, startReputation);
            Economy.MoneyChanged += hud.SetMoney;
            Economy.ReputationChanged += hud.SetReputation;
            Economy.Lost += OnLost;

            hud.SetMoney(Economy.Money);
            hud.SetReputation(Economy.Reputation);
            hud.SetTime(0f);
        }

        private void OnDestroy()
        {
            Economy.MoneyChanged -= hud.SetMoney;
            Economy.ReputationChanged -= hud.SetReputation;
            Economy.Lost -= OnLost;
        }

        private void Update()
        {
            if (Economy.IsLost)
                return;

            ElapsedTime += Time.deltaTime;

            // HUD pokazuje pełne sekundy, więc tekst zmieniamy raz na sekundę, a nie co klatkę.
            int seconds = (int)ElapsedTime;
            if (seconds != _shownSeconds)
            {
                _shownSeconds = seconds;
                hud.SetTime(ElapsedTime);
            }
        }

        private void OnLost(LossReason reason)
        {
            hud.SetTime(ElapsedTime);
            gameOver.Show(ElapsedTime, Describe(reason));
        }

        private static string Describe(LossReason reason) => reason switch
        {
            LossReason.Reputation => "Reputacja spadła do zera.",
            LossReason.Money => "Skończyły się pieniądze.",
            _ => null,
        };

        #region Debug

        // Do sprawdzania HUD i ekranu przegranej z menu komponentu, zanim podłączymy koszty i rozmowy.
        [ContextMenu("Debug/+100 $")]
        private void DebugEarn() => Economy?.Earn(100);

        [ContextMenu("Debug/Kup za 250 $")]
        private void DebugSpend() => Economy?.TrySpend(250);

        [ContextMenu("Debug/Kara 500 $")]
        private void DebugCharge() => Economy?.Charge(500);

        [ContextMenu("Debug/-10% reputacji")]
        private void DebugLoseReputation() => Economy?.ChangeReputation(-0.1f);

        [ContextMenu("Debug/+10% reputacji")]
        private void DebugGainReputation() => Economy?.ChangeReputation(0.1f);

        #endregion
    }
}
