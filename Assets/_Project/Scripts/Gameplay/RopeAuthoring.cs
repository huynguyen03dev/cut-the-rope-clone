using UnityEngine;

/// <summary>
/// Authored rope for the US-008 level pipeline. Pins a rope from
/// <see cref="anchorOverride"/> (defaults to this transform) to the level candy, with a
/// per-rope rest length and segment count that override the driver defaults so each rope in
/// a level is independently tunable. The driver collects <see cref="RopeAuthoring"/> from its
/// children at Awake and bakes each into solver points via
/// <see cref="Game.Core.RopeSimulation.AddRope(Vector2, int, float)"/>.
///
/// The target is candy-only for M2a: the solver is candy-centric (the candy is the shared
/// terminal point of every rope). The <see cref="RopeTarget"/> enum is forward-compatible so
/// a future 'FreeEnd' (decorative/hanging) target can be added without re-authoring levels.
/// </summary>
public sealed class RopeAuthoring : MonoBehaviour
{
    /// <summary>What the loose end of the rope attaches to. Candy is the only M2a target.</summary>
    public enum RopeTarget { Candy }

    [Tooltip("Where this rope is pinned. Defaults to this GameObject's transform.")]
    [SerializeField] Transform anchorOverride;
    [Tooltip("What the loose end attaches to. Candy = the level's candy (only target in M2a).")]
    [SerializeField] RopeTarget target = RopeTarget.Candy;
    [Tooltip("Rest length of the WHOLE rope (world units). ≤0 = anchor→candy distance at build "
             + "(zero-energy attach, matches auto-grab).")]
    [SerializeField] float restLength = 0f;
    [Tooltip("Number of segments (~10–16). More = smoother but costlier.")]
    [Range(4, 24)] [SerializeField] int segmentCount = 12;

    /// <summary>World position this rope is pinned to.</summary>
    public Vector2 AnchorPosition => (anchorOverride != null ? anchorOverride : transform).position;
    /// <summary>The authored target for the loose end.</summary>
    public RopeTarget Target => target;
    /// <summary>Number of segments to bake.</summary>
    public int SegmentCount => segmentCount;
    /// <summary>Authored whole-rope rest length (≤0 means 'use anchor→candy distance').</summary>
    public float RestLength => restLength;

    void OnDrawGizmos()
    {
        Vector2 a = AnchorPosition;
        // Rope intent: a faint tan hang line below the anchor so the pin + intended rope are
        // visible while authoring (there is no live candy in edit mode).
        Gizmos.color = new Color(0.78f, 0.64f, 0.41f, 0.6f);
        Gizmos.DrawLine(a, a + Vector2.down * 1.5f);
        // Anchor marker (matches RopeAnchor's cyan sphere so the two authoring styles read alike).
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(a, 0.12f);
    }
}
