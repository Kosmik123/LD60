using SpaghettiWires.Map;
using UnityEngine;

namespace SpaghettiWires.Views
{
    /// <summary>
    /// Płaski znacznik pola pod kursorem: zielony, gdy można tu coś postawić, czerwony, gdy nie.
    /// </summary>
    public sealed class CellHighlight : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private MapGridBehaviour grid;
        [SerializeField] private Renderer marker;
        [Range(0.1f, 1f)] [SerializeField] private float fill = 0.9f;
        [SerializeField] private float heightOffset = 0.03f;

        private MaterialPropertyBlock _block;

        public void Show(Vector2Int cell, Color color)
        {
            var mapGrid = grid.Grid;
            marker.transform.position = mapGrid.CellToWorld(cell) + Vector3.up * heightOffset;
            marker.transform.localScale = new Vector3(mapGrid.CellSize * fill, 0.01f, mapGrid.CellSize * fill);

            _block ??= new MaterialPropertyBlock();
            marker.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            marker.SetPropertyBlock(_block);
            marker.enabled = true;
        }

        public void Hide() => marker.enabled = false;
    }
}
