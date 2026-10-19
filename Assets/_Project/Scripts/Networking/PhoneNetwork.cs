namespace SpaghettiWires.Networking
{
    /// <summary>
    /// Fasada sieci (TECH 4.1): tworzy trzy warstwy we właściwej kolejności i trzyma je razem.
    /// To jedyny obiekt sieci, który dostają reguły, symulacja i widoki. Każda warstwa
    /// udostępnia swoje operacje i zdarzenia, więc fasada ich nie powiela.
    /// </summary>
    public sealed class PhoneNetwork
    {
        public CableNetwork Structure { get; }
        public CableCondition Condition { get; }
        public Traffic Traffic { get; }

        public PhoneNetwork()
        {
            // Kolejność ma znaczenie: każda warstwa subskrybuje zdarzenia poprzednich.
            Structure = new CableNetwork();
            Condition = new CableCondition(Structure);
            Traffic = new Traffic(Structure, Condition);
        }
    }
}
