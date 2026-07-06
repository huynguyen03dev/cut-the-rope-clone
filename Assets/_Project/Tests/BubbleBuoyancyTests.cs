using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    /// <summary>
    /// US-006 bubble buoyancy (DESIGN §2 per-point gravity flip). The bubble query itself is
    /// Unity-physics (proved by integration); this suite pins the pure-core contract that
    /// <see cref="RopeSimulation.CandyGravityScale"/> flips only the candy's gravity, mirrors
    /// gravity when fully inverted, and — like the auto-grab attach — injects zero energy when
    /// set or restored (the candy's implied velocity is unchanged at the attach/pop frame).
    /// </summary>
    public class BubbleBuoyancyTests
    {
        const float Dt = 0.02f;

        [Test]
        public void Default_Candy_Gravity_Scale_Is_One()
        {
            Assert.AreEqual(1f, NewSim().CandyGravityScale, "candy starts at normal weight");
        }

        [Test]
        public void Default_Candy_Falls_Under_Gravity()
        {
            var sim = NewSim();
            sim.Step(Dt);
            Assert.Less(sim.Candy.Pos.y, 0f, "with scale 1 the candy falls (gravity is -y)");
        }

        [Test]
        public void Inverted_Scale_Makes_Candy_Rise()
        {
            var sim = NewSim();
            sim.CandyGravityScale = -1f;
            sim.Step(Dt);
            Assert.Greater(sim.Candy.Pos.y, 0f, "a bubble (negative scale) floats the candy up");
        }

        [Test]
        public void Full_Inversion_Mirrors_Gravity()
        {
            var down = NewSim();                       // scale 1
            var up = NewSim();
            up.CandyGravityScale = -1f;                // scale -1

            down.Step(Dt);
            up.Step(Dt);

            // From rest the first step is pure gDt2 (damping acts on zero velocity), so a full
            // inversion produces an exactly opposite candy displacement.
            Assert.That(up.Candy.Pos.y, Is.EqualTo(-down.Candy.Pos.y).Within(1e-6f),
                "scale -1 must mirror the displacement of scale 1");
        }

        [Test]
        public void Setting_Scale_Injects_Zero_Candy_Velocity()
        {
            var sim = NewSim();
            sim.Candy.PrevPos = sim.Candy.Pos - new Vector2(0.5f, 0f); // moving +x (swinging in)
            Vector2 velBefore = sim.CandyVelocity(Dt);

            sim.CandyGravityScale = -0.6f; // attach a bubble

            // Flipping the scale must not touch Pos/PrevPos — no yank on grab (zero-energy rule).
            Assert.AreEqual(velBefore, sim.CandyVelocity(Dt),
                "attaching a bubble must not change the candy's implied velocity");
        }

        [Test]
        public void Popping_Restores_Gravity_With_Zero_Energy()
        {
            var sim = NewSim();
            sim.CandyGravityScale = -1f;
            for (int i = 0; i < 5; i++) sim.Step(Dt); // let the candy build upward velocity

            Vector2 velAtPop = sim.CandyVelocity(Dt);
            sim.CandyGravityScale = 1f; // pop
            Assert.AreEqual(velAtPop, sim.CandyVelocity(Dt),
                "popping must not change the candy's implied velocity (zero-energy)");

            float upVel = sim.CandyVelocity(Dt).y;
            sim.Step(Dt);
            Assert.Less(sim.CandyVelocity(Dt).y, upVel,
                "with gravity restored the candy's upward velocity must start decreasing again");
        }

        // No ropes: the candy integrates freely so the tests isolate gravity from constraints.
        static RopeSimulation NewSim() => new RopeSimulation(Vector2.zero, 0.05f);
    }
}
