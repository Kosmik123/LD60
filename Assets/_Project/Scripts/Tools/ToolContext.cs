using System;
using SpaghettiWires.Views;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaghettiWires.Tools
{
    /// <summary>
    /// Wszystko, czego potrzebują narzędzia: wskaźnik, akcje gry, elementy podglądu
    /// i wspólne podświetlanie wskazanego węzła lub kabla.
    /// </summary>
    public sealed class ToolContext
    {
        public static readonly Color ValidColor = new Color(0.3f, 0.9f, 0.4f, 0.6f);
        public static readonly Color InvalidColor = new Color(0.95f, 0.3f, 0.25f, 0.6f);
        public static readonly Color NeutralColor = new Color(1f, 1f, 1f, 0.6f);

        private readonly Action<string> _postMessage;
        private NodeView _highlightedNode;
        private CableView _highlightedCable;

        public ScenePointer Pointer { get; }
        public IBuildActions Actions { get; }
        public GridOverlay GridOverlay { get; }
        public CellHighlight CellHighlight { get; }
        public CablePreview CablePreview { get; }

        /// <summary>Ceny, jeśli budowa jest płatna. Null = bez cen.</summary>
        public IBuildPricing Pricing { get; }

        /// <summary>Cena nad podglądem kabla, czyta ją UI.</summary>
        public PricePreview PricePreview { get; private set; }

        public ToolContext(ScenePointer pointer, IBuildActions actions, GridOverlay gridOverlay,
            CellHighlight cellHighlight, CablePreview cablePreview, Action<string> postMessage)
        {
            Pointer = pointer;
            Actions = actions;
            GridOverlay = gridOverlay;
            CellHighlight = cellHighlight;
            CablePreview = cablePreview;
            Pricing = actions as IBuildPricing;
            _postMessage = postMessage;
        }

        /// <summary>Kliknięcie lewym przyciskiem w scenę (nie w UI).</summary>
        public bool WasClickedThisFrame =>
            Pointer.HasPointer && !Pointer.IsOverUI && Mouse.current.leftButton.wasPressedThisFrame;

        public bool WasReleasedThisFrame => Pointer.HasPointer && Mouse.current.leftButton.wasReleasedThisFrame;

        public void PostMessage(string message) => _postMessage?.Invoke(message);

        public void ShowPrice(Vector3 position, int price) =>
            PricePreview = new PricePreview(position, price, Pricing == null || Pricing.CanAfford(price));

        public void HidePrice() => PricePreview = default;

        public void HighlightNode(NodeView node, Color color)
        {
            if (_highlightedNode != node)
                ClearNodeHighlight();

            _highlightedNode = node;
            if (node != null)
                node.SetHighlight(color, 0.5f);
        }

        public void HighlightCable(CableView cable, Color color)
        {
            if (_highlightedCable != cable)
                ClearCableHighlight();

            _highlightedCable = cable;
            if (cable != null)
                cable.SetHighlight(color);
        }

        /// <summary>Chowa wszystko, co narzędzia pokazują na scenie.</summary>
        public void ClearAll()
        {
            ClearNodeHighlight();
            ClearCableHighlight();
            GridOverlay.Visible = false;
            CellHighlight.Hide();
            CablePreview.Hide();
            HidePrice();
        }

        private void ClearNodeHighlight()
        {
            if (_highlightedNode != null)
                _highlightedNode.ClearHighlight();
            _highlightedNode = null;
        }

        private void ClearCableHighlight()
        {
            if (_highlightedCable != null)
                _highlightedCable.ClearHighlight();
            _highlightedCable = null;
        }
    }
}
