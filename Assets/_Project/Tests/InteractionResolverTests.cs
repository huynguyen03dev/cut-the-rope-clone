using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class InteractionResolverTests
    {
        [Test]
        public void Star_And_Mouth_Same_Step_Collects_Star_Then_Eats()
        {
            var res = InteractionResolver.Resolve(starHit: true, mouthHit: true, leftPlayfield: false, hazardHit: false);
            Assert.IsTrue(res.CollectStars);
            Assert.AreEqual(StepOutcome.Won, res.Outcome);
        }

        [Test]
        public void Win_And_Lose_Same_Step_Wins()
        {
            var res = InteractionResolver.Resolve(starHit: false, mouthHit: true, leftPlayfield: true, hazardHit: false);
            Assert.AreEqual(StepOutcome.Won, res.Outcome);

            res = InteractionResolver.Resolve(starHit: false, mouthHit: true, leftPlayfield: false, hazardHit: true);
            Assert.AreEqual(StepOutcome.Won, res.Outcome);
        }

        [Test]
        public void Leaving_The_Playfield_Loses()
        {
            var res = InteractionResolver.Resolve(starHit: false, mouthHit: false, leftPlayfield: true, hazardHit: false);
            Assert.AreEqual(StepOutcome.Lost, res.Outcome);
        }

        [Test]
        public void Hazard_Hit_Loses()
        {
            var res = InteractionResolver.Resolve(starHit: false, mouthHit: false, leftPlayfield: false, hazardHit: true);
            Assert.AreEqual(StepOutcome.Lost, res.Outcome);
        }

        [Test]
        public void No_Star_Collect_On_A_Losing_Step()
        {
            var res = InteractionResolver.Resolve(starHit: true, mouthHit: false, leftPlayfield: true, hazardHit: false);
            Assert.IsFalse(res.CollectStars, "a destroyed candy cannot collect");
            Assert.AreEqual(StepOutcome.Lost, res.Outcome);
        }

        [Test]
        public void Star_Only_Collects_And_Play_Continues()
        {
            var res = InteractionResolver.Resolve(starHit: true, mouthHit: false, leftPlayfield: false, hazardHit: false);
            Assert.IsTrue(res.CollectStars);
            Assert.AreEqual(StepOutcome.None, res.Outcome);
        }

        [Test]
        public void Nothing_Touched_Resolves_To_Nothing()
        {
            var res = InteractionResolver.Resolve(starHit: false, mouthHit: false, leftPlayfield: false, hazardHit: false);
            Assert.IsFalse(res.CollectStars);
            Assert.AreEqual(StepOutcome.None, res.Outcome);
        }
    }
}
