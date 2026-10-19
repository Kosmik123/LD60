using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SpaghettiWires.UI
{
    /// <summary>
    /// Ekran przegranej (GDD 3.8) z czasem przetrwania i restartem.
    /// Domyślnie ukryty. Pokazuje go ten, kto wykryje przegraną (PLAN etap 6), przez <see cref="Show"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameOverView : MonoBehaviour
    {
        [SerializeField] private StyleSheet styleSheet;

        private VisualElement _overlay;
        private Label _reason;
        private Label _time;

        public bool IsVisible { get; private set; }

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            root.styleSheets.Add(styleSheet);
            root.pickingMode = PickingMode.Ignore;

            // Zasłona łapie mysz, więc gdy ekran jest widoczny, scena nie reaguje na kliknięcia.
            _overlay = new VisualElement();
            _overlay.AddToClassList("game-over");
            root.Add(_overlay);

            var panel = new VisualElement();
            panel.AddToClassList("game-over__panel");
            _overlay.Add(panel);

            panel.Add(CreateLabel("Koniec gry", "game-over__title"));
            _reason = CreateLabel(string.Empty, "game-over__reason");
            panel.Add(_reason);
            panel.Add(CreateLabel("CZAS PRZETRWANIA", "game-over__caption"));
            _time = CreateLabel(string.Empty, "game-over__time");
            panel.Add(_time);

            var restart = new Button(Restart) { text = "Zagraj ponownie" };
            restart.AddToClassList("game-over__button");
            panel.Add(restart);

            SetVisible(IsVisible);
        }

        /// <param name="survivedSeconds">Czas od początku gry do przegranej.</param>
        /// <param name="reason">Np. „Reputacja spadła do zera.”. Pusty = bez opisu.</param>
        public void Show(float survivedSeconds, string reason = null)
        {
            _time.text = HudFormat.Time(survivedSeconds);
            _reason.text = reason ?? string.Empty;
            _reason.style.display = string.IsNullOrEmpty(reason) ? DisplayStyle.None : DisplayStyle.Flex;
            SetVisible(true);
        }

        public void Hide() => SetVisible(false);

        /// <summary>Wczytuje scenę od nowa.</summary>
        public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        private void SetVisible(bool visible)
        {
            IsVisible = visible;
            _overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static Label CreateLabel(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }
    }
}
