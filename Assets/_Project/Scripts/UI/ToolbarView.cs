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
            (ToolType.Cable, "1", "Kabel"),
            (ToolType.Trunk, "2", "Magistrala"),
            (ToolType.Pole, "3", "Słup"),
            (ToolType.Meter, "4", "Miernik"),
            (ToolType.Scissors, "5", "Nożyce"),
        };

        [SerializeField] private ToolController tools;
        [SerializeField] private StyleSheet styleSheet;
        [Tooltip("Jak długo komunikat zostaje na ekranie (s).")]
        [SerializeField] private float messageDuration = 3f;

        private readonly Dictionary<ToolType, Button> _buttons = new Dictionary<ToolType, Button>();
        private Label _message;
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

                bar.Add(button);
                _buttons[type] = button;
            }

            tools.ToolChanged += OnToolChanged;
            tools.MessagePosted += OnMessagePosted;
            OnToolChanged(tools.Current);
            _message.visible = false;
        }

        private void OnDisable()
        {
            tools.ToolChanged -= OnToolChanged;
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
