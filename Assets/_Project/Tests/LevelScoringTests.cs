using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class LevelScoringTests
    {
        [Test]
        public void Score_Is_Stars_Times_PointsPerStar_Plus_TimeBonus()
        {
            // 3 stars, finished 2s under par(10s), 10 bonus/sec → 3*1000 + 20 = 3020
            int score = LevelScoring.Compute(stars: 3, elapsedSec: 8f, parSec: 10f, bonusPerSecond: 10f);
            Assert.AreEqual(3020, score);
        }

        [Test]
        public void TimeBonus_Under_Par_Is_Floored_Positive_Bonus()
        {
            // (10 - 8.4) * 10 = 16 → floor 16
            Assert.AreEqual(16, LevelScoring.TimeBonus(8.4f, 10f, 10f));
        }

        [Test]
        public void TimeBonus_At_Par_Is_Zero()
        {
            Assert.AreEqual(0, LevelScoring.TimeBonus(10f, 10f, 10f));
        }

        [Test]
        public void TimeBonus_Over_Par_Clamps_To_Zero_Never_Negative()
        {
            Assert.AreEqual(0, LevelScoring.TimeBonus(20f, 10f, 10f));
            Assert.AreEqual(0, LevelScoring.TimeBonus(10.001f, 10f, 10f));
        }

        [Test]
        public void Zero_Stars_Award_No_Star_Portions_Only_TimeBonus()
        {
            // 0 stars + over-par → 0
            Assert.AreEqual(0, LevelScoring.Compute(0, 12f, 10f, 10f));
            // 0 stars + under-par → only the time bonus
            Assert.AreEqual(50, LevelScoring.Compute(0, 5f, 10f, 10f));
        }

        [Test]
        public void LevelResult_Payload_Round_Trips_Stars_Score_Elapsed()
        {
            var r = new LevelResult(2, 1250, 7.5f);
            Assert.AreEqual(2, r.Stars);
            Assert.AreEqual(1250, r.Score);
            Assert.AreEqual(7.5f, r.ElapsedSec);
        }
    }
}