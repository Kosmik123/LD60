using System;
using SpaghettiWires.Views;
using UnityEngine;

namespace SpaghettiWires.Tools
{
    /// <summary>
    /// Narzędzie działające na wskazanym kablu jednym kliknięciem: miernik i nożyce (GDD 3.5).
    /// </summary>
    public sealed class CableActionTool : ITool
    {
        private readonly ToolContext _context;
        private readonly Color _highlight;
        private readonly Action<CableView> _onClick;

        public CableActionTool(ToolContext context, Color highlight, Action<CableView> onClick)
        {
            _context = context;
            _highlight = highlight;
            _onClick = onClick;
        }

        public static CableActionTool Meter(ToolContext context) =>
            new CableActionTool(context, new Color(0.3f, 0.8f, 1f), cable =>
            {
                int working = context.Actions.MeasureWorkingPairs(cable.Id);
                context.PostMessage(cable.Pairs == 1
                    ? (working > 0 ? "The cable works." : "The cable is broken.")
                    : $"Working pairs: {working}/{cable.Pairs}");
            });

        public static CableActionTool Scissors(ToolContext context) =>
            new CableActionTool(context, new Color(1f, 0.35f, 0.25f), cable => context.Actions.RemoveCable(cable.Id));

        public void Enter() { }

        public void Tick()
        {
            var cable = _context.Pointer.HoveredCable;
            _context.HighlightCable(cable, _highlight);

            if (cable != null && _context.WasClickedThisFrame)
            {
                _context.HighlightCable(null, default); // kabel może zaraz zniknąć
                _onClick(cable);
            }
        }

        public void Cancel() { }

        public void Exit() => _context.ClearAll();
    }
}
