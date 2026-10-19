namespace SpaghettiWires.Simulation
{
    /// <summary>
    /// Stawia nowe domy na mapie. Teraz robi to tymczasowy sandbox, docelowo reguły budowy (PLAN etap 2).
    /// Progresja gry decyduje, kiedy, a implementacja gdzie.
    /// </summary>
    public interface IHouseSpawner
    {
        /// <summary>Stawia jeden dom. False, gdy nie ma już na niego miejsca.</summary>
        bool TrySpawnHouse();
    }
}
