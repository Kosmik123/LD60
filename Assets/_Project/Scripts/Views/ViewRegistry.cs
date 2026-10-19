using System.Collections.Generic;
using SpaghettiWires.Map;
using UnityEngine;

namespace SpaghettiWires.Views
{
    /// <summary>
    /// Tworzy i usuwa widoki węzłów i kabli oraz trzyma słownik Id → widok (TECH 7).
    /// Id pochodzą z sieci: warstwa, która słucha zdarzeń sieci, woła tutaj Spawn/Remove.
    /// </summary>
    public sealed class ViewRegistry : MonoBehaviour
    {
        [SerializeField] private MapGridBehaviour grid;

        [Header("Prefaby węzłów")]
        [SerializeField] private NodeView housePrefab;
        [SerializeField] private NodeView polePrefab;
        [SerializeField] private NodeView exchangePrefab;

        [Header("Kable")]
        [SerializeField] private Material cableMaterial;

        private readonly Dictionary<int, NodeView> _nodes = new Dictionary<int, NodeView>();
        private readonly Dictionary<int, CableView> _cables = new Dictionary<int, CableView>();

        private Transform _nodesRoot;
        private Transform _cablesRoot;

        public IReadOnlyCollection<NodeView> Nodes => _nodes.Values;
        public IReadOnlyCollection<CableView> Cables => _cables.Values;

        public NodeView SpawnNode(int id, NodeVisualKind kind, Vector2Int cell)
        {
            var view = Instantiate(GetPrefab(kind), grid.Grid.CellToWorld(cell), GetRotation(id, kind), GetRoot(ref _nodesRoot, "Nodes"));
            view.Initialize(id, kind, cell);
            _nodes.Add(id, view);
            return view;
        }

        /// <summary>Usuwa widok węzła. Kable węzła trzeba usunąć wcześniej (sieć zgłasza to osobnymi zdarzeniami).</summary>
        public void RemoveNode(int id)
        {
            if (_nodes.Remove(id, out var view))
                Destroy(view.gameObject);
        }

        public CableView SpawnCable(int id, int fromNodeId, int toNodeId, int pairs)
        {
            var cableObject = new GameObject();
            cableObject.transform.SetParent(GetRoot(ref _cablesRoot, "Cables"), false);
            cableObject.AddComponent<LineRenderer>().sharedMaterial = cableMaterial;

            var view = cableObject.AddComponent<CableView>();
            view.Initialize(id, _nodes[fromNodeId], _nodes[toNodeId], pairs);
            _cables.Add(id, view);
            return view;
        }

        public void RemoveCable(int id)
        {
            if (_cables.Remove(id, out var view))
            {
                view.Detach();
                Destroy(view.gameObject);
            }
        }

        // Domy obrócone losowo o wielokrotność 90°, żeby punkt przyczepu na ścianie wypadał z różnych stron.
        // Losowanie zależy od Id, więc dom zawsze stoi tak samo.
        private static Quaternion GetRotation(int id, NodeVisualKind kind) =>
            kind == NodeVisualKind.House
                ? Quaternion.Euler(0f, 90f * new System.Random(id).Next(4), 0f)
                : Quaternion.identity;

        public bool TryGetNode(int id, out NodeView view) => _nodes.TryGetValue(id, out view);

        /// <summary>Czy kabel między węzłami nie przeszedłby przez żaden budynek (<see cref="CableClearance"/>).</summary>
        public bool IsCablePathClear(int fromNodeId, int toNodeId) =>
            CableClearance.IsClear(_nodes[fromNodeId], _nodes[toNodeId]);
        public bool TryGetCable(int id, out CableView view) => _cables.TryGetValue(id, out view);

        private NodeView GetPrefab(NodeVisualKind kind) => kind switch
        {
            NodeVisualKind.House => housePrefab,
            NodeVisualKind.Pole => polePrefab,
            NodeVisualKind.Exchange => exchangePrefab,
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        private Transform GetRoot(ref Transform root, string rootName)
        {
            if (root == null)
            {
                root = new GameObject(rootName).transform;
                root.SetParent(transform, false);
            }
            return root;
        }
    }
}
