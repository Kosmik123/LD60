namespace SpaghettiWires.Views
{
    /// <summary>
    /// Wygląd węzła na scenie. Odpowiada typom węzłów sieci (dom, słup, centrala),
    /// ale widoki nie zależą od assembly sieci.
    /// </summary>
    public enum NodeVisualKind
    {
        House,
        Pole,
        Exchange,
    }
}
