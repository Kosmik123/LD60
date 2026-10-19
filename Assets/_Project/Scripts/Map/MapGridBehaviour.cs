using UnityEngine;

namespace SpaghettiWires.Map
{
    /// <summary>
    /// Ustawienia siatki mapy na scenie. Siatka jest wyśrodkowana na pozycji obiektu,
    /// a poziom ziemi to jego wysokość Y.
    /// </summary>
    public sealed class MapGridBehaviour : MonoBehaviour
    {
        [Tooltip("Bok pola w metrach.")]
        [Min(0.1f)] [SerializeField] private float cellSize = 4f;
        [SerializeField] private Vector2Int size = new Vector2Int(25, 25);

        private MapGrid _grid;

        public MapGrid Grid => _grid ??= CreateGrid();

        private MapGrid CreateGrid() => MapGrid.Centered(cellSize, size, transform.position);

        private void OnValidate()
        {
            size = Vector2Int.Max(size, Vector2Int.one);
            _grid = null;
        }

        private void OnDrawGizmosSelected()
        {
            var grid = CreateGrid();
            Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
            for (int x = 0; x <= grid.Size.x; x++)
            {
                Vector3 from = grid.Origin + new Vector3(x * grid.CellSize, 0f, 0f);
                Gizmos.DrawLine(from, from + new Vector3(0f, 0f, grid.WorldSize.y));
            }
            for (int y = 0; y <= grid.Size.y; y++)
            {
                Vector3 from = grid.Origin + new Vector3(0f, 0f, y * grid.CellSize);
                Gizmos.DrawLine(from, from + new Vector3(grid.WorldSize.x, 0f, 0f));
            }
        }
    }
}
