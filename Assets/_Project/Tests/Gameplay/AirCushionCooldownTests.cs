using System.Reflection;
using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Gameplay.Tests
{
    /// <summary>
    /// Regression guard for the US-007 integration bug. <see cref="AirCushion.Cooldown"/> is a
    /// <see cref="PuffCooldown"/> (struct) stored in an auto-property, so a method that called
    /// <c>Cooldown.TryFire(...)</c> would mutate a throwaway getter copy and <c>ReadyAt</c> would
    /// never persist — every tap would fire and the cooldown never blocked. <see cref="AirCushion"/>
    /// was caught live (EditMode tests use a local <c>var cd = new PuffCooldown()</c>, so they stayed
    /// green through the regression). <see cref="AirCushion.BeginPuff"/> now does a local-mutate
    /// → assign-back; this suite exercises the REAL MonoBehaviour method and asserts the cooldown
    /// persists and blocks a same-instant re-tap. A future refactor that reverts BeginPuff to the
    /// buggy <c>Cooldown.TryFire(...)</c> form makes <see cref="Retap_Blocked_ReadyAt_Persists"/>
    /// fail (the second BeginPuff returns true and ReadyAt stays 0).
    ///
    /// The AirCushion GameObject is kept INACTIVE so Awake (which builds the LineRenderer ring +
    /// starts PrimeTween tweens via Shader.Find) never runs — the cooldown contract is independent
    /// of the visual, and this keeps the EditMode test fast, allocation-light, and free of
    /// editor-mode tween/visual noise. <c>cooldownSeconds</c> is set via reflection to a known
    /// value for a deterministic <c>ReadyAt</c> assertion (the [Range] field default already
    /// initializes on construction, but pinning it makes intent explicit).
    /// </summary>
    public class AirCushionCooldownTests
    {
        const float Cooldown = 0.5f;

        [Test]
        public void First_Tap_Fires_And_Advances_ReadyAt()
        {
            var (go, cushion) = NewInactiveCushion();
            try
            {
                bool fired = cushion.BeginPuff(now: 10f);
                Assert.IsTrue(fired, "the very first tap always fires");
                Assert.That(cushion.Cooldown.ReadyAt, Is.EqualTo(10.5f).Within(1e-6f),
                    "ReadyAt must persist to now + cooldown (the auto-property getter-copy bug leaves it 0)");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Retap_Blocked_ReadyAt_Persists()
        {
            var (go, cushion) = NewInactiveCushion();
            try
            {
                Assert.IsTrue(cushion.BeginPuff(10f), "first fire");

                bool firedSameInstant = cushion.BeginPuff(now: 10f);
                Assert.IsFalse(firedSameInstant, "a same-instant re-tap must be blocked");
                Assert.That(cushion.Cooldown.ReadyAt, Is.EqualTo(10.5f).Within(1e-6f),
                    "a blocked fire must not advance ReadyAt");

                Assert.IsFalse(cushion.BeginPuff(10.49f), "still cooling just before ReadyAt");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Refire_Allowed_At_ReadyAt_Exactly()
        {
            var (go, cushion) = NewInactiveCushion();
            try
            {
                Assert.IsTrue(cushion.BeginPuff(10f), "first fire");
                Assert.IsTrue(cushion.BeginPuff(10.5f), "at exactly ReadyAt the gate opens");
                Assert.That(cushion.Cooldown.ReadyAt, Is.EqualTo(11f).Within(1e-6f),
                    "re-fire advances ReadyAt to now + cooldown");
            }
            finally { Object.DestroyImmediate(go); }
        }

        // Create an AirCushion on an INACTIVE GameObject so Awake (visual ring + PrimeTween) is
        // deferred. Field initializers still run, so cooldownSeconds has its [Range] default; we
        // pin it via reflection for a deterministic ReadyAt.
        static (GameObject go, AirCushion cushion) NewInactiveCushion()
        {
            var go = new GameObject("AirCushionUT");
            go.SetActive(false); // Awake deferred: no LineRenderer / PrimeTween / Shader.Find
            var cushion = go.AddComponent<AirCushion>(); // RequireComponent auto-adds CircleCollider2D
            SetCooldownSeconds(cushion, Cooldown);
            return (go, cushion);
        }

        static void SetCooldownSeconds(AirCushion cushion, float value)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo f = typeof(AirCushion).GetField("cooldownSeconds", flags);
            Assert.IsNotNull(f, "AirCushion must have a cooldownSeconds field");
            f.SetValue(cushion, value);
        }
    }
}