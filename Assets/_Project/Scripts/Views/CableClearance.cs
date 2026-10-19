using System.Collections.Generic;
using UnityEngine;

namespace SpaghettiWires.Views
{
    /// <summary>
    /// Czy kabel między dwoma węzłami da się poprowadzić bez przechodzenia przez budynek (dom, centralę).
    /// Sprawdza kształt kabla ze zwisem, odcinek po odcinku. Liczą się też budynki na końcach kabla:
    /// jeśli uchwyt jest na ścianie odwróconej od drugiego końca, kabel przeszedłby przez ten budynek.
    /// Słupy nie blokują kabli.
    /// </summary>
    public static class CableClearance
    {
        private const int Segments = 12;

        private static readonly List<Vector3> Points = new List<Vector3>();
        private static readonly RaycastHit[] Hits = new RaycastHit[16];

        public static bool IsClear(NodeView from, NodeView to, float sagPerMeter = CableCurve.DefaultSagPerMeter)
        {
            Vector3 a = from.GetAnchorPosition(to.transform.position);
            Vector3 b = to.GetAnchorPosition(from.transform.position);
            CableCurve.Sample(a, b, CableCurve.SagForSpan(a, b, sagPerMeter), Segments, Points);

            for (int i = 0; i + 1 < Points.Count; i++)
            {
                Vector3 direction = Points[i + 1] - Points[i];
                int count = Physics.RaycastNonAlloc(Points[i], direction.normalized, Hits, direction.magnitude,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

                for (int h = 0; h < count; h++)
                {
                    var node = Hits[h].collider.GetComponentInParent<NodeView>();
                    if (node != null && IsBuilding(node.Kind))
                        return false;
                }
            }
            return true;
        }

        private static bool IsBuilding(NodeVisualKind kind) =>
            kind == NodeVisualKind.House || kind == NodeVisualKind.Exchange;
    }
}
