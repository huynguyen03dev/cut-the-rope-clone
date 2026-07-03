using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    public class RopeCutTests
    {
        const float Dt = 1f / 60f;

        // Anchor at origin, candy 2 units below, 12 segments: point i sits at
        // y = -i/6, rest length 1/6. Geometry is exact before any Step.
        static RopeSimulation MakeVerticalRope(out Rope rope)
        {
            var sim = new RopeSimulation(new Vector2(0f, -2f), 0.05f);
            rope = sim.AddRope(new Vector2(0f, 0f), 12);
            return sim;
        }

        [Test]
        public void Cut_Splits_Rope_Into_Anchor_Stub_And_Candy_Stub()
        {
            var sim = MakeVerticalRope(out Rope original);
            bool cut = sim.TryCut(new Vector2(-1f, -1.02f), new Vector2(1f, -1.02f), 0f, out Vector2 cutPos);

            Assert.IsTrue(cut);
            Assert.That(cutPos.y, Is.EqualTo(-1.02f).Within(1e-4f));
            Assert.AreEqual(2, sim.Ropes.Count);

            Rope anchorStub = sim.Ropes[0];
            Rope candyStub = sim.Ropes[1];
            Assert.AreSame(original, anchorStub);
            Assert.IsFalse(anchorStub.AttachedToCandy, "anchor stub must detach from the candy");
            Assert.IsTrue(candyStub.AttachedToCandy, "candy stub must keep hanging from the candy");
            Assert.AreEqual(0f, anchorStub.Points[0].InvMass, "anchor stub keeps its pin");
            // swipe at y=-1.02 crosses the segment between point 6 (y=-1.0) and point 7
            Assert.AreEqual(7, anchorStub.Points.Length);
            Assert.AreEqual(5, candyStub.Points.Length);
        }

        [Test]
        public void Stubs_Leave_The_Swipe_Test_Set_Immediately()
        {
            var sim = MakeVerticalRope(out _);
            Assert.IsTrue(sim.TryCut(new Vector2(-1f, -1.02f), new Vector2(1f, -1.02f), 0f, out _));
            Assert.IsFalse(sim.Ropes[0].Cuttable);
            Assert.IsFalse(sim.Ropes[1].Cuttable);
            // the same swipe again crosses both stubs geometrically — must hit nothing
            Assert.IsFalse(sim.TryCut(new Vector2(-1f, -1.02f), new Vector2(1f, -1.02f), 0f, out _));
        }

        [Test]
        public void Both_Halves_Swing_Correctly_After_The_Cut()
        {
            var sim = MakeVerticalRope(out _);
            sim.TryCut(new Vector2(-1f, -1.02f), new Vector2(1f, -1.02f), 0f, out _);
            for (int i = 0; i < 300; i++) sim.Step(Dt);

            Rope anchorStub = sim.Ropes[0];
            Rope candyStub = sim.Ropes[1];
            // anchor stub still hangs from its pin
            Assert.AreEqual(new Vector2(0f, 0f), anchorStub.Points[0].Pos);
            // candy stub stays glued to the (free-falling) candy
            float tie = Vector2.Distance(candyStub.Points[candyStub.Points.Length - 1].Pos, sim.Candy.Pos);
            Assert.That(tie, Is.EqualTo(candyStub.RestLength).Within(candyStub.RestLength),
                "candy stub flailed off the candy");
            // with every rope cut, the candy free-falls
            Assert.Less(sim.Candy.Pos.y, -3f, "candy did not fall after its only rope was cut");
        }

        [Test]
        public void Cutting_The_Terminal_Segment_Just_Detaches_The_Candy()
        {
            var sim = MakeVerticalRope(out Rope rope);
            // terminal segment spans point 11 (y=-11/6) to the candy (y=-2)
            bool cut = sim.TryCut(new Vector2(-1f, -1.95f), new Vector2(1f, -1.95f), 0f, out _);
            Assert.IsTrue(cut);
            Assert.AreEqual(1, sim.Ropes.Count, "terminal cut must not create an empty stub");
            Assert.IsFalse(rope.AttachedToCandy);
            Assert.IsFalse(rope.Cuttable);
        }

        [Test]
        public void Swipe_Missing_Every_Rope_Cuts_Nothing()
        {
            var sim = MakeVerticalRope(out _);
            Assert.IsFalse(sim.TryCut(new Vector2(1f, -1f), new Vector2(2f, -1f), 0f, out _));
            Assert.AreEqual(1, sim.Ropes.Count);
            Assert.IsTrue(sim.Ropes[0].Cuttable);
        }

        [Test]
        public void Cut_Only_Severs_The_Crossed_Rope_When_Two_Ropes_Share_The_Candy()
        {
            var sim = new RopeSimulation(new Vector2(0f, -2f), 0.05f);
            sim.AddRope(new Vector2(-1f, 0f), 12);
            Rope right = sim.AddRope(new Vector2(1f, 0f), 12);
            // vertical swipe crossing only the right rope's upper half
            bool cut = sim.TryCut(new Vector2(0.8f, -0.2f), new Vector2(0.8f, -0.7f), 0f, out _);
            Assert.IsTrue(cut);
            Assert.AreEqual(3, sim.Ropes.Count);
            Assert.IsTrue(sim.Ropes[0].Cuttable, "left rope must stay intact and cuttable");
            Assert.IsTrue(sim.Ropes[0].AttachedToCandy);
            Assert.IsFalse(right.Cuttable);
        }
    }
}
