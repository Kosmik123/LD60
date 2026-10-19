namespace SpaghettiWires.Tools
{
    /// <summary>Narzędzia gracza w kolejności z GDD (rozdział 4), co odpowiada klawiszom 1–5.</summary>
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

namespace SpaghettiWires.Tools
{
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

        /// <summary>Klawisz wybierający narzędzie: 1–5 w kolejności z <see cref="ToolType"/>.</summary>
        public static string Key(this ToolType type) => type == ToolType.None ? string.Empty : ((int)type).ToString();
    }
}
