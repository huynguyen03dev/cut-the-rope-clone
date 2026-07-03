using Game.Core;
using UnityEngine;

/// <summary>
/// Candy ↔ world interaction via explicit physics queries inside the sim step
/// (decision 0008 — no trigger callbacks anywhere in this path). The driver
/// calls ResolveStep right after the solver step; results resolve in the
/// priority order encoded in Game.Core.InteractionResolver.
/// </summary>
public sealed class CandyInteractor : MonoBehaviour
{
    [SerializeField] Playfield playfield;
    [SerializeField] float candyRadius = 0.22f;
    [SerializeField] LayerMask starMask;
    [SerializeField] LayerMask mouthMask;
    [Tooltip("Swept (CircleCast) so fast candy cannot tunnel — empty until spikes (US-010).")]
    [SerializeField] LayerMask hazardMask;
    [Tooltip("US-005 auto-grab zones. Point-in-collider query inside the sim step " +
             "(decision 0008 — no trigger callbacks). Empty = no grab zones in this level.")]
    [SerializeField] LayerMask grabZoneMask;

    /// <summary>Set once a win or lose resolved; the session flow (US-003) will
    /// own what happens next. Queries stop immediately.</summary>
    public bool Finished { get; private set; }

    RopeSimulationDriver _driver;
    AutoGrabZone[] _grabZones; // cached in Awake (the driver/interactor rebuild every restart)
    bool _hasMultiUseZone;     // skip the re-arm scan entirely when every zone is single-use
    readonly Collider2D[] _starHits = new Collider2D[8]; // reused — no per-step allocations
    readonly Collider2D[] _grabHits = new Collider2D[8];  // US-005 auto-grab zones

    void Awake()
    {
        _driver = GetComponent<RopeSimulationDriver>();
        // Scene-scoped for the gray box; US-008 folds zones into the level prefab and this
        // lookup moves under the level root. Zero-arg FindObjectsByType is the non-deprecated
        // replacement for FindObjectsOfType on Unity 6 (active objects, no sort cost).
        _grabZones = FindObjectsByType<AutoGrabZone>();
        _hasMultiUseZone = false;
        foreach (AutoGrabZone zone in _grabZones)
            if (!zone.SingleUse) { _hasMultiUseZone = true; break; }
    }

    /// <summary>Called by the driver at the end of each fixed step.</summary>
    public void ResolveStep()
    {
        if (Finished) return;

        RopeSimulation sim = _driver.Sim;
        Vector2 pos = sim.Candy.Pos;
        Vector2 prev = _driver.CandyPrevStepPos;

        // US-005: auto-grab runs before win/lose resolution. A grab is a side-effect
        // (attach a rope), never a terminal outcome, so it must not be suppressed by a
        // same-frame win/lose; but we still bail once Finished below.
        ResolveGrabZones(sim, pos);

        int starCount = Physics2D.OverlapCircleNonAlloc(pos, candyRadius, _starHits, starMask);
        bool mouthHit = Physics2D.OverlapCircle(pos, candyRadius, mouthMask) != null;
        bool hazardHit = SweepHitsHazard(prev, pos);
        bool leftPlayfield = playfield != null && !playfield.Contains(pos);

        StepResolution res = InteractionResolver.Resolve(starCount > 0, mouthHit, leftPlayfield, hazardHit);

        if (res.CollectStars)
        {
            for (int i = 0; i < starCount; i++)
            {
                GameObject star = _starHits[i].gameObject;
                star.SetActive(false);
                _driver.Events.RaiseStarCollected(star);
            }
        }

        if (res.Outcome == StepOutcome.Won)
        {
            Finished = true;
            _driver.Events.RaiseCandyEaten();
        }
        else if (res.Outcome == StepOutcome.Lost)
        {
            Finished = true;
            _driver.Events.RaiseCandyLost();
        }
    }

    bool SweepHitsHazard(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float dist = delta.magnitude;
        if (dist < 1e-6f)
        {
            return Physics2D.OverlapCircle(to, candyRadius, hazardMask) != null;
        }
        return Physics2D.CircleCast(from, candyRadius, delta / dist, dist, hazardMask).collider != null;
    }

    /// <summary>
    /// US-005 auto-grab (DESIGN §2). A point-in-collider query so the grab fires exactly
    /// when the candy CENTER enters the zone radius ("enters the radius"). On entry the
    /// zone attaches a zero-energy rope via RopeSimulation.AddRope; single-use zones are
    /// consumed, multi-use zones re-arm once the candy leaves (one grab per entry).
    /// </summary>
    void ResolveGrabZones(RopeSimulation sim, Vector2 candyPos)
    {
        if (grabZoneMask == 0) return;

        int count = Physics2D.OverlapPointNonAlloc(candyPos, _grabHits, grabZoneMask);
        for (int i = 0; i < count; i++)
        {
            AutoGrabZone zone = _grabHits[i] != null ? _grabHits[i].GetComponent<AutoGrabZone>() : null;
            if (zone == null || zone.Used) continue;
            sim.AddRope(zone.AnchorPos, zone.Segments); // zero-energy attach
            zone.Consume();
            _driver.Events.RaiseRopeAttached(zone.AnchorPos);
        }

        // Re-arm multi-use zones the candy has left (single-use zones stay consumed).
        // Nothing to re-arm when every zone is single-use, so skip the scan — and the
        // geometric Contains test means no per-step allocation for the multi-use case.
        if (!_hasMultiUseZone || _grabZones == null) return;
        foreach (AutoGrabZone zone in _grabZones)
        {
            if (zone == null) continue; // destroyed
            if (zone.Used && !zone.Contains(candyPos)) zone.RearmIfMultiUse();
        }
    }
}
