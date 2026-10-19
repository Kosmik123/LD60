using UnityEngine;

namespace SpaghettiWires.Views
{
    /// <summary>
    /// Widok węzła sieci (dom, słup, centrala) na scenie. Musi mieć collider, żeby dało się go wskazać myszą.
    /// Punkty przyczepu kabli i ich zajętość obsługuje <see cref="CableAttachments"/>.
    /// </summary>
    [RequireComponent(typeof(CableAttachments))]
    public sealed class NodeView : MonoBehaviour
    {
        private CableAttachments _attachments;
        private Tint _tint;

        public int Id { get; private set; }
        public NodeVisualKind Kind { get; private set; }
        public Vector2Int Cell { get; private set; }

        public CableAttachments Attachments =>
            _attachments != null ? _attachments : _attachments = GetComponent<CableAttachments>();

        public void Initialize(int id, NodeVisualKind kind, Vector2Int cell)
        {
            Id = id;
            Kind = kind;
            Cell = cell;
            name = $"{kind}#{id}";
        }

        /// <summary>Gdzie zaczepi się kabel o podanym Id prowadzony w stronę podanego punktu (podgląd, sprawdzanie).</summary>
        public Vector3 GetAnchorPosition(int cableId, Vector3 towards) => Attachments.PreviewPosition(cableId, towards);

        // Tint tworzony leniwie: widoki bywają tworzone też w trybie edycji, gdzie Awake się nie wywołuje.
        public void SetHighlight(Color color, float amount) => (_tint ??= new Tint(gameObject)).Apply(color, amount);

        public void ClearHighlight() => (_tint ??= new Tint(gameObject)).Clear();
    }
}
