using UnityEngine;
using UnityEngine.UIElements;

namespace SpaghettiWires.UI
{
    /// <summary>
    /// HUD (GDD 3.8): pieniądze i reputacja w lewym górnym rogu, czas gry w prawym górnym.
    /// Tylko wyświetla wartości. Ustawia je ekonomia i symulacja (PLAN etap 6).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudView : MonoBehaviour
    {
        private const string LowReputationClass = "hud-meter--low";

        [SerializeField] private StyleSheet styleSheet;
        [Tooltip("Poniżej tej reputacji (0–1) pasek robi się czerwony.")]
        [Range(0f, 1f)] [SerializeField] private float lowReputation = 0.25f;

        private Label _money;
        private Label _reputation;
        private VisualElement _reputationMeter;
        private VisualElement _reputationFill;
        private Label _time;

        // Wartości pamiętane osobno, żeby przetrwały wyłączenie i włączenie widoku.
        private int _moneyValue;
        private float _reputationValue = 1f;
        private float _timeValue;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            root.styleSheets.Add(styleSheet);
            root.pickingMode = PickingMode.Ignore;

            var top = new VisualElement { pickingMode = PickingMode.Ignore };
            top.AddToClassList("hud-top");
            root.Add(top);

            var left = AddPanel(top);
            _money = AddRow(left, "PIENIĄDZE");
            _reputation = AddRow(left, "REPUTACJA");
            _reputationMeter = new VisualElement();
            _reputationMeter.AddToClassList("hud-meter");
            _reputationFill = new VisualElement();
            _reputationFill.AddToClassList("hud-meter__fill");
            _reputationMeter.Add(_reputationFill);
            left.Add(_reputationMeter);

            var right = AddPanel(top);
            _time = AddRow(right, "CZAS");

            SetMoney(_moneyValue);
            SetReputation(_reputationValue);
            SetTime(_timeValue);
        }

        public void SetMoney(int amount)
        {
            _moneyValue = amount;
            if (_money != null)
                _money.text = HudFormat.Money(amount);
        }

        /// <param name="normalized">Reputacja od 0 (przegrana) do 1.</param>
        public void SetReputation(float normalized)
        {
            _reputationValue = Mathf.Clamp01(normalized);
            if (_reputation == null)
                return;

            _reputation.text = HudFormat.Percent(_reputationValue);
            _reputationFill.style.width = Length.Percent(_reputationValue * 100f);
            _reputationMeter.EnableInClassList(LowReputationClass, _reputationValue < lowReputation);
        }

        /// <param name="seconds">Czas od początku gry.</param>
        public void SetTime(float seconds)
        {
            _timeValue = seconds;
            if (_time != null)
                _time.text = HudFormat.Time(seconds);
        }

        private static VisualElement AddPanel(VisualElement parent)
        {
            var panel = new VisualElement();
            panel.AddToClassList("hud-panel");
            parent.Add(panel);
            return panel;
        }

        /// <summary>Wiersz „podpis – wartość”. Zwraca etykietę wartości.</summary>
        private static Label AddRow(VisualElement panel, string caption)
        {
            var row = new VisualElement();
            row.AddToClassList("hud-row");

            var captionLabel = new Label(caption);
            captionLabel.AddToClassList("hud-caption");
            row.Add(captionLabel);

            var value = new Label();
            value.AddToClassList("hud-value");
            row.Add(value);

            panel.Add(row);
            return value;
        }
    }
}
