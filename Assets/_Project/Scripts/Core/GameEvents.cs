using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Plain C# event channels (DESIGN §3): gameplay raises, UI/audio/save subscribe.
    /// Instance-owned and rebuilt per level — never static — so subscribers on destroyed
    /// level objects cannot leak or throw across restarts (US-003 lifecycle rule).
    /// </summary>
    public sealed class GameEvents
    {
        /// <summary>Raised at the cut position (audio/VFX subscribe in US-014/US-015).</summary>
        public event Action<Vector2> RopeCut;

        public event Action<GameObject> StarCollected;
        public event Action CandyEaten;
        public event Action CandyLost;

        /// <summary>Raised on the Won transition with the run's stars + score payload
        /// (US-013's next-level flow consumes it; US-012 persists best score).</summary>
        public event Action<LevelResult> LevelCompleted;

        public void RaiseRopeCut(Vector2 at) => RopeCut?.Invoke(at);
        public void RaiseStarCollected(GameObject star) => StarCollected?.Invoke(star);
        public void RaiseCandyEaten() => CandyEaten?.Invoke();
        public void RaiseCandyLost() => CandyLost?.Invoke();
        public void RaiseLevelCompleted(LevelResult result) => LevelCompleted?.Invoke(result);

        /// <summary>US-003 lifecycle rule: rebuild per level. Clears every subscriber so
        /// handlers bound to the destroyed level instance drop off cleanly, then the
        /// session re-subscribes the objects that survive the restart.</summary>
        public void Rebuild()
        {
            RopeCut = null;
            StarCollected = null;
            CandyEaten = null;
            CandyLost = null;
            LevelCompleted = null;
        }
    }
}
