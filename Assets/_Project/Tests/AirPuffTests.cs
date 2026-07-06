using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    /// <summary>
    /// US-007 air cushion puff (DESIGN §2 external forces). The cushion query itself is
    /// Unity-physics (proved by integration); this suite pins the pure-core contract that
    /// <see cref="AirPuff.ComputeImpulse"/> produces a radial, linearly-falloff impulse that
    /// is full inside an inner radius, zero at/beyond the max radius, and degenerate-to-zero
    /// when the target coincides with the origin; and that <see cref="PuffCooldown"/> gates
    /// retriggering and exposes a ready progress for the ready-vs-cooling visual.
    /// </summary>
    public class AirPuffTests
    {
        const float Tol = 1e-5f;

        // ---- AirPuff.ComputeImpulse ----

        [Test]
        public void Inside_Inner_Radius_Is_Full_Magnitude_Outward()
        {
            Vector2 origin = Vector2.zero;
            Vector2 target = new Vector2(0f, 1f); // straight up at distance 1

            Vector2 imp = AirPuff.ComputeImpulse(origin, target, magnitude: 5f, innerRadius: 2f, maxRadius: 6f);

            // Full magnitude (5) along the origin->target direction (up).
            Assert.That(imp.x, Is.EqualTo(0f).Within(Tol));
            Assert.That(imp.y, Is.EqualTo(5f).Within(Tol));
        }

        [Test]
        public void At_Max_Radius_Is_Zero()
        {
            Vector2 origin = Vector2.zero;
            Vector2 target = new Vector2(6f, 0f); // exactly at maxRadius

            Vector2 imp = AirPuff.ComputeImpulse(origin, target, 5f, innerRadius: 2f, maxRadius: 6f);

            Assert.AreEqual(Vector2.zero, imp, "at maxRadius the puff must be zero (falloff ends here)");
        }

        [Test]
        public void Beyond_Max_Radius_Is_Zero()
        {
            Vector2 target = new Vector2(7f, 0f);

            Vector2 imp = AirPuff.ComputeImpulse(Vector2.zero, target, 5f, 2f, 6f);

            Assert.AreEqual(Vector2.zero, imp);
        }

        [Test]
        public void Falloff_Is_Linear_Between_Inner_And_Max()
        {
            Vector2 target = new Vector2(4f, 0f); // midway between inner=2 and max=6

            Vector2 imp = AirPuff.ComputeImpulse(Vector2.zero, target, magnitude: 5f, innerRadius: 2f, maxRadius: 6f);

            // Halfway through the band -> half the magnitude, still pointing +x.
            Assert.That(imp.x, Is.EqualTo(2.5f).Within(Tol));
            Assert.That(imp.y, Is.EqualTo(0f).Within(Tol));
        }

        [Test]
        public void Direction_Is_Radial_Outward_For_Off_Axis_Target()
        {
            Vector2 target = new Vector2(3f, 4f); // distance 5, inside inner=2? no -> falloff; direction (0.6,0.8)

            Vector2 imp = AirPuff.ComputeImpulse(Vector2.zero, target, magnitude: 10f, innerRadius: 2f, maxRadius: 6f);

            // At distance 5, band is [2,6] -> scale = 1 - (5-2)/(6-2) = 1 - 0.75 = 0.25 -> magnitude 2.5.
            // Unit outward = (3,4)/5 = (0.6, 0.8). Impulse = (1.5, 2.0).
            Assert.That(imp.x, Is.EqualTo(1.5f).Within(Tol));
            Assert.That(imp.y, Is.EqualTo(2.0f).Within(Tol));
        }

        [Test]
        public void Target_At_Origin_Is_Zero_Not_NaN()
        {
            Vector2 imp = AirPuff.ComputeImpulse(Vector2.zero, Vector2.zero, 5f, 2f, 6f);

            Assert.AreEqual(Vector2.zero, imp, "radial outward of a point on itself is undefined -> push nothing");
            Assert.IsFalse(float.IsNaN(imp.x) || float.IsNaN(imp.y));
        }

        [Test]
        public void NonPositive_Magnitude_Is_Zero()
        {
            Vector2 target = new Vector2(1f, 0f);

            Assert.AreEqual(Vector2.zero, AirPuff.ComputeImpulse(Vector2.zero, target, 0f, 2f, 6f));
            Assert.AreEqual(Vector2.zero, AirPuff.ComputeImpulse(Vector2.zero, target, -3f, 2f, 6f));
        }

        [Test]
        public void Zero_Max_Radius_Is_Zero()
        {
            Vector2 target = new Vector2(1f, 0f);

            Assert.AreEqual(Vector2.zero, AirPuff.ComputeImpulse(Vector2.zero, target, 5f, 0f, 0f));
        }

        [Test]
        public void Swapped_Inner_Greater_Than_Max_Is_Clamped_To_Step()
        {
            // Inspector typo: innerRadius > maxRadius must NOT invert into a negative scale.
            Vector2 targetInner = new Vector2(1f, 0f); // inside clamped inner (=max=6) -> full
            Vector2 targetMid = new Vector2(4f, 0f);   // inside clamped inner -> full as well
            Vector2 targetOver = new Vector2(7f, 0f);  // over max -> zero

            Assert.AreEqual(new Vector2(5f, 0f),
                AirPuff.ComputeImpulse(Vector2.zero, targetInner, 5f, innerRadius: 9f, maxRadius: 6f));
            Assert.AreEqual(new Vector2(5f, 0f),
                AirPuff.ComputeImpulse(Vector2.zero, targetMid, 5f, innerRadius: 9f, maxRadius: 6f));
            Assert.AreEqual(Vector2.zero,
                AirPuff.ComputeImpulse(Vector2.zero, targetOver, 5f, innerRadius: 9f, maxRadius: 6f));
        }

        [Test]
        public void Inner_Equal_Max_Is_A_Step_Falloff()
        {
            Vector2 justInside = new Vector2(5.999f, 0f);
            Vector2 atMax = new Vector2(6f, 0f);

            Assert.AreEqual(new Vector2(5f, 0f),
                AirPuff.ComputeImpulse(Vector2.zero, justInside, 5f, innerRadius: 6f, maxRadius: 6f));
            Assert.AreEqual(Vector2.zero,
                AirPuff.ComputeImpulse(Vector2.zero, atMax, 5f, innerRadius: 6f, maxRadius: 6f));
        }

        [Test]
        public void Impulse_Is_Independent_Of_Origin_Offset()
        {
            Vector2 origin = new Vector2(10f, -3f);
            Vector2 target = origin + new Vector2(0f, 1f); // 1 above the cushion

            Vector2 imp = AirPuff.ComputeImpulse(origin, target, 5f, 2f, 6f);

            Assert.That(imp.x, Is.EqualTo(0f).Within(Tol));
            Assert.That(imp.y, Is.EqualTo(5f).Within(Tol));
        }

        // ---- PuffCooldown ----

        [Test]
        public void First_Fire_Always_Allowed_And_Sets_ReadyAt()
        {
            var cd = new PuffCooldown();

            Assert.IsTrue(cd.TryFire(now: 3f, cooldownSeconds: 2f), "the very first tap always fires");
            Assert.That(cd.ReadyAt, Is.EqualTo(5f).Within(Tol));
        }

        [Test]
        public void Fire_Blocked_Within_Cooldown_Window()
        {
            var cd = new PuffCooldown();
            cd.TryFire(3f, 2f); // ready at 5

            Assert.IsFalse(cd.TryFire(3f, 2f), "same instant blocked");
            Assert.IsFalse(cd.TryFire(4.999f, 2f), "still cooling");
            Assert.That(cd.ReadyAt, Is.EqualTo(5f).Within(Tol), "a blocked fire must not advance ReadyAt");
        }

        [Test]
        public void Fire_Allowed_When_Cooldown_Elapses()
        {
            var cd = new PuffCooldown();
            cd.TryFire(3f, 2f); // ready at 5

            Assert.IsTrue(cd.TryFire(5f, 2f), "at exactly ReadyAt the gate opens");
            Assert.That(cd.ReadyAt, Is.EqualTo(7f).Within(Tol), "re-fire advances to now + cooldown");
        }

        [Test]
        public void Ready_And_Remaining_Track_The_Gate()
        {
            var cd = new PuffCooldown();
            cd.TryFire(3f, 2f); // ready at 5

            Assert.IsFalse(cd.Ready(4f));
            Assert.That(cd.Remaining(4f), Is.EqualTo(1f).Within(Tol));

            Assert.IsTrue(cd.Ready(5f));
            Assert.That(cd.Remaining(5f), Is.EqualTo(0f).Within(Tol));
            Assert.That(cd.Remaining(6f), Is.EqualTo(0f).Within(Tol), "remaining never goes negative");
        }

        [Test]
        public void Ready01_Rises_From_Zero_To_One_During_Cooldown()
        {
            var cd = new PuffCooldown();
            cd.TryFire(3f, 2f); // ready at 5, started at 3

            Assert.That(cd.Ready01(3f, 2f), Is.EqualTo(0f).Within(Tol), "just fired -> 0");
            Assert.That(cd.Ready01(4f, 2f), Is.EqualTo(0.5f).Within(Tol), "halfway -> 0.5");
            Assert.That(cd.Ready01(5f, 2f), Is.EqualTo(1f).Within(Tol), "ready -> 1");
            Assert.That(cd.Ready01(9f, 2f), Is.EqualTo(1f), "stays clamped at 1 past ready");
        }

        [Test]
        public void Zero_Cooldown_Always_Fires_And_Reads_Ready()
        {
            var cd = new PuffCooldown();

            Assert.IsTrue(cd.TryFire(1f, 0f));
            Assert.IsTrue(cd.TryFire(1f, 0f), "no cooldown -> no blocking");
            Assert.That(cd.Ready01(1f, 0f), Is.EqualTo(1f), "no cooldown -> visual always reads ready");
        }
    }
}
