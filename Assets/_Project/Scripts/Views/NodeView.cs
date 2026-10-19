using System.Collections.Generic;
using UnityEngine;

namespace SpaghettiWires.Views
{
    /// <summary>
    /// Widok węzła sieci (dom, słup, centrala) na scenie. Musi mieć collider, żeby dało się go wskazać myszą.
    /// Punkty zaczepienia kabli to dzieci o nazwach zaczynających się od „CableAnchor”
    /// (tak jak w prefabie drewnianego słupa).
    /// </summary>
    public sealed class NodeView : MonoBehaviour
    {
        private const string AnchorPrefix = "CableAnchor";

        [Tooltip("Wysokość zaczepienia kabla, gdy model nie ma punktów „CableAnchor”.")]
        [SerializeField] private float fallbackAnchorHeight = 3f;

        private readonly List<Transform> _anchors = new List<Transform>();
        private Tint _tint;

        public int Id { get; private set; }
        public NodeVisualKind Kind { get; private set; }
        public Vector2Int Cell { get; private set; }

        public void Initialize(int id, NodeVisualKind kind, Vector2Int cell)
        {
            Id = id;
            Kind = kind;
            Cell = cell;
            name = $"{kind}#{id}";
        }

        // Leniwie, a nie w Awake: widoki bywają tworzone też w trybie edycji, gdzie Awake się nie wywołuje.
        private void EnsureInitialized()
        {
            if (_tint != null)
                return;

            foreach (var child in GetComponentsInChildren<Transform>())
            {
                if (child.name.StartsWith(AnchorPrefix))
                    _anchors.Add(child);
            }

            _tint = new Tint(gameObject);
        }

        /// <summary>Punkt zaczepienia kabla najbliższy podanemu punktowi (np. drugiemu końcowi kabla).</summary>
        public Vector3 GetAnchorPosition(Vector3 towards)
        {
            EnsureInitialized();
            if (_anchors.Count == 0)
                return transform.position + Vector3.up * fallbackAnchorHeight;

            Vector3 best = _anchors[0].position;
            float bestDistance = float.MaxValue;
            foreach (var anchor in _anchors)
            {
                float distance = (anchor.position - towards).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = anchor.position;
                }
            }
            return best;
        }

        public void SetHighlight(Color color, float amount)
        {
            EnsureInitialized();
            _tint.Apply(color, amount);
        }

        public void ClearHighlight()
        {
            EnsureInitialized();
            _tint.Clear();
        }
    }
}
