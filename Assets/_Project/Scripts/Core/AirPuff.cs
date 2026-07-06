using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// US-007 air cushion puff (DESIGN §2 external forces — "air puffs ... adjust the candy
    /// point's implicit velocity: prevPos -= impulse * dt"). A cushion emits a radial puff:
    /// a push that points away from the cushion origin, with magnitude that is full inside an
    /// inner radius and falls off linearly to zero at a max radius. This is the pure,
    /// Unity-free, unit-testable math behind the impulse plus the cooldown gate that stops a
    /// cushion from being spammed. The actual PrevPos mutation is applied by
    /// <see cref="RopeSimulation.ApplyCandyImpulse"/> (zero energy as a one-frame velocity
    /// nudge); the cushion MonoBehaviour owns the cooldown's wall-clock state.
    /// </summary>
    public static class AirPuff
    {
        /// <summary>
        /// Radial puff impulse on a target point from a cushion origin.
        ///
        /// The impulse direction is always <c>origin → target</c> (outward, so a candy placed
        /// above the cushion is pushed up). Magnitude is:
        /// <list type="bullet">
        /// <item><c>magnitude</c> when the target is at or inside <paramref name="innerRadius"/>.</item>
        /// <item>a linear falloff from <c>magnitude</c> at <paramref name="innerRadius"/> to
        /// <c>0</c> at <paramref name="maxRadius"/>.</item>
        /// <item><c>0</c> when the target is at or beyond <paramref name="maxRadius"/>, or when
        /// the target coincides with the origin (radial outward of a point on itself is
        /// undefined — push nothing rather than emit a NaN/zero-direction vector), or when the
        /// magnitude is non-positive.</item>
        /// </list>
        /// When <paramref name="innerRadius"/> equals <paramref name="maxRadius"/> the falloff
        /// collapses to a step (full inside, zero at/over) — clamped so ordering cannot invert
        /// it. The caller passes the candy's current sim position as <paramref name="target"/>
        /// so the impulse is computed against the <em>play-visible</em> candy position, not a
        /// cached transform.
        /// </summary>
        public static Vector2 ComputeImpulse(Vector2 origin, Vector2 target,
                                             float magnitude, float innerRadius, float maxRadius)
        {
            if (magnitude <= 0f) return Vector2.zero;
            if (maxRadius <= 0f) return Vector2.zero;

            Vector2 delta = target - origin;
            float distSq = delta.sqrMagnitude;
            if (distSq < 1e-8f) return Vector2.zero; // target == origin: radial outward undefined

            float dist = Mathf.Sqrt(distSq);
            if (dist >= maxRadius) return Vector2.zero;

            // Clamp so a swapped inner/maxRadius in the inspector cannot invert the band.
            float inner = Mathf.Min(innerRadius, maxRadius);

            float scale;
            if (dist <= inner)
            {
                scale = 1f;
            }
            else
            {
                // Linear falloff: 1 at dist==inner, 0 at dist==maxRadius.
                float span = maxRadius - inner;
                scale = span <= 0f ? 0f : 1f - (dist - inner) / span;
            }

            return delta / dist * (magnitude * scale); // unit outward * scaled magnitude
        }
    }

    /// <summary>
    /// Pure cooldown gate for an air cushion puff (the twitch-prevention barrier between a
    /// tapped cushion and its next puff). Wall-clock seconds are injected by the caller
    /// (<see cref="AirCushion"/>) so this stays Unity-free and unit-testable. A puff is
    /// allowed only when <c>now &gt;= ReadyAt</c>; firing records the time the cushion will
    /// next be ready. <see cref="Remaining"/> / <see cref="Ready01"/> expose the fractional
    /// state for the ready-vs-cooling visual (0 = just fired/cooling, 1 = ready).
    /// </summary>
    public struct PuffCooldown
    {
        /// <summary>Earliest timestamp (seconds, caller's clock) at which the next puff may
        /// fire. Starts at 0 so the very first tap always fires.</summary>
        public float ReadyAt;

        /// <summary>True when the cooldown has elapsed and a puff may fire now.</summary>
        public bool Ready(float now) => now >= ReadyAt;

        /// <summary>Seconds remaining until ready; clamped at 0 (never negative).</summary>
        public float Remaining(float now) => Mathf.Max(0f, ReadyAt - now);

        /// <summary>
        /// Ready progress in 0..1 for the visual: <c>1</c> when ready, dropping toward
        /// <c>0</c> immediately after a puff and rising back to <c>1</c> as the cooldown
        /// elapses. <paramref name="cooldownSeconds"/> must be the same value used to fire.
        /// </summary>
        public float Ready01(float now, float cooldownSeconds)
        {
            if (cooldownSeconds <= 0f) return 1f; // no cooldown → always reads ready
            return Mathf.Clamp01(1f - Remaining(now) / cooldownSeconds);
        }

        /// <summary>
        /// Fires a puff if the cooldown allows it. When allowed, advances
        /// <see cref="ReadyAt"/> to <c>now + cooldownSeconds</c> (the next ready time) and
        /// returns true; otherwise leaves the gate untouched and returns false. A
        /// non-positive cooldown always fires but still records a non-blocking future time so
        /// the contract is uniform. Thread-safe enough for the single-threaded sim update.
        /// </summary>
        public bool TryFire(float now, float cooldownSeconds)
        {
            if (now < ReadyAt) return false;
            ReadyAt = now + Mathf.Max(0f, cooldownSeconds);
            return true;
        }
    }
}