using System.Collections.Generic;
using SpaghettiWires.Map;
using SpaghettiWires.Views;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SpaghettiWires.Tools
{
    /// <summary>
    /// Co jest pod kursorem myszy: punkt na ziemi i pole siatki, węzeł (collider) albo kabel.
    /// Liczone raz na klatkę, przed narzędziami.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class ScenePointer : MonoBehaviour
    {
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private MapGridBehaviour grid;
        [SerializeField] private ViewRegistry views;
        [SerializeField] private LayerMask nodeLayers = ~0;
        [Tooltip("Jak blisko kabla (w pikselach) musi być kursor, żeby go wskazać.")]
        [SerializeField] private float cablePickRadius = 10f;
        [Tooltip("Dokumenty UI, nad którymi kursor nie wskazuje sceny.")]
        [SerializeField] private List<UIDocument> blockingDocuments = new List<UIDocument>();

        public bool HasPointer { get; private set; }
        public Vector2 ScreenPosition { get; private set; }
        public bool IsOverUI { get; private set; }

        /// <summary>Punkt przecięcia promienia z płaszczyzną ziemi.</summary>
        public bool HasGroundPoint { get; private set; }
        public Vector3 GroundPoint { get; private set; }

        /// <summary>Pole siatki pod kursorem. Ważne tylko, gdy <see cref="IsCellInGrid"/>.</summary>
        public Vector2Int Cell { get; private set; }
        public bool IsCellInGrid { get; private set; }

        public NodeView HoveredNode { get; private set; }

        /// <summary>Kabel pod kursorem. Węzeł ma pierwszeństwo, więc gdy kursor jest nad węzłem, to null.</summary>
        public CableView HoveredCable { get; private set; }

        private void Update()
        {
            var mouse = Mouse.current;
            HasPointer = mouse != null;
            HoveredNode = null;
            HoveredCable = null;
            HasGroundPoint = false;
            IsCellInGrid = false;
            if (!HasPointer)
                return;

            ScreenPosition = mouse.position.ReadValue();
            IsOverUI = IsPointerOverUI(ScreenPosition);
            if (IsOverUI)
                return;

            Ray ray = sceneCamera.ScreenPointToRay(ScreenPosition);
            var mapGrid = grid.Grid;
            var ground = new Plane(Vector3.up, mapGrid.Origin);
            if (ground.Raycast(ray, out float enter))
            {
                HasGroundPoint = true;
                GroundPoint = ray.GetPoint(enter);
                IsCellInGrid = mapGrid.TryWorldToCell(GroundPoint, out var cell);
                Cell = cell;
            }

            if (Physics.Raycast(ray, out var hit, sceneCamera.farClipPlane, nodeLayers))
                HoveredNode = hit.collider.GetComponentInParent<NodeView>();

            if (HoveredNode == null)
                HoveredCable = FindCableUnderPointer();
        }

        private CableView FindCableUnderPointer()
        {
            CableView best = null;
            float bestDistance = cablePickRadius;
            foreach (var cable in views.Cables)
            {
                float distance = ScreenDistanceToPolyline(cable.Points);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = cable;
                }
            }
            return best;
        }

        // Kable są cienkie, więc wskazujemy je po odległości na ekranie, a nie colliderem.
        private float ScreenDistanceToPolyline(IReadOnlyList<Vector3> points)
        {
            float best = float.MaxValue;
            for (int i = 0; i + 1 < points.Count; i++)
            {
                Vector3 a = sceneCamera.WorldToScreenPoint(points[i]);
                Vector3 b = sceneCamera.WorldToScreenPoint(points[i + 1]);
                if (a.z <= 0f || b.z <= 0f)
                    continue; // odcinek za kamerą

                best = Mathf.Min(best, DistanceToSegment(ScreenPosition, a, b));
            }
            return best;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(p, a + ab * t);
        }

        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            foreach (var document in blockingDocuments)
            {
                var panel = document != null ? document.rootVisualElement?.panel : null;
                if (panel == null)
                    continue;

                // Panel liczy od lewego górnego rogu, ekran od lewego dolnego.
                var panelPosition = RuntimePanelUtils.ScreenToPanel(panel,
                    new Vector2(screenPosition.x, Screen.height - screenPosition.y));
                if (panel.Pick(panelPosition) != null)
                    return true;
            }
            return false;
        }
    }
}
