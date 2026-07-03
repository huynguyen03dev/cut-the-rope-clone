using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Position-based dynamics rope solver (DESIGN §2). The candy is a single
    /// heavy Verlet point shared as the terminal constraint target of every
    /// attached rope. Runs on a fixed timestep driven from outside; render
    /// interpolation reads lerp(RenderPos, Pos, alpha).
    /// </summary>
    public sealed class RopeSimulation
    {
        public RopePoint Candy;
        public readonly List<Rope> Ropes = new List<Rope>();

        public Vector2 Gravity = new Vector2(0f, -20f);
        public float Damping = 0.99f;
        public int Iterations = 16;

        public RopeSimulation(Vector2 candyPos, float candyInvMass)
        {
            Candy = RopePoint.At(candyPos, candyInvMass);
        }

        /// <summary>
        /// Spawns a rope from a pinned anchor to the current candy position.
        /// Rest length is the anchor→candy distance at spawn and every point
        /// starts with PrevPos = Pos, so attaching injects zero energy.
        /// </summary>
        public Rope AddRope(Vector2 anchorPos, int segments)
        {
            var pts = new RopePoint[segments]; // anchor + interior; candy is the last segment's far end
            pts[0] = RopePoint.At(anchorPos, 0f);
            for (int i = 1; i < segments; i++)
            {
                pts[i] = RopePoint.At(Vector2.Lerp(anchorPos, Candy.Pos, (float)i / segments), 1f);
            }

            var rope = new Rope
            {
                Points = pts,
                RestLength = Vector2.Distance(anchorPos, Candy.Pos) / segments,
                AttachedToCandy = true,
                Cuttable = true,
            };
            Ropes.Add(rope);
            return rope;
        }

        public void Step(float dt)
        {
            SnapshotRenderPositions();
            Integrate(dt);
            SolveConstraints();
        }

        /// <summary>
        /// External forces (air puffs, buoyancy) shift the implied Verlet
        /// velocity: prevPos -= impulse * dt (DESIGN §2).
        /// </summary>
        public void ApplyCandyImpulse(Vector2 impulse, float dt)
        {
            Candy.PrevPos -= impulse * dt;
        }

        public static Vector2 Interpolate(in RopePoint p, float alpha)
            => Vector2.LerpUnclamped(p.RenderPos, p.Pos, alpha);

        public Vector2 CandyInterpolated(float alpha) => Interpolate(Candy, alpha);

        /// <summary>Implied candy velocity from Verlet state.</summary>
        public Vector2 CandyVelocity(float dt) => (Candy.Pos - Candy.PrevPos) / dt;

        /// <summary>
        /// Tests the swipe segment a→b against every cuttable rope segment at the
        /// interpolated positions the player sees (raw positions as a fallback,
        /// matching the prototype), cutting at most one rope. On a hit the severed
        /// constraint splits the rope into an anchor-side stub and a candy-side
        /// stub; both leave the swipe test set immediately.
        /// </summary>
        public bool TryCut(Vector2 swipeA, Vector2 swipeB, float alpha, out Vector2 cutPos)
        {
            for (int r = 0; r < Ropes.Count; r++)
            {
                Rope rope = Ropes[r];
                if (!rope.Cuttable) continue;

                RopePoint[] pts = rope.Points;
                for (int i = 0; i < pts.Length - 1; i++)
                {
                    if (SegmentHit(swipeA, swipeB, in pts[i], in pts[i + 1], alpha, out cutPos))
                    {
                        Split(rope, i);
                        return true;
                    }
                }

                if (rope.AttachedToCandy &&
                    SegmentHit(swipeA, swipeB, in pts[pts.Length - 1], in Candy, alpha, out cutPos))
                {
                    Split(rope, pts.Length - 1);
                    return true;
                }
            }

            cutPos = default;
            return false;
        }

        static bool SegmentHit(Vector2 a, Vector2 b, in RopePoint p, in RopePoint q, float alpha, out Vector2 hit)
        {
            return SegmentMath.SegmentsIntersect(a, b, Interpolate(p, alpha), Interpolate(q, alpha), out hit)
                || SegmentMath.SegmentsIntersect(a, b, p.Pos, q.Pos, out hit);
        }

        /// <summary>
        /// Severs the constraint between Points[segmentIndex] and its successor
        /// (the candy itself when segmentIndex is the last point). The rope
        /// becomes the anchor-side stub; the points below the cut become a
        /// candy-side stub that keeps swinging from the candy. Stub retract/fade
        /// and double-cut despawn are US-003.
        /// </summary>
        void Split(Rope rope, int segmentIndex)
        {
            rope.Cuttable = false;
            bool wasAttached = rope.AttachedToCandy;
            rope.AttachedToCandy = false;

            RopePoint[] pts = rope.Points;
            int lowerCount = pts.Length - (segmentIndex + 1);
            if (lowerCount == 0) return; // cut the terminal segment: candy just detaches

            var upper = new RopePoint[segmentIndex + 1];
            System.Array.Copy(pts, 0, upper, 0, upper.Length);
            var lower = new RopePoint[lowerCount];
            System.Array.Copy(pts, segmentIndex + 1, lower, 0, lowerCount);
            rope.Points = upper;

            Ropes.Add(new Rope
            {
                Points = lower,
                RestLength = rope.RestLength,
                AttachedToCandy = wasAttached,
                Cuttable = false,
            });
        }

        void SnapshotRenderPositions()
        {
            for (int r = 0; r < Ropes.Count; r++)
            {
                RopePoint[] pts = Ropes[r].Points;
                for (int i = 0; i < pts.Length; i++) pts[i].RenderPos = pts[i].Pos;
            }
            Candy.RenderPos = Candy.Pos;
        }

        void Integrate(float dt)
        {
            Vector2 gDt2 = Gravity * (dt * dt);
            for (int r = 0; r < Ropes.Count; r++)
            {
                RopePoint[] pts = Ropes[r].Points;
                for (int i = 0; i < pts.Length; i++) IntegratePoint(ref pts[i], gDt2, Damping);
            }
            IntegratePoint(ref Candy, gDt2, Damping);
        }

        static void IntegratePoint(ref RopePoint p, Vector2 gDt2, float damping)
        {
            if (p.InvMass <= 0f) return;
            Vector2 vel = (p.Pos - p.PrevPos) * damping;
            p.PrevPos = p.Pos;
            p.Pos += vel + gDt2;
        }

        void SolveConstraints()
        {
            for (int it = 0; it < Iterations; it++)
            {
                for (int r = 0; r < Ropes.Count; r++)
                {
                    Rope rope = Ropes[r];
                    RopePoint[] pts = rope.Points;
                    for (int i = 0; i < pts.Length - 1; i++)
                    {
                        SolvePair(ref pts[i], ref pts[i + 1], rope.RestLength);
                    }
                    if (rope.AttachedToCandy)
                    {
                        SolvePair(ref pts[pts.Length - 1], ref Candy, rope.RestLength);
                    }
                }
            }
        }

        /// <summary>
        /// Mass-weighted relaxation: the anchor (invMass 0) never moves, rope
        /// points absorb most correction, the heavy candy barely moves — the rope
        /// feels light and the candy feels heavy.
        /// </summary>
        static void SolvePair(ref RopePoint a, ref RopePoint b, float rest)
        {
            Vector2 delta = b.Pos - a.Pos;
            float dist = delta.magnitude;
            if (dist < 1e-6f) return;
            float wSum = a.InvMass + b.InvMass;
            if (wSum <= 0f) return;

            float error = (dist - rest) / dist;
            a.Pos += delta * (error * (a.InvMass / wSum));
            b.Pos -= delta * (error * (b.InvMass / wSum));
        }
    }
}
