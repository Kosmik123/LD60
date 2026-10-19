using System.Collections.Generic;
using SpaghettiWires.Tools;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaghettiWires.UI
{
    /// <summary>
    /// Pasek narzędzi na dole ekranu (GDD 4) i linia komunikatów nad nim.
    /// Elementy tworzone w kodzie, wygląd w arkuszu USS.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolbarView : MonoBehaviour
    {
        private const string SelectedClass = "tool-button--selected";

        private static readonly (ToolType type, string key, string label)[] Buttons =
        {
            (ToolType.Cable, "1", "Cable"),
            (ToolType.Trunk, "2", "Trunk"),
            (ToolType.Pole, "3", "Pole"),
            (ToolType.Meter, "4", "Meter"),
            (ToolType.Scissors, "5", "Cutters"),
        };

        [SerializeField] private ToolController tools;
        [SerializeField] private StyleSheet styleSheet;
        [Tooltip("Jak długo komunikat zostaje na ekranie (s).")]
        [SerializeField] private float messageDuration = 3f;

        private readonly Dictionary<ToolType, Button> _buttons = new Dictionary<ToolType, Button>();
        private Label _message;
        private Label _hint;
        private ToolType _hovered = ToolType.None;
        private float _messageHideTime;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            root.styleSheets.Add(styleSheet);
            // Pełnoekranowy korzeń nie może łapać myszy, inaczej zasłaniałby całą scenę.
            root.pickingMode = PickingMode.Ignore;

            var hud = new VisualElement { pickingMode = PickingMode.Ignore };
            hud.AddToClassList("hud");
            root.Add(hud);

            _message = new Label { pickingMode = PickingMode.Ignore };
            _message.AddToClassList("message");
            hud.Add(_message);

            // Cena narzędzia pod kursorem, a gdy kursor nie jest nad paskiem, wybranego narzędzia.
            _hint = new Label { pickingMode = PickingMode.Ignore };
            _hint.AddToClassList("tool-hint");
            hud.Add(_hint);

            var bar = new VisualElement();
            bar.AddToClassList("toolbar");
            hud.Add(bar);

            foreach (var (type, key, label) in Buttons)
            {
                var button = new Button(() => tools.Select(tools.Current == type ? ToolType.None : type));
                button.AddToClassList("tool-button");

                var keyLabel = new Label(key);
                keyLabel.AddToClassList("tool-button__key");
                button.Add(keyLabel);
                button.Add(new Label(label));

                button.RegisterCallback<PointerEnterEvent>(_ => SetHovered(type));
                button.RegisterCallback<PointerLeaveEvent>(_ => SetHovered(ToolType.None));

                bar.Add(button);
                _buttons[type] = button;
                button.SetEnabled(tools.IsUnlocked(type));
            }

            tools.ToolChanged += OnToolChanged;
            tools.ToolUnlocked += OnToolUnlocked;
            tools.MessagePosted += OnMessagePosted;
            OnToolChanged(tools.Current);
            _message.visible = false;
        }

        private void OnDisable()
        {
            tools.ToolChanged -= OnToolChanged;
            tools.ToolUnlocked -= OnToolUnlocked;
            tools.MessagePosted -= OnMessagePosted;
        }

        private void Update()
        {
            if (_message.visible && Time.unscaledTime >= _messageHideTime)
                _message.visible = false;
        }

        private void OnToolChanged(ToolType current)
        {
            foreach (var (type, button) in _buttons)
                button.EnableInClassList(SelectedClass, type == current);
            UpdateHint();
        }

        private void OnToolUnlocked(ToolType type)
        {
            if (!_buttons.TryGetValue(type, out var button))
                return;

            button.SetEnabled(true);
            foreach (var (buttonType, key, label) in Buttons)
            {
                if (buttonType == type)
                    OnMessagePosted($"New tool: {label} ({key})");
            }
        }

        private void SetHovered(ToolType type)
        {
            _hovered = type;
            UpdateHint();
        }

        private void UpdateHint()
        {
            string hint = GetPriceHint(_hovered != ToolType.None ? _hovered : tools.Current);
            _hint.text = hint ?? string.Empty;
            _hint.style.display = hint != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Cennik narzędzia, np. „Cable (1 pair): $5/m”. Null, gdy budowa nie jest płatna.</summary>
        private string GetPriceHint(ToolType type)
        {
            var pricing = tools.Pricing;
            if (pricing == null)
                return null;

            return type switch
            {
                ToolType.Cable => $"Cable ({HudFormat.Pairs(1)}): {HudFormat.MoneyPerMeter(pricing.GetCablePricePerMeter(1))}",
                ToolType.Trunk => $"Trunk ({HudFormat.Pairs(tools.TrunkPairs)}): {HudFormat.MoneyPerMeter(pricing.GetCablePricePerMeter(tools.TrunkPairs))}",
                ToolType.Pole => $"Pole: {HudFormat.Money(pricing.PolePrice)}",
                ToolType.Meter => "Meter: free",
                ToolType.Scissors => "Cutters: free",
                _ => null,
            };
        }

        private void OnMessagePosted(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            _message.text = message;
            _message.visible = true;
            _messageHideTime = Time.unscaledTime + messageDuration;
        }
    }
}
