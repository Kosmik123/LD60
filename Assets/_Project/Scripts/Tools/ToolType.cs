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

    public static class ToolTypeExtensions
    {
        /// <summary>Nazwa narzędzia pokazywana graczowi.</summary>
        public static string Label(this ToolType type) => type switch
        {
            ToolType.Cable => "Cable",
            ToolType.Trunk => "Trunk",
            ToolType.Pole => "Pole",
            ToolType.Meter => "Meter",
            ToolType.Scissors => "Cutters",
            _ => string.Empty,
        };

        /// <summary>Klawisz wybierający narzędzie, jak w <see cref="ToolController"/>. Miernik nie ma klawisza.</summary>
        public static string Key(this ToolType type) => type switch
        {
            ToolType.Cable => "1",
            ToolType.Pole => "2",
            ToolType.Scissors => "3",
            ToolType.Trunk => "4",
            _ => string.Empty,
        };
    }
}
