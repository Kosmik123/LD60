using UnityEngine;

namespace SpaghettiWires.Tools
{
    /// <summary>
    /// Stawia nowe domy na mapie. Teraz robi to tymczasowy sandbox, docelowo reguły budowy (PLAN etap 2).
    /// Progresja gry decyduje, kiedy, a implementacja, gdzie (chyba że progresja poda pole).
    /// </summary>
    public interface IHouseSpawner
    {
        /// <summary>Stawia dom w losowym miejscu. False, gdy nie ma już na niego miejsca.</summary>
        bool TrySpawnHouse();

        /// <summary>Stawia dom na podanym polu. False, gdy pole jest poza mapą albo zajęte.</summary>
        bool TrySpawnHouse(Vector2Int cell);
    }
}
