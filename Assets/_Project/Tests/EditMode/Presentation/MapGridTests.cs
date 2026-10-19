using NUnit.Framework;
using SpaghettiWires.Map;
using UnityEngine;

namespace SpaghettiWires.Tests
{
    public class MapGridTests
    {
        // 4 m na pole, 10×6 pól, wyśrodkowana w (0, 0, 0): rogi w (-20, -12) i (20, 12).
        private readonly MapGrid _grid = MapGrid.Centered(4f, new Vector2Int(10, 6), Vector3.zero);

        [Test]
        public void Centered_PutsOriginAtLowerLeftCorner()
        {
            Assert.AreEqual(new Vector3(-20f, 0f, -12f), _grid.Origin);
            Assert.AreEqual(Vector3.zero, _grid.WorldCenter);
        }

        [Test]
        public void CellToWorld_ReturnsCellCenterOnGround()
        {
            Assert.AreEqual(new Vector3(-18f, 0f, -10f), _grid.CellToWorld(new Vector2Int(0, 0)));
            Assert.AreEqual(new Vector3(18f, 0f, 10f), _grid.CellToWorld(new Vector2Int(9, 5)));
        }

        [Test]
        public void WorldToCell_IsInverseOfCellToWorld()
        {
            for (int x = 0; x < _grid.Size.x; x++)
            for (int y = 0; y < _grid.Size.y; y++)
            {
                var cell = new Vector2Int(x, y);
                Assert.AreEqual(cell, _grid.WorldToCell(_grid.CellToWorld(cell)));
            }
        }

        [Test]
        public void WorldToCell_UsesFloorOnCellBorders()
        {
            Assert.AreEqual(new Vector2Int(5, 3), _grid.WorldToCell(new Vector3(0f, 0f, 0f)));
            Assert.AreEqual(new Vector2Int(4, 2), _grid.WorldToCell(new Vector3(-0.01f, 0f, -0.01f)));
        }

        [Test]
        public void WorldToCell_IgnoresHeight()
        {
            Assert.AreEqual(_grid.WorldToCell(new Vector3(3f, 0f, 3f)), _grid.WorldToCell(new Vector3(3f, 50f, 3f)));
        }

        [Test]
        public void TryWorldToCell_FailsOutsideGrid()
        {
            Assert.IsTrue(_grid.TryWorldToCell(new Vector3(19.9f, 0f, 11.9f), out var inside));
            Assert.AreEqual(new Vector2Int(9, 5), inside);

            Assert.IsFalse(_grid.TryWorldToCell(new Vector3(20f, 0f, 0f), out _));
            Assert.IsFalse(_grid.TryWorldToCell(new Vector3(-20.1f, 0f, 0f), out _));
        }

        [Test]
        public void CellDistance_IsEuclidean()
        {
            Assert.AreEqual(5f, MapGrid.CellDistance(new Vector2Int(0, 0), new Vector2Int(3, 4)));
        }
    }
}
