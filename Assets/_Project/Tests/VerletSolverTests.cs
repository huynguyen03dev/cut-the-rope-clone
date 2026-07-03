using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    public class VerletSolverTests
    {
        const float Dt = 1f / 60f;

        static RopeSimulation MakeHangingRope(Vector2 anchor, Vector2 candy, int segments = 12)
        {
            var sim = new RopeSimulation(candy, 0.05f);
            sim.AddRope(anchor, segments);
            return sim;
        }

        [Test]
        public void Constraints_Converge_To_Rest_Length()
        {
            var sim = MakeHangingRope(new Vector2(0f, 0f), new Vector2(0f, -2f));
            for (int i = 0; i < 300; i++) sim.Step(Dt);

            Rope rope = sim.Ropes[0];
            float rest = rope.RestLength;
            for (int i = 0; i < rope.Points.Length - 1; i++)
            {
                float len = Vector2.Distance(rope.Points[i].Pos, rope.Points[i + 1].Pos);
                Assert.That(len, Is.EqualTo(rest).Within(rest * 0.10f), $"segment {i} did not converge");
            }
            // terminal segment carries the heavy candy — allow more residual stretch
            float terminal = Vector2.Distance(rope.Points[rope.Points.Length - 1].Pos, sim.Candy.Pos);
            Assert.That(terminal, Is.EqualTo(rest).Within(rest * 0.25f), "terminal segment did not converge");
        }

        [Test]
        public void Anchor_Never_Moves()
        {
            var anchor = new Vector2(1.5f, 2f);
            var sim = MakeHangingRope(anchor, new Vector2(0f, -1f));
            for (int i = 0; i < 300; i++) sim.Step(Dt);
            Assert.AreEqual(anchor, sim.Ropes[0].Points[0].Pos, "invMass-0 anchor moved");
        }

        [Test]
        public void Swinging_Rope_Settles_Under_Damping()
        {
            // candy released horizontally offset from the anchor — a pendulum
            var sim = MakeHangingRope(new Vector2(0f, 0f), new Vector2(1.5f, -1.5f));
            for (int i = 0; i < 900; i++) sim.Step(Dt); // 15 simulated seconds
            float speed = sim.CandyVelocity(Dt).magnitude;
            Assert.Less(speed, 0.2f, "candy still swinging after 15s — damping not converging");
            // settled candy hangs below the anchor at full rope extension
            Assert.Less(sim.Candy.Pos.y, -1.5f);
            Assert.That(sim.Candy.Pos.x, Is.EqualTo(0f).Within(0.15f), "candy did not settle to plumb");
        }

        [Test]
        public void Step_Snapshots_RenderPos_Before_Integrating()
        {
            var sim = MakeHangingRope(new Vector2(0f, 0f), new Vector2(0f, -2f));
            sim.Step(Dt);
            Vector2 posBefore = sim.Candy.Pos;
            sim.Step(Dt);
            Assert.AreEqual(posBefore, sim.Candy.RenderPos,
                "RenderPos must be the position at the start of the current fixed step");
            Assert.AreNotEqual(sim.Candy.RenderPos, sim.Candy.Pos,
                "candy under gravity should have moved within the step");
        }

        [Test]
        public void Heavy_Candy_Dominates_Light_Rope_Points()
        {
            // pull test: teleport the candy sideways (Pos and PrevPos together, so
            // no implied velocity) to stretch the chain; solving must move the
            // light terminal rope point toward the candy far more than it moves
            // the heavy candy toward the rope.
            var sim = MakeHangingRope(new Vector2(0f, 0f), new Vector2(0f, -2f));
            sim.Gravity = Vector2.zero;
            var shift = new Vector2(0.5f, 0f);
            sim.Candy.Pos += shift;
            sim.Candy.PrevPos += shift;
            Vector2 candyBefore = sim.Candy.Pos;
            int last = sim.Ropes[0].Points.Length - 1;
            Vector2 lastBefore = sim.Ropes[0].Points[last].Pos;
            sim.Step(Dt);
            float candyPullback = (sim.Candy.Pos - candyBefore).magnitude;
            float lastMoved = (sim.Ropes[0].Points[last].Pos - lastBefore).magnitude;
            Assert.Greater(lastMoved, candyPullback,
                "mass weighting broken: the heavy candy moved more than a light rope point");
        }

        [Test]
        public void Impulse_Shifts_Implied_Velocity()
        {
            var sim = new RopeSimulation(new Vector2(0f, 0f), 0.05f);
            sim.Gravity = Vector2.zero; // isolate the impulse
            var impulse = new Vector2(3f, 1f);
            sim.ApplyCandyImpulse(impulse, Dt);
            sim.Step(Dt);
            Vector2 vel = sim.CandyVelocity(Dt);
            Assert.That(vel.x, Is.EqualTo(impulse.x * Dt * sim.Damping / Dt).Within(0.05f));
            Assert.That(vel.y, Is.EqualTo(impulse.y * Dt * sim.Damping / Dt).Within(0.05f));
        }
    }
}
