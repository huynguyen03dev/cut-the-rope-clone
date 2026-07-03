using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Score = stars collected + a time bonus, computed at win (DESIGN §1,
    /// docs/product/progression.md). Pure math so it is unit-testable; best-score
    /// persistence is US-012 (decision 0009).
    /// </summary>
    public static class LevelScoring
    {
        public const int PointsPerStar = 1000;

        /// <summary>Time bonus: floor((par - elapsed) * bonusPerSecond), clamped at zero.
        /// Finishing under par rewards up to par*bonusPerSecond points; over par awards none.</summary>
        public static int TimeBonus(float elapsedSec, float parSec, float bonusPerSecond = 10f)
            => Mathf.Max(0, Mathf.FloorToInt((parSec - elapsedSec) * bonusPerSecond));

        /// <summary>Total score for a completed level run.</summary>
        public static int Compute(int stars, float elapsedSec, float parSec, float bonusPerSecond = 10f)
            => stars * PointsPerStar + TimeBonus(elapsedSec, parSec, bonusPerSecond);
    }

    /// <summary>Outcome payload raised with <see cref="GameEvents.LevelCompleted"/> on the Won transition.</summary>
    public readonly struct LevelResult
    {
        public readonly int Stars;
        public readonly int Score;
        public readonly float ElapsedSec;

        public LevelResult(int stars, int score, float elapsedSec)
        {
            Stars = stars;
            Score = score;
            ElapsedSec = elapsedSec;
        }
    }
}
