using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    /// <summary>
    /// US-006 tap-vs-swipe classification (DESIGN §3, gameplay.md input rule #1). Pins the
    /// pure predicate that guarantees one touch is EITHER a swipe (cutter) OR a tap (bubble
    /// pop), never both: a pointer commits to a swipe once it travels past the distance
    /// threshold, and a release still under the threshold is a tap only if it was brief.
    /// </summary>
    public class PointerGestureTests
    {
        const float Dist = 24f;
        const float Dur = 0.3f;

        [Test]
        public void StaysCandidate_Within_Distance_Threshold()
        {
            Assert.IsFalse(PointerGesture.HasCommittedToSwipe(Vector2.zero, new Vector2(5f, 3f), Dist),
                "small travel stays a tap candidate");
        }

        [Test]
        public void Commits_To_Swipe_Past_Distance_Threshold()
        {
            Assert.IsTrue(PointerGesture.HasCommittedToSwipe(Vector2.zero, new Vector2(25f, 0f), Dist),
                "travel past the threshold commits to a swipe");
        }

        [Test]
        public void Does_Not_Commit_Exactly_At_Threshold()
        {
            Assert.IsFalse(PointerGesture.HasCommittedToSwipe(Vector2.zero, new Vector2(Dist, 0f), Dist),
                "the threshold is strict (> not >=), so exactly at it is still a candidate");
        }

        [Test]
        public void Quick_Local_Release_Is_Tap()
        {
            Assert.IsTrue(PointerGesture.IsTap(Vector2.zero, new Vector2(5f, 0f), 0.1f, Dist, Dur),
                "a brief release within the distance threshold is a tap");
        }

        [Test]
        public void Far_Release_Is_Not_Tap()
        {
            Assert.IsFalse(PointerGesture.IsTap(Vector2.zero, new Vector2(40f, 0f), 0.1f, Dist, Dur),
                "a release beyond the distance threshold is not a tap");
        }

        [Test]
        public void Long_Hold_Is_Not_Tap()
        {
            Assert.IsFalse(PointerGesture.IsTap(Vector2.zero, new Vector2(2f, 0f), 0.5f, Dist, Dur),
                "held past the time threshold, a local touch is neither a cut nor a tap");
        }
    }
}
