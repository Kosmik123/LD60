using SpaghettiWires.Tools;
using SpaghettiWires.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaghettiWires.UI
{
    /// <summary>
    /// Okienko nad słupem pod kursorem z zajętością miejsc, np. „7/12” (GDD 4).
    /// Element UI na ekranie, przesuwany co klatkę do rzutu punktu nad słupem.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class PoleTooltipView : MonoBehaviour
    {
        private const string FullClass = "node-tooltip--full";

        [SerializeField] private ScenePointer pointer;
        [SerializeField] private ToolController tools;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private StyleSheet styleSheet;
        [Tooltip("Wysokość okienka nad podstawą słupa (m).")]
        [SerializeField] private float heightAboveNode = 8f;

        private VisualElement _tooltip;
        private Label _slots;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            root.styleSheets.Add(styleSheet);
            root.pickingMode = PickingMode.Ignore;

            _tooltip = new VisualElement { pickingMode = PickingMode.Ignore };
            _tooltip.AddToClassList("node-tooltip");

            var caption = new Label("SLOTS") { pickingMode = PickingMode.Ignore };
            caption.AddToClassList("node-tooltip__caption");
            _tooltip.Add(caption);

            _slots = new Label { pickingMode = PickingMode.Ignore };
            _slots.AddToClassList("node-tooltip__value");
            _tooltip.Add(_slots);

            root.Add(_tooltip);
            Hide();
        }

        // Po ScenePointer (Update) i po ruchu kamery, żeby okienko nie zostawało klatkę w tyle.
        private void LateUpdate()
        {
            var node = pointer.HoveredNode;
            var actions = tools.BuildActions;
            if (node == null || node.Kind != NodeVisualKind.Pole || actions == null
                || !actions.TryGetSlotUsage(node.Id, out int used, out int capacity))
            {
                Hide();
                return;
            }

            Vector3 anchor = node.transform.position + Vector3.up * heightAboveNode;
            if (sceneCamera.WorldToScreenPoint(anchor).z <= 0f)
            {
                Hide();
                return;
            }

            Vector2 position = RuntimePanelUtils.CameraTransformWorldToPanel(_tooltip.panel, anchor, sceneCamera);
            _tooltip.style.left = position.x;
            _tooltip.style.top = position.y;
            _slots.text = $"{used}/{capacity}";
            _tooltip.EnableInClassList(FullClass, used >= capacity);
            _tooltip.style.display = DisplayStyle.Flex;
        }

        private void Hide() => _tooltip.style.display = DisplayStyle.None;
    }
}
