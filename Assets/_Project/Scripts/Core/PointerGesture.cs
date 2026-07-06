using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Pure tap-vs-swipe classification (DESIGN §3, gameplay.md input rule #1). A single
    /// touch is EITHER a tap (which pops a bubble / US-006) OR a swipe (which cuts ropes /
    /// US-001) — never both. A pointer commits to "swipe" the instant it travels past the
    /// distance threshold, and from then on can only cut; a pointer released without ever
    /// committing counts as a tap only if it stayed brief and local. Thresholds are supplied
    /// by the caller in a single consistent space (screen pixels), so this stays a pure,
    /// unit-testable predicate with no Unity input or time dependency.
    /// </summary>
    public static class PointerGesture
    {
        /// <summary>Has the pointer moved far enough from its press origin to commit to a
        /// swipe (become a cutter)? Once true the caller MUST latch it: the touch is now a
        /// cutter for the rest of its life and can no longer pop a bubble. Uses squared
        /// distance to avoid the sqrt.</summary>
        public static bool HasCommittedToSwipe(Vector2 start, Vector2 current, float distanceThreshold)
            => (current - start).sqrMagnitude > distanceThreshold * distanceThreshold;

        /// <summary>On release: a pointer that never committed to a swipe is a tap when it
        /// was released within the distance threshold of its origin AND within the time
        /// threshold. (A slow drift that never crossed the distance threshold but was held a
        /// long time is neither a cut nor a tap — it resolves to nothing.)</summary>
        public static bool IsTap(Vector2 start, Vector2 release, float duration,
                                 float distanceThreshold, float durationThreshold)
            => (release - start).sqrMagnitude <= distanceThreshold * distanceThreshold
               && duration >= 0f && duration <= durationThreshold;
    }
}
