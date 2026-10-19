using SpaghettiWires.Views;
using UnityEngine;

namespace SpaghettiWires.Tools
{
    /// <summary>
    /// Kładzenie kabla lub magistrali: wciśnij na węźle, przeciągnij i puść na drugim węźle.
    /// Podgląd jest zielony, gdy kabel da się położyć, i czerwony, gdy nie.
    /// </summary>
    public sealed class CableTool : ITool
    {
        private readonly ToolContext _context;
        private readonly int _pairs;
        private NodeView _from;

        public CableTool(ToolContext context, int pairs)
        {
            _context = context;
            _pairs = pairs;
        }

        public bool IsDragging => _from != null;

        public void Enter() { }

        public void Tick()
        {
            if (_from == null)
                TickIdle();
            else
                TickDragging();
        }

        private void TickIdle()
        {
            var node = _context.Pointer.HoveredNode;
            _context.HighlightNode(node, ToolContext.NeutralColor);

            if (node != null && _context.WasClickedThisFrame)
                _from = node;
        }

        private void TickDragging()
        {
            var pointer = _context.Pointer;
            var target = pointer.HoveredNode != _from ? pointer.HoveredNode : null;
            var check = target != null ? _context.Actions.CheckCable(_from.Id, target.Id, _pairs) : default;

            Color color = target == null ? ToolContext.NeutralColor
                : check.IsValid ? ToolContext.ValidColor
                : ToolContext.InvalidColor;
            _context.HighlightNode(target, color);

            if (target != null)
            {
                Vector3 end = target.GetAnchorPosition(_from.transform.position);
                _context.CablePreview.Show(_from.GetAnchorPosition(end), end, _pairs, color);
            }
            else if (pointer.HasGroundPoint)
            {
                _context.CablePreview.Show(_from.GetAnchorPosition(pointer.GroundPoint), pointer.GroundPoint, _pairs, color);
            }

            if (!_context.WasReleasedThisFrame)
                return;

            if (target != null)
            {
                if (check.IsValid)
                    _context.Actions.LayCable(_from.Id, target.Id, _pairs);
                else
                    _context.PostMessage(check.Reason);
            }
            Cancel();
        }

        public void Cancel()
        {
            _from = null;
            _context.CablePreview.Hide();
            _context.HighlightNode(null, default);
        }

        public void Exit()
        {
            Cancel();
            _context.ClearAll();
        }
    }
}
