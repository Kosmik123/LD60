using System.Collections.Generic;
using UnityEngine;

namespace SpaghettiWires.Views
{
    /// <summary>Podgląd kabla w trakcie przeciągania, zanim gracz go położy.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class CablePreview : MonoBehaviour
    {
        private const int Segments = 16;

        [SerializeField] private float widthPerSqrtPair = 0.06f;
        [SerializeField] private float sagPerMeter = 0.05f;

        private readonly List<Vector3> _points = new List<Vector3>();
        private LineRenderer _line;

        private LineRenderer Line => _line != null ? _line : _line = GetComponent<LineRenderer>();

        public void Show(Vector3 from, Vector3 to, int pairs, Color color)
        {
            CableCurve.Sample(from, to, CableCurve.SagForSpan(from, to, sagPerMeter), Segments, _points);
            Line.positionCount = _points.Count;
            for (int i = 0; i < _points.Count; i++)
                Line.SetPosition(i, _points[i]);

            Line.widthMultiplier = widthPerSqrtPair * Mathf.Sqrt(pairs);
            Line.startColor = color;
            Line.endColor = color;
            Line.enabled = true;
        }

        public void Hide() => Line.enabled = false;
    }
}
