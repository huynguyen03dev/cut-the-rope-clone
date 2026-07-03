using UnityEngine;

namespace Game.Core
{
    public static class SegmentMath
    {
        /// <summary>
        /// Segment–segment intersection (parametric form). Returns the
        /// intersection point when segments a1→a2 and b1→b2 cross. Parallel and
        /// collinear pairs report no intersection — good enough for swipe-vs-rope,
        /// where a following frame resolves the degenerate case.
        /// </summary>
        public static bool SegmentsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 point)
        {
            Vector2 r = a2 - a1;
            Vector2 s = b2 - b1;
            float denom = r.x * s.y - r.y * s.x;
            if (Mathf.Abs(denom) < 1e-9f)
            {
                point = default;
                return false;
            }

            Vector2 d = b1 - a1;
            float t = (d.x * s.y - d.y * s.x) / denom;
            float u = (d.x * r.y - d.y * r.x) / denom;
            if (t < 0f || t > 1f || u < 0f || u > 1f)
            {
                point = default;
                return false;
            }

            point = a1 + r * t;
            return true;
        }
    }
}
