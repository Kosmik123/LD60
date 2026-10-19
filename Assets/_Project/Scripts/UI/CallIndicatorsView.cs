using System.Collections.Generic;
using SpaghettiWires.Networking;
using SpaghettiWires.Simulation;
using SpaghettiWires.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaghettiWires.UI
{
    /// <summary>
    /// Znaczniki nad domami (PLAN etap 5): kropka, gdy dom rozmawia, i na chwilę napis,
    /// gdy rozmowa się nie udała („zajęte”, „brak połączenia”) albo została zerwana.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class CallIndicatorsView : MonoBehaviour
    {
        private sealed class Indicator
        {
            public VisualElement Root;
            public VisualElement Dot;
            public Label Message;
            public int Calls;
            public float MessageUntil;
        }

        [SerializeField] private CallSimulationRunner simulation;
        [SerializeField] private ViewRegistry views;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private StyleSheet styleSheet;
        [Tooltip("Wysokość znacznika nad podstawą domu (m).")]
        [SerializeField] private float heightAboveNode = 5.5f;
        [Tooltip("Jak długo widać napis o nieudanej lub zerwanej rozmowie (s).")]
        [SerializeField] private float messageDuration = 2.5f;

        private readonly Dictionary<NodeId, Indicator> _indicators = new Dictionary<NodeId, Indicator>();
        private readonly List<NodeId> _expired = new List<NodeId>();
        private VisualElement _root;
        private CallSimulation _simulation;

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _root.Clear();
            _root.styleSheets.Add(styleSheet);
            _root.pickingMode = PickingMode.Ignore;
            _indicators.Clear();
        }

        // W Start, a nie w OnEnable: symulacja tworzy sieć dopiero przy pierwszym użyciu.
        private void Start()
        {
            _simulation = simulation.Simulation;
            _simulation.CallConnected += OnCallConnected;
            _simulation.CallCompleted += OnCallCompleted;
            _simulation.CallDropped += OnCallDropped;
            _simulation.CallFailed += OnCallFailed;
        }

        private void OnDestroy()
        {
            if (_simulation == null)
                return;

            _simulation.CallConnected -= OnCallConnected;
            _simulation.CallCompleted -= OnCallCompleted;
            _simulation.CallDropped -= OnCallDropped;
            _simulation.CallFailed -= OnCallFailed;
        }

        private void OnCallConnected(Call call)
        {
            Get(call.From).Calls++;
            Get(call.To).Calls++;
        }

        private void OnCallCompleted(Call call)
        {
            Get(call.From).Calls--;
            Get(call.To).Calls--;
        }

        private void OnCallDropped(Call call)
        {
            OnCallCompleted(call);
            ShowMessage(call.From, "DROPPED");
            ShowMessage(call.To, "DROPPED");
        }

        private void OnCallFailed(NodeId from, NodeId to, CallOutcome outcome)
        {
            string message = outcome switch
            {
                CallOutcome.CalleeBusy => "BUSY",
                CallOutcome.NoRoute => "NO CONNECTION",
                _ => "LINE BUSY",
            };
            ShowMessage(from, message);
        }

        private void ShowMessage(NodeId nodeId, string message)
        {
            var indicator = Get(nodeId);
            indicator.Message.text = message;
            indicator.MessageUntil = Time.time + messageDuration;
        }

        // Po ruchu kamery, żeby znaczniki nie zostawały klatkę w tyle.
        private void LateUpdate()
        {
            _expired.Clear();
            foreach (var (nodeId, indicator) in _indicators)
            {
                bool hasMessage = Time.time < indicator.MessageUntil;
                if (!views.TryGetNode(nodeId.Value, out var node) || (indicator.Calls <= 0 && !hasMessage))
                {
                    _expired.Add(nodeId);
                    continue;
                }

                Vector3 anchor = node.transform.position + Vector3.up * heightAboveNode;
                bool visible = sceneCamera.WorldToScreenPoint(anchor).z > 0f;
                indicator.Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!visible)
                    continue;

                Vector2 position = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, anchor, sceneCamera);
                indicator.Root.style.left = position.x;
                indicator.Root.style.top = position.y;
                indicator.Dot.style.display = indicator.Calls > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                indicator.Message.style.display = hasMessage ? DisplayStyle.Flex : DisplayStyle.None;
            }

            foreach (var nodeId in _expired)
            {
                _indicators[nodeId].Root.RemoveFromHierarchy();
                _indicators.Remove(nodeId);
            }
        }

        private Indicator Get(NodeId nodeId)
        {
            if (_indicators.TryGetValue(nodeId, out var indicator))
                return indicator;

            indicator = new Indicator
            {
                Root = new VisualElement { pickingMode = PickingMode.Ignore },
                Dot = new VisualElement { pickingMode = PickingMode.Ignore },
                Message = new Label { pickingMode = PickingMode.Ignore },
            };
            indicator.Root.AddToClassList("call-indicator");
            indicator.Dot.AddToClassList("call-indicator__dot");
            indicator.Message.AddToClassList("call-indicator__message");
            indicator.Root.Add(indicator.Message);
            indicator.Root.Add(indicator.Dot);
            _root.Add(indicator.Root);
            _indicators.Add(nodeId, indicator);
            return indicator;
        }
    }
}
