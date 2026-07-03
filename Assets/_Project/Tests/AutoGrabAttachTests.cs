using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    /// <summary>
    /// US-005 auto-grab attach math (DESIGN §2 zero-energy rule). The grab query
    /// itself is Unity-physics (proved by integration); this suite pins the pure-core
    /// contract that <see cref="RopeSimulation.AddRope"/> attaches a rope with zero
    /// energy: rest length from the anchor→candy distance, every spawned point at rest,
    /// and the candy's implied velocity unchanged across the attach.
    /// </summary>
    public class AutoGrabAttachTests
    {
        [Test]
        public void RestLength_Is_AnchorToCandy_Distance_Over_Segments()
        {
            var sim = NewSim();
            Vector2 anchor = new Vector2(1f, 3f);
            const int segments = 8;

            Rope rope = sim.AddRope(anchor, segments);

            float expected = Vector2.Distance(anchor, sim.Candy.Pos) / segments;
            Assert.That(rope.RestLength, Is.EqualTo(expected).Within(1e-5f),
                "rest length must be the anchor→candy distance / segments (not the trigger radius)");
        }

        [Test]
        public void Every_Spawned_Point_Starts_At_Rest()
        {
            var sim = NewSim();
            Rope rope = sim.AddRope(new Vector2(0f, 4f), 10);

            // PrevPos == Pos for every point (incl. the pinned anchor) means the new rope
            // contributes zero implicit velocity on its first step — no yank on grab.
            for (int i = 0; i < rope.Points.Length; i++)
            {
                Assert.AreEqual(rope.Points[i].Pos, rope.Points[i].PrevPos,
                    $"point {i} must start with PrevPos == Pos");
            }
            Assert.AreEqual(0f, rope.Points[0].InvMass, "anchor point must be pinned (invMass 0)");
            for (int i = 1; i < rope.Points.Length; i++)
                Assert.AreEqual(1f, rope.Points[i].InvMass, $"interior point {i} mass");
        }

        [Test]
        public void New_Rope_Is_Cuttable_And_Attached_To_Candy()
        {
            var sim = NewSim();

            Rope rope = sim.AddRope(new Vector2(0f, 2f), 6);

            Assert.IsTrue(rope.AttachedToCandy, "a grabbed rope must share the candy terminal point");
            Assert.IsTrue(rope.Cuttable, "a grabbed rope must be cuttable like an authored rope");
            Assert.Contains(rope, sim.Ropes, "grabbed rope is registered in the sim");
        }

        [Test]
        public void Attach_Injects_Zero_Candy_Velocity()
        {
            var sim = NewSim();
            // Give the candy an existing velocity (simulate a swing into the zone).
            const float dt = 0.02f;
            sim.Candy.PrevPos = sim.Candy.Pos - new Vector2(0.5f, 0f); // moving +x
            Vector2 velBefore = sim.CandyVelocity(dt);

            sim.AddRope(new Vector2(0f, 3f), 12);

            Vector2 velAfter = sim.CandyVelocity(dt);
            // AddRope must not mutate candy Pos/PrevPos — the attach frame changes nothing
            // about the candy's motion. (Points at rest absorb the constraint on the NEXT step.)
            Assert.AreEqual(velBefore, velAfter,
                "attach must not change the candy's implied velocity (zero-energy rule)");
        }

        [Test]
        public void Multiple_Ropes_Share_The_Candy_Terminal_Point()
        {
            var sim = NewSim();
            sim.AddRope(new Vector2(-1f, 3f), 8);
            sim.AddRope(new Vector2(1f, 3f), 8); // a second grabbed rope

            Assert.AreEqual(2, sim.Ropes.Count, "two ropes attached");
            foreach (Rope rope in sim.Ropes)
                Assert.IsTrue(rope.AttachedToCandy, "every rope shares the candy");
        }

        static RopeSimulation NewSim() => new RopeSimulation(new Vector2(0f, -1f), 0.05f);
    }
}
