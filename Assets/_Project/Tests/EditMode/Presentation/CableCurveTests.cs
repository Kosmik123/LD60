using System.Collections.Generic;
using NUnit.Framework;
using SpaghettiWires.Views;
using UnityEngine;

namespace SpaghettiWires.Tests
{
    public class CableCurveTests
    {
        [Test]
        public void Sample_StartsAndEndsAtAnchors()
        {
            var points = new List<Vector3>();
            var a = new Vector3(0f, 5f, 0f);
            var b = new Vector3(10f, 7f, 0f);

            CableCurve.Sample(a, b, sag: 1f, segments: 8, points);

            Assert.AreEqual(9, points.Count);
            Assert.AreEqual(a, points[0]);
            Assert.AreEqual(b, points[8]);
        }

        [Test]
        public void Sample_SagsByGivenAmountInTheMiddle()
        {
            var points = new List<Vector3>();

            CableCurve.Sample(new Vector3(0f, 5f, 0f), new Vector3(10f, 5f, 0f), sag: 0.5f, segments: 10, points);

            Assert.AreEqual(4.5f, points[5].y, 1e-5f);
            for (int i = 1; i < 10; i++)
                Assert.Less(points[i].y, 5f, "Every inner point hangs below the anchors");
        }

        [Test]
        public void SagForSpan_DependsOnHorizontalLengthOnly()
        {
            float flat = CableCurve.SagForSpan(Vector3.zero, new Vector3(3f, 0f, 4f), 0.1f);
            float sloped = CableCurve.SagForSpan(Vector3.zero, new Vector3(3f, 20f, 4f), 0.1f);

            Assert.AreEqual(0.5f, flat, 1e-5f);
            Assert.AreEqual(flat, sloped, 1e-5f);
        }
    }
}
