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

        /// <summary>US-006 buoyancy: gravity multiplier for the candy point ONLY (DESIGN §2
        /// "gravity can be flipped per-point"). 1 = normal weight; a bubble flips this
        /// negative so the candy floats upward, and popping restores it to 1. It scales only
        /// future acceleration and never touches Pos/PrevPos, so changing it mid-swing injects
        /// zero energy — the candy's implied velocity is unchanged at the attach/pop frame.
        /// Owned by the bubble logic (CandyInteractor); the driver does NOT reset it per step.</summary>
        public float CandyGravityScale = 1f;

        /// <summary>US-003 cut aftermath: candy-side stub fade/retract duration (seconds).</summary>
        public float StubFadeDuration = 0.5f;

        /// <summary>Per-second lerp factor for the candy-side stub retract toward the candy.</summary>
        public float RetractRate = 6f;

        public RopeSimulation(Vector2 candyPos, float candyInvMass)
        {
            Candy = RopePoint.At(candyPos, candyInvMass);
        }

        /// <summary>
        /// Spawns a rope from a pinned anchor to the current candy position. Rest length
        /// defaults to the anchor→candy distance and every point starts with PrevPos = Pos,
        /// so attaching injects zero energy. Equivalent to
        /// <see cref="AddRope(Vector2,int,float)"/> with per-segment rest = distance/segments.
        /// </summary>
        public Rope AddRope(Vector2 anchorPos, int segments)
            => AddRope(anchorPos, segments, Vector2.Distance(anchorPos, Candy.Pos) / segments);

        /// <summary>
        /// Spawns a candy-attached rope from a pinned anchor with an explicit PER-SEGMENT rest
        /// length (US-008 RopeAuthoring). Pass <paramref name="restLengthPerSegment"/> ≤ 0 to
        /// fall back to the anchor→candy distance (zero-energy attach, matches auto-grab).
        ///
        /// Points always start with PrevPos = Pos regardless of the authored rest length, so the
        /// attach injects zero *velocity*; an authored rest length that differs from the actual
        /// point spacing is intentional — the first solve step settles the rope to the designer's
        /// taut/slack shape rather than the natural hang.
        /// </summary>
        public Rope AddRope(Vector2 anchorPos, int segments, float restLengthPerSegment)
        {
            if (restLengthPerSegment <= 0f)
                restLengthPerSegment = Vector2.Distance(anchorPos, Candy.Pos) / segments;

            var pts = new RopePoint[segments]; // anchor + interior; candy is the last segment's far end
            pts[0] = RopePoint.At(anchorPos, 0f);
            for (int i = 1; i < segments; i++)
            {
                pts[i] = RopePoint.At(Vector2.Lerp(anchorPos, Candy.Pos, (float)i / segments), 1f);
            }

            var rope = new Rope
            {
                Points = pts,
                RestLength = restLengthPerSegment,
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
            TickFades(dt);
            DetectFreePieces();
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
            => TryCut(swipeA, swipeB, alpha, out cutPos, out _);

        /// <summary>
        /// As above, but also returns <paramref name="severedLower"/> — the candy-side
        /// stub created by the cut, or null when the terminal segment is cut (the candy
        /// just detaches) or when nothing is hit. The candy-side stub starts fading
        /// immediately so it retracts toward the candy and despawns within
        /// <see cref="StubFadeDuration"/> (DESIGN §2 cut aftermath).
        /// </summary>
        public bool TryCut(Vector2 swipeA, Vector2 swipeB, float alpha, out Vector2 cutPos, out Rope severedLower)
        {
            severedLower = null;
            for (int r = 0; r < Ropes.Count; r++)
            {
                Rope rope = Ropes[r];
                if (!rope.Cuttable) continue;

                RopePoint[] pts = rope.Points;
                for (int i = 0; i < pts.Length - 1; i++)
                {
                    if (SegmentHit(swipeA, swipeB, in pts[i], in pts[i + 1], alpha, out cutPos))
                    {
                        severedLower = Split(rope, i);
                        FadeCandyStub(severedLower);
                        return true;
                    }
                }

                if (rope.AttachedToCandy &&
                    SegmentHit(swipeA, swipeB, in pts[pts.Length - 1], in Candy, alpha, out cutPos))
                {
                    severedLower = Split(rope, pts.Length - 1);
                    FadeCandyStub(severedLower);
                    return true;
                }
            }

            cutPos = default;
            return false;
        }

        void FadeCandyStub(Rope stub)
        {
            // The lower stub keeps AttachedToCandy == true (Split copies it); the
            // terminal-segment cut returns null and just detaches the candy.
            if (stub != null && stub.AttachedToCandy) BeginFade(stub, StubFadeDuration);
        }

        static bool SegmentHit(Vector2 a, Vector2 b, in RopePoint p, in RopePoint q, float alpha, out Vector2 hit)
        {
            return SegmentMath.SegmentsIntersect(a, b, Interpolate(p, alpha), Interpolate(q, alpha), out hit)
                || SegmentMath.SegmentsIntersect(a, b, p.Pos, q.Pos, out hit);
        }

        /// <summary>
        /// Severs the constraint between Points[segmentIndex] and its successor
        /// (the candy itself when segmentIndex is the last point). The rope becomes
        /// the anchor-side stub; the points below the cut become a candy-side stub
        /// that keeps swinging from the candy. Returns the candy-side stub, or null
        /// for a terminal-segment cut (candy just detaches). Stub retract/fade and
        /// free-piece despawn are applied by the caller via <see cref="BeginFade"/>.
        /// </summary>
        Rope Split(Rope rope, int segmentIndex)
        {
            rope.Cuttable = false;
            bool wasAttached = rope.AttachedToCandy;
            rope.AttachedToCandy = false;

            RopePoint[] pts = rope.Points;
            int lowerCount = pts.Length - (segmentIndex + 1);
            if (lowerCount == 0) return null; // terminal segment: candy just detaches

            var upper = new RopePoint[segmentIndex + 1];
            System.Array.Copy(pts, 0, upper, 0, upper.Length);
            var lower = new RopePoint[lowerCount];
            System.Array.Copy(pts, segmentIndex + 1, lower, 0, lowerCount);
            rope.Points = upper;

            var stub = new Rope
            {
                Points = lower,
                RestLength = rope.RestLength,
                AttachedToCandy = wasAttached,
                Cuttable = false,
            };
            Ropes.Add(stub);
            return stub;
        }

        /// <summary>US-003 cut aftermath (DESIGN §2). Marks a rope to fade and despawn
        /// after <paramref name="duration"/> seconds: the rope leaves the swipe test set
        /// but keeps integrating and solving its constraints, so the candy-side stub keeps
        /// swinging (glued to the candy) during the fade — the retract/shrink toward the
        /// candy is a render-time visual owned by RopeRenderer. Core just schedules the
        /// despawn once FadeTime reaches zero.</summary>
        public void BeginFade(Rope rope, float duration)
        {
            rope.Fading = true;
            rope.FadeTime = duration;
            rope.FadeDuration = duration;
            rope.Cuttable = false;
            // AttachedToCandy is intentionally left as-is so the candy-side stub keeps its
            // constraint and swings during the fade; free middle pieces have no candy tie.
        }

        void TickFades(float dt)
        {
            for (int i = Ropes.Count - 1; i >= 0; i--)
            {
                Rope rope = Ropes[i];
                if (!rope.Fading) continue;
                rope.FadeTime -= dt;
                if (rope.FadeTime <= 0f) Ropes.RemoveAt(i);
            }
        }

        /// <summary>A free-floating piece: not cuttable, not attached, not already
        /// fading, and no pinned point. Defensive despawn for the doubly-cut middle
        /// piece; anchor-side stubs (Points[0] pinned) are intentionally kept hanging.</summary>
        static bool IsFreePiece(Rope rope)
            => !rope.Cuttable && !rope.AttachedToCandy && !rope.Fading
               && rope.Points.Length > 0 && rope.Points[0].InvMass > 0f;

        void DetectFreePieces()
        {
            for (int r = 0; r < Ropes.Count; r++)
            {
                Rope rope = Ropes[r];
                if (IsFreePiece(rope)) BeginFade(rope, StubFadeDuration);
            }
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
            float dt2 = dt * dt;
            Vector2 gDt2 = Gravity * dt2;
            for (int r = 0; r < Ropes.Count; r++)
            {
                // Fading ropes keep integrating so they swing naturally during the fade;
                // free middle pieces keep falling (despawn is timed, not velocity-gated).
                RopePoint[] pts = Ropes[r].Points;
                for (int i = 0; i < pts.Length; i++) IntegratePoint(ref pts[i], gDt2, Damping);
            }
            // The candy integrates with its own gravity scale so a bubble can flip it to
            // buoyancy (US-006) without disturbing the rope points hanging from it.
            IntegratePoint(ref Candy, Gravity * (CandyGravityScale * dt2), Damping);
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
