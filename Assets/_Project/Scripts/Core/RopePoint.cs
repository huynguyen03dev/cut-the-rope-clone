using UnityEngine;

namespace Game.Core
{
    /// <summary>One particle in the Verlet simulation (DESIGN §2).</summary>
    public struct RopePoint
    {
        public Vector2 Pos;

        /// <summary>Verlet integration state — NOT a render snapshot.</summary>
        public Vector2 PrevPos;

        /// <summary>Snapshot of Pos taken at the start of each fixed step; render
        /// reads lerp(RenderPos, Pos, alpha).</summary>
        public Vector2 RenderPos;

        /// <summary>0 = pinned anchor; rope points 1; the candy is low (heavy).</summary>
        public float InvMass;

        /// <summary>PrevPos = Pos, so a freshly spawned point carries zero energy.</summary>
        public static RopePoint At(Vector2 pos, float invMass) => new RopePoint
        {
            Pos = pos,
            PrevPos = pos,
            RenderPos = pos,
            InvMass = invMass,
        };
    }
}
