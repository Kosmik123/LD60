using SpaghettiWires.Views;
using UnityEngine;

namespace SpaghettiWires.Tools
{
    /// <summary>
    /// Kładzenie kabla lub magistrali: wciśnij na węźle, przeciągnij i puść na drugim węźle.
    /// Podgląd jest zielony, gdy kabel da się położyć, i czerwony, gdy nie.
    /// Zanim kabel wyceluje w węzeł, jest czerwony, gdy jest za długi albo gracza na niego nie stać.
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

            // Podgląd kończy się w punktach przyczepu, w których kabel faktycznie wyląduje.
            int cableId = _context.Actions.NextCableId;
            if (target != null)
            {
                Color color = check.IsValid ? ToolContext.ValidColor : ToolContext.InvalidColor;
                _context.HighlightNode(target, color);

                Vector3 start = _from.GetAnchorPosition(cableId, target.transform.position);
                Vector3 end = target.GetAnchorPosition(cableId, _from.transform.position);
                _context.CablePreview.Show(start, end, _pairs, color);
                ShowPrice(GetPrice(GetLength(target.transform.position)), (start + end) / 2f);
            }
            else if (pointer.HasGroundPoint)
            {
                _context.HighlightNode(null, default);

                float length = GetLength(pointer.GroundPoint);
                int? price = GetPrice(length);
                bool tooLong = length > _context.Actions.GetMaxCableLength(_pairs);
                bool tooExpensive = price.HasValue && !_context.Pricing.CanAfford(price.Value);
                Color color = tooLong || tooExpensive ? ToolContext.InvalidColor : ToolContext.NeutralColor;

                Vector3 start = _from.GetAnchorPosition(cableId, pointer.GroundPoint);
                _context.CablePreview.Show(start, pointer.GroundPoint, _pairs, color);
                ShowPrice(price, (start + pointer.GroundPoint) / 2f);
            }
            else
            {
                _context.HighlightNode(null, default);
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
        /// Długość kabla do punktu: odległość w poziomie od węzła startowego,
        /// tak samo jak przy zakupie, czyli bez zwisu.
        /// </summary>
        private float GetLength(Vector3 end)
        {
            Vector3 offset = end - _from.transform.position;
            return new Vector2(offset.x, offset.z).magnitude;
        }

        /// <summary>Cena kabla o danej długości. Null, gdy budowa nie jest płatna.</summary>
        private int? GetPrice(float length) => _context.Pricing?.GetCablePrice(length, _pairs);

        /// <summary>Cena nad środkiem podglądu.</summary>
        private void ShowPrice(int? price, Vector3 labelPosition)
        {
            if (price.HasValue)
                _context.ShowPrice(labelPosition, price.Value);
            else
                _context.HidePrice();
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
