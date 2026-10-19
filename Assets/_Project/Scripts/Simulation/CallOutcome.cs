namespace SpaghettiWires.Simulation
{
    /// <summary>Wynik próby połączenia (GDD 3.1, 3.8).</summary>
    public enum CallOutcome
    {
        Connected,

        /// <summary>Linia dzwoniącego jest zajęta (rozmawia albo przechodzi przez niego cudza rozmowa).</summary>
        CallerBusy,

        /// <summary>Linia odbiorcy jest zajęta.</summary>
        CalleeBusy,

        /// <summary>Nie ma trasy z wolnymi, sprawnymi żyłami.</summary>
        NoRoute,
    }
}
