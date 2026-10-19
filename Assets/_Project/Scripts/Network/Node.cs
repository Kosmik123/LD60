namespace SpaghettiWires.Network
{
    /// <summary>
    /// Węzeł sieci: dom, słup albo centrala. Jeden węzeł na obiekt na mapie.
    /// Węzeł nie zna swoich kabli, indeks „węzeł → kable” trzyma <see cref="CableNetwork"/>.
    /// </summary>
    public sealed class Node
    {
        public int Id { get; }
        public NodeKind Kind { get; }

        public Node(int id, NodeKind kind)
        {
            Id = id;
            Kind = kind;
        }

        public override string ToString() => $"Node#{Id} ({Kind})";
    }
}
