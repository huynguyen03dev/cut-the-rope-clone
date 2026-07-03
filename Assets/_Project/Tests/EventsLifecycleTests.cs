using System;
using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    public class GameEventsLifecycleTests
    {
        [Test]
        public void Raise_Dispatches_To_Current_Subscribers()
        {
            var events = new GameEvents();
            int ropeCuts = 0, stars = 0, eaten = 0, lost = 0, completed = 0;
            events.RopeCut += _ => ropeCuts++;
            events.StarCollected += _ => stars++;
            events.CandyEaten += () => eaten++;
            events.CandyLost += () => lost++;
            events.LevelCompleted += _ => completed++;

            events.RaiseRopeCut(Vector2.zero);
            events.RaiseStarCollected(null);
            events.RaiseCandyEaten();
            events.RaiseCandyLost();
            events.RaiseLevelCompleted(new LevelResult(2, 2000, 5f));

            Assert.AreEqual(1, ropeCuts);
            Assert.AreEqual(1, stars);
            Assert.AreEqual(1, eaten);
            Assert.AreEqual(1, lost);
            Assert.AreEqual(1, completed);
        }

        [Test]
        public void Two_Consecutive_Restarts_Leak_No_Subscribers_And_Throw_Nothing()
        {
            // US-003 acceptance: events rebuilt per level — two restarts leak no subscribers
            // and never throw against destroyed level objects. We simulate a level's handlers
            // by holding them in a weak list; rebuild must clear them so the next level does
            // not invoke stale handlers.
            var events = new GameEvents();
            int firstLevelCalls = 0;
            events.CandyEaten += () => firstLevelCalls++;

            // Restart #1: rebuild, re-subscribe a fresh "level instance".
            events.Rebuild();
            int secondLevelCalls = 0;
            events.CandyEaten += () => secondLevelCalls++;

            events.RaiseCandyEaten();
            Assert.AreEqual(0, firstLevelCalls, "stale level-1 handler fired after rebuild (leak)");
            Assert.AreEqual(1, secondLevelCalls);

            // Restart #2: same drill. The level-2 handler must NOT fire again after this
            // raise — it stays at its prior value of 1 (not 2), proving rebuild cleared it.
            events.Rebuild();
            int thirdLevelCalls = 0;
            events.CandyEaten += () => thirdLevelCalls++;

            events.RaiseCandyEaten();
            Assert.AreEqual(0, firstLevelCalls);
            Assert.AreEqual(1, secondLevelCalls, "stale level-2 handler fired again after rebuild (leak)");
            Assert.AreEqual(1, thirdLevelCalls);
            Assert.DoesNotThrow(() => events.RaiseCandyLost());
        }

        [Test]
        public void Unraised_Events_Do_Not_Invoke_Null_Delegates()
        {
            // Rebuild leaves every delegate null; raising must not NPE.
            var events = new GameEvents();
            events.Rebuild();
            Assert.DoesNotThrow(() =>
            {
                events.RaiseRopeCut(Vector2.zero);
                events.RaiseStarCollected(null);
                events.RaiseCandyEaten();
                events.RaiseCandyLost();
                events.RaiseLevelCompleted(new LevelResult(0, 0, 0f));
            });
        }
    }
}