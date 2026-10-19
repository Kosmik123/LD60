using System.Collections.Generic;
using UnityEngine;

namespace SpaghettiWires.Views
{
    /// <summary>
    /// Kształt zwisającego kabla. Przy małym zwisie krzywa łańcuchowa jest praktycznie
    /// nie do odróżnienia od paraboli, więc liczymy parabolę.
    /// </summary>
    public static class CableCurve
    {
        /// <summary>
        /// Wypełnia <paramref name="points"/> punktami kabla od <paramref name="a"/> do <paramref name="b"/>.
        /// <paramref name="sag"/> to zwis w najniższym punkcie (w metrach, poniżej prostej a–b).
        /// </summary>
        public static void Sample(Vector3 a, Vector3 b, float sag, int segments, List<Vector3> points)
        {
            points.Clear();
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                points.Add(Vector3.Lerp(a, b, t) + Vector3.down * (4f * sag * t * (1f - t)));
            }
        }

        /// <summary>Zwis zależny od poziomej długości przęsła.</summary>
        public static float SagForSpan(Vector3 a, Vector3 b, float sagPerMeter)
        {
            var horizontal = new Vector2(b.x - a.x, b.z - a.z);
            return horizontal.magnitude * sagPerMeter;
        }
    }
}
