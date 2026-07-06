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

        /// <summary>Raised at the grab anchor position when an auto-grab zone attaches a
        /// new rope to the candy (US-005). Audio/juice subscribe in US-014/US-015.</summary>
        public event Action<Vector2> RopeAttached;

        /// <summary>Raised at the candy position when a bubble envelops it and flips it to
        /// buoyancy (US-006). Audio/juice subscribe in US-014/US-015.</summary>
        public event Action<Vector2> CandyBubbled;

        /// <summary>Raised at the pop position when a tap pops the bubble and restores normal
        /// gravity (US-006). Pop VFX + sound hook (US-014/US-015).</summary>
        public event Action<Vector2> BubblePopped;

        /// <summary>Raised at the cushion position when a tap emits a radial puff that nudges
        /// the candy via the solver's external-force hook (US-007). Puff VFX + sound hook
        /// (US-014/US-015).</summary>
        public event Action<Vector2> AirPuffed;

        public event Action<GameObject> StarCollected;
        public event Action CandyEaten;
        public event Action CandyLost;

        /// <summary>Raised on the Won transition with the run's stars + score payload
        /// (US-013's next-level flow consumes it; US-012 persists best score).</summary>
        public event Action<LevelResult> LevelCompleted;

        public void RaiseRopeCut(Vector2 at) => RopeCut?.Invoke(at);
        public void RaiseRopeAttached(Vector2 at) => RopeAttached?.Invoke(at);
        public void RaiseCandyBubbled(Vector2 at) => CandyBubbled?.Invoke(at);
        public void RaiseBubblePopped(Vector2 at) => BubblePopped?.Invoke(at);
        public void RaiseAirPuffed(Vector2 at) => AirPuffed?.Invoke(at);
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
            RopeAttached = null;
            CandyBubbled = null;
            BubblePopped = null;
            AirPuffed = null;
            StarCollected = null;
            CandyEaten = null;
            CandyLost = null;
            LevelCompleted = null;
        }
    }
}
