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

            // Podgląd kończy się w punktach przyczepu, w których kabel faktycznie wyląduje.
            int cableId = _context.Actions.NextCableId;
            if (target != null)
            {
                Vector3 start = _from.GetAnchorPosition(cableId, target.transform.position);
                Vector3 end = target.GetAnchorPosition(cableId, _from.transform.position);
                _context.CablePreview.Show(start, end, _pairs, color);
                ShowPrice(target.transform.position, (start + end) / 2f);
            }
            else if (pointer.HasGroundPoint)
            {
                Vector3 start = _from.GetAnchorPosition(cableId, pointer.GroundPoint);
                _context.CablePreview.Show(start, pointer.GroundPoint, _pairs, color);
                ShowPrice(pointer.GroundPoint, (start + pointer.GroundPoint) / 2f);
            }
            else
            {
                _context.HidePrice();
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

        /// <summary>
        /// Cena nad środkiem podglądu. Długość to odległość w poziomie od węzła startowego
        /// do celu (węzła albo kursora), tak samo jak przy zakupie, czyli bez zwisu.
        /// </summary>
        private void ShowPrice(Vector3 end, Vector3 labelPosition)
        {
            if (_context.Pricing == null)
                return;

            Vector3 offset = end - _from.transform.position;
            float length = new Vector2(offset.x, offset.z).magnitude;
            _context.ShowPrice(labelPosition, _context.Pricing.GetCablePrice(length, _pairs));
        }

        public void Cancel()
        {
            _from = null;
            _context.CablePreview.Hide();
            _context.HidePrice();
            _context.HighlightNode(null, default);
        }

        public void Exit()
        {
            Cancel();
            _context.ClearAll();
        }
    }
}
