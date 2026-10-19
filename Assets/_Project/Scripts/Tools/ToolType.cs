namespace SpaghettiWires.Tools
{
    /// <summary>
    /// Narzędzia gracza. Kolejność na pasku i klawisze ustala <see cref="ToolController"/>.
    /// Miernika nie ma w MVP, więc nie da się go wybrać.
    /// </summary>
    public enum ToolType
    {
        None,
        Cable,
        Trunk,
        Pole,
        Meter,
        Scissors,
    }
}
