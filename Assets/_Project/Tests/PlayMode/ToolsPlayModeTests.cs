using System.Collections;
using System.Linq;
using NUnit.Framework;
using SpaghettiWires.Map;
using SpaghettiWires.Sandbox;
using SpaghettiWires.Tools;
using SpaghettiWires.Views;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SpaghettiWires.Tests
{
    /// <summary>
    /// Sprawdza scenę gry od kursora do efektu: symulowana mysz → wskaźnik (raycast, siatka) → narzędzie → akcja.
    /// Akcje obsługuje tymczasowy sandbox, więc test nie zależy od sieci.
    /// </summary>
    public class ToolsPlayModeTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";

        private InputSettings _originalInputSettings;
        private InputSettings _testInputSettings;
        private Mouse _mouse;
        private Camera _camera;
        private ToolController _tools;
        private ViewRegistry _views;
        private MapGrid _grid;
        private SandboxBuildActions _sandbox;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#endif
            // W trybie batch okno nie ma fokusu, a domyślnie Input System wyłącza wtedy urządzenia.
            _originalInputSettings = InputSystem.settings;
            _testInputSettings = ScriptableObject.CreateInstance<InputSettings>();
            _testInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _testInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = _testInputSettings;

            _mouse = InputSystem.AddDevice<Mouse>();
            _mouse.MakeCurrent();

            // Nowa mysz stoi w (0, 0), czyli na krawędzi ekranu, co przesuwałoby kamerę. Środek ekranu jest bezpieczny.
            yield return Move(new Vector2(Screen.width / 2f, Screen.height / 2f), pressed: false);

            // Klatka na Start() komponentów (sandbox stawia wtedy domy).
            yield return null;

            _camera = Camera.main;
            _tools = Object.FindAnyObjectByType<ToolController>();
            _views = Object.FindAnyObjectByType<ViewRegistry>();
            _grid = Object.FindAnyObjectByType<MapGridBehaviour>().Grid;
            _sandbox = Object.FindAnyObjectByType<SandboxBuildActions>();
        }

        [TearDown]
        public void TearDown()
        {
            InputSystem.RemoveDevice(_mouse);

            // Projekt nie ma pliku ustawień Input System, więc oryginał to obiekt w pamięci, który Unity mogło zwolnić.
            InputSystem.settings = _originalInputSettings != null
                ? _originalInputSettings
                : ScriptableObject.CreateInstance<InputSettings>();
            Object.Destroy(_testInputSettings);
        }

        [UnityTest]
        public IEnumerator Scene_StartsWithHousesAndNoTool()
        {
            Assert.Greater(_views.Nodes.Count, 0);
            Assert.IsTrue(_views.Nodes.All(node => node.Kind == NodeVisualKind.House));
            Assert.AreEqual(ToolType.None, _tools.Current);
            yield break;
        }

        [UnityTest]
        public IEnumerator PoleTool_ClickOnFreeCell_PlacesPoleOnThatCell()
        {
            var cell = FindFreeCellNearCenter();
            _tools.Select(ToolType.Pole);
            Assert.IsTrue(Object.FindAnyObjectByType<GridOverlay>().Visible, "Grid is shown while placing poles");

            yield return Click(_camera.WorldToScreenPoint(_grid.CellToWorld(cell)));

            var pole = _views.Nodes.SingleOrDefault(node => node.Kind == NodeVisualKind.Pole);
            Assert.IsNotNull(pole, "A pole was placed");
            Assert.AreEqual(cell, pole.Cell);
        }

        [UnityTest]
        public IEnumerator PoleTool_ClickOnOccupiedCell_DoesNothing()
        {
            var house = _views.Nodes.First();
            int nodeCount = _views.Nodes.Count;
            _tools.Select(ToolType.Pole);

            yield return Click(_camera.WorldToScreenPoint(_grid.CellToWorld(house.Cell)));

            Assert.AreEqual(nodeCount, _views.Nodes.Count);
        }

        [UnityTest]
        public IEnumerator CableTool_DragFromHouseToHouse_LaysCable()
        {
            var (from, to) = FindPickableHousePair();
            _tools.Select(ToolType.Cable);

            yield return Move(PickPoint(from), pressed: false);
            yield return Move(PickPoint(from), pressed: true);
            yield return Move(PickPoint(to), pressed: true);
            yield return Move(PickPoint(to), pressed: false);

            var cable = _views.Cables.Single();
            Assert.AreEqual(from, cable.From);
            Assert.AreEqual(to, cable.To);
            Assert.AreEqual(1, cable.Pairs);
        }

        [UnityTest]
        public IEnumerator ScissorsTool_ClickOnCable_RemovesIt()
        {
            var (from, to) = FindPickableHousePair();
            _sandbox.LayCable(from.Id, to.Id, 1);
            var cable = _views.Cables.Single();
            _tools.Select(ToolType.Scissors);

            yield return Click(FindPickablePointOn(cable));

            Assert.IsEmpty(_views.Cables);
        }

        #region Helpers

        private IEnumerator Click(Vector2 screenPosition)
        {
            yield return Move(screenPosition, pressed: false);
            yield return Move(screenPosition, pressed: true);
            yield return Move(screenPosition, pressed: false);
        }

        private IEnumerator Move(Vector2 screenPosition, bool pressed)
        {
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = screenPosition }.WithButton(MouseButton.Left, pressed));
            yield return null;
        }

        private Vector2Int FindFreeCellNearCenter()
        {
            var center = _grid.Size / 2;
            for (int radius = 0; radius < 10; radius++)
            for (int x = -radius; x <= radius; x++)
            for (int y = -radius; y <= radius; y++)
            {
                var cell = center + new Vector2Int(x, y);
                if (_views.Nodes.All(node => node.Cell != cell))
                    return cell;
            }
            throw new AssertionException("No free cell near the center.");
        }

        // Punkt ekranu, w którym promień z kamery trafia właśnie w ten węzeł (a nie w dom przed nim).
        private Vector2 PickPoint(NodeView node) => _camera.WorldToScreenPoint(node.transform.position + Vector3.up * 2f);

        private bool IsPickable(NodeView node)
        {
            var ray = _camera.ScreenPointToRay(PickPoint(node));
            return Physics.Raycast(ray, out var hit) && hit.collider.GetComponentInParent<NodeView>() == node;
        }

        private (NodeView from, NodeView to) FindPickableHousePair()
        {
            var houses = _views.Nodes.Where(IsPickable).ToList();
            foreach (var a in houses)
            foreach (var b in houses)
            {
                if (a != b && _sandbox.CheckCable(a.Id, b.Id, 1).IsValid)
                    return (a, b);
            }
            Assert.Inconclusive("No two visible houses within cable span.");
            return default;
        }

        // Punkt kabla, pod którym nie ma żadnego węzła (węzły mają pierwszeństwo przy wskazywaniu).
        private Vector2 FindPickablePointOn(CableView cable)
        {
            for (int i = 1; i < cable.Points.Count - 1; i++)
            {
                Vector2 screen = _camera.WorldToScreenPoint(cable.Points[i]);
                if (!Physics.Raycast(_camera.ScreenPointToRay(screen)))
                    return screen;
            }
            Assert.Inconclusive("Every point of the cable is covered by a node.");
            return default;
        }

        #endregion
    }
}
