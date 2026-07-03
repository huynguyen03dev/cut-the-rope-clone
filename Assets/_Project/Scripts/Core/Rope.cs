namespace Game.Core
{
    /// <summary>
    /// One chain of Verlet points. Points[0] is the pinned anchor for ropes and
    /// anchor-side stubs; candy-side stubs have no pinned point and hang from the
    /// shared candy point via their terminal constraint.
    /// </summary>
    public sealed class Rope
    {
        public RopePoint[] Points;

        /// <summary>Rest length of each segment, fixed at spawn (anchor→candy
        /// distance / segment count — the zero-energy rule).</summary>
        public float RestLength;

        /// <summary>When true the last point is constrained to the shared candy point.</summary>
        public bool AttachedToCandy;

        /// <summary>Stubs produced by a cut leave the swipe test set immediately
        /// and are never cuttable again (US-001 domain rule).</summary>
        public bool Cuttable;

        /// <summary>US-003 cut aftermath (DESIGN §2): the candy-side stub retracts
        /// toward the shared candy point and fades over <see cref="FadeDuration"/>,
        /// then leaves the simulation. Anchor-side stubs keep hanging. Free-floating
        /// middle pieces (no pin, not attached to candy) fade and despawn too. While
        /// <c>Fading</c> a rope skips integration and constraint solving so the solver
        /// cannot fight the retract.</summary>
        public bool Fading;

        /// <summary>Remaining fade time in seconds; reaches zero, the rope is dropped.</summary>
        public float FadeTime;

        /// <summary>Total fade duration in seconds (drives the visual alpha/width ratio).</summary>
        public float FadeDuration;
    }
}
