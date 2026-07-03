namespace Game.Core
{
    public enum StepOutcome
    {
        None,
        Won,
        Lost,
    }

    public readonly struct StepResolution
    {
        /// <summary>Stars overlapped this step are collected — before the eat on
        /// a winning step, never on a losing step (the candy is destroyed).</summary>
        public readonly bool CollectStars;

        public readonly StepOutcome Outcome;

        public StepResolution(bool collectStars, StepOutcome outcome)
        {
            CollectStars = collectStars;
            Outcome = outcome;
        }
    }

    /// <summary>
    /// Same-frame priority rules (gameplay.md, decided once, encoded here):
    /// win beats lose; stars collect before the eat.
    /// </summary>
    public static class InteractionResolver
    {
        public static StepResolution Resolve(bool starHit, bool mouthHit, bool leftPlayfield, bool hazardHit)
        {
            if (mouthHit) return new StepResolution(starHit, StepOutcome.Won);
            if (leftPlayfield || hazardHit) return new StepResolution(false, StepOutcome.Lost);
            return new StepResolution(starHit, StepOutcome.None);
        }
    }
}
