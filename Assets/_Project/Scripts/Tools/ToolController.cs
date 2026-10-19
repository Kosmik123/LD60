using System;
using System.Collections.Generic;
using Bipolar;
using SpaghettiWires.Views;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaghettiWires.Tools
{
    /// <summary>
    /// Wybór aktywnego narzędzia (klawisze 1–4, Esc, pasek narzędzi) i przekazywanie mu klatek.
    /// Klawisze: 1 kabel, 2 słup, 3 nożyce, 4 magistrala.
    /// Esc najpierw przerywa trwającą akcję, a dopiero drugie Esc odkłada narzędzie.
    /// </summary>
    public sealed class ToolController : MonoBehaviour
    {
        [SerializeField] private ScenePointer pointer;
        [SerializeField] private Serialized<IBuildActions> buildActions;

        [Header("Podgląd")]
        [SerializeField] private GridOverlay gridOverlay;
        [SerializeField] private CellHighlight cellHighlight;
        [SerializeField] private CablePreview cablePreview;

        [Header("Kable")]
        [Tooltip("Liczba żył magistrali (GDD 3.3: 4, 9 albo 16).")]
        [SerializeField] private int trunkPairs = 4;

        private readonly Dictionary<ToolType, ITool> _tools = new Dictionary<ToolType, ITool>();
        private ToolContext _context;

        public ToolType Current { get; private set; } = ToolType.None;

        public event Action<ToolType> ToolChanged;

        /// <summary>Komunikat dla gracza, np. powód, dla którego akcja się nie udała.</summary>
        public event Action<string> MessagePosted;

        /// <summary>Pokazuje komunikat w linii komunikatów nad paskiem narzędzi.</summary>
        public void PostMessage(string message) => MessagePosted?.Invoke(message);

        /// <summary>Liczba żył magistrali kładzionej narzędziem Magistrala.</summary>
        public int TrunkPairs => trunkPairs;

        /// <summary>Ceny budowy, jeśli akcje gry są płatne. Null = bez cen.</summary>
        public IBuildPricing Pricing => buildActions.Value as IBuildPricing;

        /// <summary>Cena nad podglądem kabla w tej klatce.</summary>
        public PricePreview PricePreview => _context?.PricePreview ?? default;

        public IBuildActions BuildActions
        {
            get => buildActions.Value;
            set => buildActions.Value = value;
        }

        private void Start()
        {
            _context = new ToolContext(pointer, buildActions.Value, gridOverlay, cellHighlight, cablePreview, PostMessage);
            _tools[ToolType.Cable] = new CableTool(_context, pairs: 1);
            _tools[ToolType.Trunk] = new CableTool(_context, trunkPairs);
            _tools[ToolType.Pole] = new PoleTool(_context);
            _tools[ToolType.Meter] = CableActionTool.Meter(_context);
            _tools[ToolType.Scissors] = CableActionTool.Scissors(_context);
            _context.ClearAll();
        }

        private void Update()
        {
            HandleKeys();
            if (_tools.TryGetValue(Current, out var tool))
                tool.Tick();
        }

        public void Select(ToolType type)
        {
            if (type == Current)
                return;

            if (_tools.TryGetValue(Current, out var previous))
                previous.Exit();

            Current = type;
            if (_tools.TryGetValue(Current, out var next))
                next.Enter();

            ToolChanged?.Invoke(Current);
        }

        private void HandleKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            // Miernika nie ma w MVP: narzędzie zostaje w kodzie, ale nie da się go wybrać.
            if (keyboard.digit1Key.wasPressedThisFrame) Select(ToolType.Cable);
            if (keyboard.digit2Key.wasPressedThisFrame) Select(ToolType.Pole);
            if (keyboard.digit3Key.wasPressedThisFrame) Select(ToolType.Scissors);
            if (keyboard.digit4Key.wasPressedThisFrame) Select(ToolType.Trunk);

            if (keyboard.escapeKey.wasPressedThisFrame)
                HandleEscape();
        }

        private void HandleEscape()
        {
            if (Current == ToolType.Cable || Current == ToolType.Trunk)
            {
                // Przeciąganie kabla: pierwsze Esc tylko je przerywa.
                var tool = (CableTool)_tools[Current];
                if (tool.IsDragging)
                {
                    tool.Cancel();
                    return;
                }
            }

            Select(ToolType.None);
        }
    }
}
