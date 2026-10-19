using SpaghettiWires.Map;
using UnityEngine;

namespace SpaghettiWires.Views
{
    /// <summary>
    /// Linie siatki na ziemi. Siatka jest logiczna i normalnie niewidoczna (GDD),
    /// pokazujemy ją tylko przy stawianiu obiektów.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class GridOverlay : MonoBehaviour
    {
        [SerializeField] private MapGridBehaviour grid;
        [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.35f);
        [Tooltip("Wysokość linii nad ziemią, żeby nie migotały z podłożem.")]
        [SerializeField] private float heightOffset = 0.02f;

        private MeshRenderer _renderer;

        public bool Visible
        {
            get => Renderer.enabled;
            set => Renderer.enabled = value;
        }

        private MeshRenderer Renderer => _renderer != null ? _renderer : _renderer = GetComponent<MeshRenderer>();

        private void Start() => Rebuild();

        public void Rebuild()
        {
            var mapGrid = grid.Grid;
            int lineCount = mapGrid.Size.x + 1 + mapGrid.Size.y + 1;
            var vertices = new Vector3[lineCount * 2];
            var colors = new Color[vertices.Length];
            var indices = new int[vertices.Length];

            // Siatka rysowana w przestrzeni świata, więc obiekt powinien stać w (0, 0, 0) bez obrotu.
            Vector3 origin = mapGrid.Origin + Vector3.up * heightOffset;
            int v = 0;
            for (int x = 0; x <= mapGrid.Size.x; x++)
            {
                Vector3 from = origin + new Vector3(x * mapGrid.CellSize, 0f, 0f);
                vertices[v++] = from;
                vertices[v++] = from + new Vector3(0f, 0f, mapGrid.WorldSize.y);
            }
            for (int y = 0; y <= mapGrid.Size.y; y++)
            {
                Vector3 from = origin + new Vector3(0f, 0f, y * mapGrid.CellSize);
                vertices[v++] = from;
                vertices[v++] = from + new Vector3(mapGrid.WorldSize.x, 0f, 0f);
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                colors[i] = lineColor;
                indices[i] = i;
            }

            var mesh = new Mesh { name = "GridOverlay" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }
}
