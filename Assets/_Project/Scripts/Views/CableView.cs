using System.Collections.Generic;
using UnityEngine;

namespace SpaghettiWires.Views
{
    /// <summary>
    /// Widok kabla między dwoma węzłami: <see cref="LineRenderer"/> ze zwisem.
    /// Grubość rośnie jak pierwiastek z liczby żył (GDD 3.3, 5).
    /// Każdy kabel ma trochę inny zwis, żeby kable między tymi samymi węzłami się nie nakładały
    /// i żeby plątanina wyglądała naturalnie.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class CableView : MonoBehaviour
    {
        private const int Segments = 16;

        [SerializeField] private float widthPerSqrtPair = 0.05f;
        [SerializeField] private float sagPerMeter = CableCurve.DefaultSagPerMeter;
        [Tooltip("Losowe odchylenie zwisu, ±procent.")]
        [Range(0f, 1f)] [SerializeField] private float sagVariation = 0.4f;
        [SerializeField] private Color color = new Color(0.08f, 0.08f, 0.08f);

        private readonly List<Vector3> _points = new List<Vector3>();
        private LineRenderer _line;
        private float _sagScale = 1f;

        public int Id { get; private set; }
        public NodeView From { get; private set; }
        public NodeView To { get; private set; }
        public int Pairs { get; private set; }

        /// <summary>Punkty kabla w świecie, np. do wskazywania myszą.</summary>
        public IReadOnlyList<Vector3> Points => _points;

        public void Initialize(int id, NodeView from, NodeView to, int pairs)
        {
            Id = id;
            From = from;
            To = to;
            Pairs = pairs;
            name = $"Cable#{id} ({from.Id}-{to.Id})";

            // Zwis zależny od Id, więc ten sam kabel zawsze wygląda tak samo.
            var random = new System.Random(id);
            _sagScale = 1f + ((float)random.NextDouble() * 2f - 1f) * sagVariation;

            // Kabel rezerwuje punkt przyczepu na obu końcach i trzyma go aż do odczepienia.
            from.Attachments.Attach(id, pairs, to.transform.position);
            to.Attachments.Attach(id, pairs, from.transform.position);

            EnsureLine();
            _line.widthMultiplier = widthPerSqrtPair * Mathf.Sqrt(pairs);
            ClearHighlight();
            Refresh();
        }

        /// <summary>Zwalnia punkty przyczepu na obu końcach. Wołane przed usunięciem kabla.</summary>
        public void Detach()
        {
            From.Attachments.Detach(Id);
            To.Attachments.Detach(Id);
        }

        /// <summary>Przelicza kształt kabla, np. gdy przesunął się któryś z węzłów.</summary>
        public void Refresh()
        {
            Vector3 a = From.Attachments.GetAnchorPosition(Id);
            Vector3 b = To.Attachments.GetAnchorPosition(Id);
            float sag = CableCurve.SagForSpan(a, b, sagPerMeter) * _sagScale;
            CableCurve.Sample(a, b, sag, Segments, _points);

            _line.positionCount = _points.Count;
            for (int i = 0; i < _points.Count; i++)
                _line.SetPosition(i, _points[i]);
        }

        public void SetHighlight(Color highlight) => SetColor(highlight);
        public void ClearHighlight() => SetColor(color);

        private void SetColor(Color value)
        {
            EnsureLine();
            _line.startColor = value;
            _line.endColor = value;
        }

        private void EnsureLine()
        {
            if (_line != null)
                return;

            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.numCapVertices = 2;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }
}
