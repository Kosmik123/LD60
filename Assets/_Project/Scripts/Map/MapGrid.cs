using UnityEngine;

namespace SpaghettiWires.Map
{
    /// <summary>
    /// Logiczna siatka mapy w płaszczyźnie XZ (TECH 5). Tylko geometria: przeliczanie
    /// między polami a pozycjami w świecie. Nie wie, co stoi na polach.
    /// Pole (0, 0) ma lewy dolny róg w <see cref="Origin"/>, oś Y pola to oś Z świata.
    /// </summary>
    public sealed class MapGrid
    {
        public float CellSize { get; }
        public Vector2Int Size { get; }

        /// <summary>Róg pola (0, 0) w świecie. Wysokość Y to poziom ziemi.</summary>
        public Vector3 Origin { get; }

        public Vector2 WorldSize => new Vector2(Size.x * CellSize, Size.y * CellSize);
        public Vector3 WorldCenter => Origin + new Vector3(WorldSize.x / 2f, 0f, WorldSize.y / 2f);

        public MapGrid(float cellSize, Vector2Int size, Vector3 origin)
        {
            CellSize = cellSize;
            Size = size;
            Origin = origin;
        }

        /// <summary>Siatka o danym rozmiarze, której środek leży w <paramref name="center"/>.</summary>
        public static MapGrid Centered(float cellSize, Vector2Int size, Vector3 center) =>
            new MapGrid(cellSize, size, center - new Vector3(size.x * cellSize / 2f, 0f, size.y * cellSize / 2f));

        public bool Contains(Vector2Int cell) =>
            cell.x >= 0 && cell.y >= 0 && cell.x < Size.x && cell.y < Size.y;

        /// <summary>Środek pola na poziomie ziemi.</summary>
        public Vector3 CellToWorld(Vector2Int cell) =>
            Origin + new Vector3((cell.x + 0.5f) * CellSize, 0f, (cell.y + 0.5f) * CellSize);

        /// <summary>Pole, w którym leży punkt. Może wypaść poza siatkę, patrz <see cref="TryWorldToCell"/>.</summary>
        public Vector2Int WorldToCell(Vector3 world)
        {
            Vector3 local = world - Origin;
            return new Vector2Int(Mathf.FloorToInt(local.x / CellSize), Mathf.FloorToInt(local.z / CellSize));
        }

        public bool TryWorldToCell(Vector3 world, out Vector2Int cell)
        {
            cell = WorldToCell(world);
            return Contains(cell);
        }

        /// <summary>Odległość między środkami pól w polach (euklidesowa).</summary>
        public static float CellDistance(Vector2Int a, Vector2Int b) => Vector2Int.Distance(a, b);
    }
}
