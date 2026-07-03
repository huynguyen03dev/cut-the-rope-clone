using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    public class SegmentMathTests
    {
        [Test]
        public void Crossing_Segments_Intersect_At_Expected_Point()
        {
            bool hit = SegmentMath.SegmentsIntersect(
                new Vector2(-1f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, -1f), new Vector2(0f, 1f),
                out Vector2 p);
            Assert.IsTrue(hit);
            Assert.That(p.x, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(p.y, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void NonCrossing_Segments_Do_Not_Intersect()
        {
            bool hit = SegmentMath.SegmentsIntersect(
                new Vector2(-1f, 0f), new Vector2(1f, 0f),
                new Vector2(2f, -1f), new Vector2(2f, 1f),
                out _);
            Assert.IsFalse(hit, "lines cross but the segments do not");
        }

        [Test]
        public void Segments_Whose_Extensions_Cross_Do_Not_Intersect()
        {
            bool hit = SegmentMath.SegmentsIntersect(
                new Vector2(-1f, 0f), new Vector2(-0.1f, 0f),
                new Vector2(0f, -1f), new Vector2(0f, 1f),
                out _);
            Assert.IsFalse(hit);
        }

        [Test]
        public void Parallel_Segments_Do_Not_Intersect()
        {
            bool hit = SegmentMath.SegmentsIntersect(
                new Vector2(-1f, 0f), new Vector2(1f, 0f),
                new Vector2(-1f, 1f), new Vector2(1f, 1f),
                out _);
            Assert.IsFalse(hit);
        }

        [Test]
        public void Endpoint_Touch_Counts_As_Intersection()
        {
            bool hit = SegmentMath.SegmentsIntersect(
                new Vector2(-1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(2f, 5f),
                out Vector2 p);
            Assert.IsTrue(hit);
            Assert.That(p.x, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void Diagonal_Crossing_Returns_Correct_Point()
        {
            bool hit = SegmentMath.SegmentsIntersect(
                new Vector2(0f, 0f), new Vector2(2f, 2f),
                new Vector2(0f, 2f), new Vector2(2f, 0f),
                out Vector2 p);
            Assert.IsTrue(hit);
            Assert.That(p.x, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(p.y, Is.EqualTo(1f).Within(1e-5f));
        }
    }
}
