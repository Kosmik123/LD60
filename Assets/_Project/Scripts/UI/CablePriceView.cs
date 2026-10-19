using SpaghettiWires.Tools;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaghettiWires.UI
{
    /// <summary>
    /// Cena nad podglądem kabla, który gracz właśnie przeciąga. Zmienia się razem z długością podglądu.
    /// Czerwona, gdy gracza na nią nie stać.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class CablePriceView : MonoBehaviour
    {
        private const string UnaffordableClass = "price-tag--unaffordable";

        [SerializeField] private ToolController tools;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private StyleSheet styleSheet;

        private Label _price;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            root.styleSheets.Add(styleSheet);
            root.pickingMode = PickingMode.Ignore;

            _price = new Label { pickingMode = PickingMode.Ignore };
            _price.AddToClassList("price-tag");
            root.Add(_price);
            Hide();
        }

        // Po narzędziach (Update) i po ruchu kamery.
        private void LateUpdate()
        {
            var preview = tools.PricePreview;
            if (!preview.IsVisible || sceneCamera.WorldToScreenPoint(preview.Position).z <= 0f)
            {
                Hide();
                return;
            }

            Vector2 position = RuntimePanelUtils.CameraTransformWorldToPanel(_price.panel, preview.Position, sceneCamera);
            _price.style.left = position.x;
            _price.style.top = position.y;
            _price.text = HudFormat.Money(preview.Price);
            _price.EnableInClassList(UnaffordableClass, !preview.IsAffordable);
            _price.style.display = DisplayStyle.Flex;
        }

        private void Hide() => _price.style.display = DisplayStyle.None;
    }
}
