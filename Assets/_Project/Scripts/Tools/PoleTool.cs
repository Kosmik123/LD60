namespace SpaghettiWires.Tools
{
    /// <summary>Stawianie słupa na wolnym polu siatki.</summary>
    public sealed class PoleTool : ITool
    {
        private readonly ToolContext _context;

        public PoleTool(ToolContext context) => _context = context;

        public void Enter() => _context.GridOverlay.Visible = true;

        public void Tick()
        {
            var pointer = _context.Pointer;
            if (!pointer.IsCellInGrid)
            {
                _context.CellHighlight.Hide();
                return;
            }

            var check = _context.Actions.CheckPole(pointer.Cell);
            _context.CellHighlight.Show(pointer.Cell, check.IsValid ? ToolContext.ValidColor : ToolContext.InvalidColor);

            if (!_context.WasClickedThisFrame)
                return;

            if (check.IsValid)
                _context.Actions.PlacePole(pointer.Cell);
            else
                _context.PostMessage(check.Reason);
        }

        public void Cancel() { }

        public void Exit() => _context.ClearAll();
    }
}
