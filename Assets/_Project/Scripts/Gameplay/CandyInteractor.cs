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
    [Tooltip("US-006 bubbles. Candy-circle overlap query inside the sim step " +
             "(decision 0008 — no trigger callbacks). Empty = no bubbles in this level.")]
    [SerializeField] LayerMask bubbleMask;
    [Tooltip("US-007 air cushions. A tap on a cushion (point-in-collider) emits a radial " +
             "puff via the solver's external-force hook. Empty = no cushions in this level.")]
    [SerializeField] LayerMask cushionMask;

    /// <summary>Set once a win or lose resolved; the session flow (US-003) will
    /// own what happens next. Queries stop immediately.</summary>
    public bool Finished { get; private set; }

    RopeSimulationDriver _driver;
    AutoGrabZone[] _grabZones; // cached in Awake (the driver/interactor rebuild every restart)
    bool _hasMultiUseZone;     // skip the re-arm scan entirely when every zone is single-use
    Bubble _activeBubble;      // the bubble currently holding the candy (buoyancy on), or null
    AirCushion[] _cushions;    // US-007: cached for tap resolution (point-in-collider per tap)
    readonly Collider2D[] _starHits = new Collider2D[8]; // reused — no per-step allocations
    readonly Collider2D[] _grabHits = new Collider2D[8];  // US-005 auto-grab zones
    readonly Collider2D[] _bubbleHits = new Collider2D[8]; // US-006 bubbles
    readonly Collider2D[] _cushionBubbleHits = new Collider2D[8]; // US-007 free-bubble push

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
        _activeBubble = null;
        _cushions = FindObjectsByType<AirCushion>();
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

        // US-006: bubble attach is likewise a side-effect resolved before win/lose. The
        // attached bubble then follows the candy each step so it "envelops" it while floating.
        ResolveBubbles(sim, pos);

        int starCount = Physics2D.OverlapCircle(pos, candyRadius, CreateContactFilter(starMask), _starHits);
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

        int count = Physics2D.OverlapPoint(candyPos, CreateContactFilter(grabZoneMask), _grabHits);
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

    /// <summary>
    /// US-006 bubble attach (DESIGN §2). A candy-circle overlap so the bubble envelops the
    /// candy on contact. On attach the candy's gravity flips to the bubble's buoyancy scale
    /// (per-point gravity in <see cref="RopeSimulation.CandyGravityScale"/> — zero energy,
    /// only future acceleration changes). One bubble holds the candy at a time; while
    /// attached the bubble follows the candy so a tap can land on it as it floats.
    /// </summary>
    void ResolveBubbles(RopeSimulation sim, Vector2 candyPos)
    {
        if (_activeBubble != null)
        {
            _activeBubble.Follow(candyPos);
            return; // one bubble at a time; ignore other overlaps until it pops
        }
        if (bubbleMask == 0) return;

        int count = Physics2D.OverlapCircle(candyPos, candyRadius, CreateContactFilter(bubbleMask), _bubbleHits);
        for (int i = 0; i < count; i++)
        {
            Bubble bubble = _bubbleHits[i] != null ? _bubbleHits[i].GetComponent<Bubble>() : null;
            if (bubble == null || bubble.Popped || bubble.Attached) continue;
            bubble.Attach();
            sim.CandyGravityScale = bubble.BuoyancyScale; // flip to buoyancy (zero energy)
            _activeBubble = bubble;
            _activeBubble.Follow(candyPos);
            _driver.Events.RaiseCandyBubbled(candyPos);
            break; // attach at most one per step
        }
    }

    /// <summary>
    /// US-006 tap-to-pop. Called by <see cref="SwipeCutter"/> (via the driver) with a tap
    /// already classified as a tap — never a swipe — so one touch cannot both pop and cut.
    /// Pops the bubble holding the candy if the tap landed on it, restoring normal gravity
    /// with zero energy injected. Returns true when a pop happened.
    /// </summary>
    public bool TryPopBubble(Vector2 worldPoint)
    {
        if (_activeBubble == null || !_activeBubble.Contains(worldPoint)) return false;
        _activeBubble.Pop();
        _driver.Sim.CandyGravityScale = 1f; // restore normal gravity, no energy injected
        _driver.Events.RaiseBubblePopped(worldPoint);
        _activeBubble = null;
        return true;
    }

    /// <summary>
    /// US-007 tap-to-puff. Called by <see cref="SwipeCutter"/> (via the driver) AFTER a tap
    /// has been classified and <see cref="TryPopBubble"/> did not consume it, so one touch is
    /// EITHER a cut OR a pop OR a puff — never more than one. Finds the cushion whose tap
    /// radius contains the point (decision 0008 — a one-shot input query, not a trigger
    /// callback), consumes its cooldown, computes the radial impulse from the cushion's config
    /// against the candy's CURRENT sim position, and applies it through the solver's
    /// external-force hook (<see cref="RopeSimulation.ApplyCandyImpulse"/> — prevPos -=
    /// impulse * dt, a one-frame velocity nudge with no Pos/PrevPos touch beyond that). Also
    /// pushes free bubbles in range (gameplay.md interactable #3). Returns true when a puff
    /// fired (a tap on empty space or a cooling cushion returns false).
    /// </summary>
    public bool TryTapAirCushion(Vector2 worldPoint)
    {
        if (cushionMask == 0 || _cushions == null) return false;

        AirCushion target = null;
        foreach (AirCushion c in _cushions)
        {
            if (c == null) continue; // destroyed between cache and tap
            if (c.Contains(worldPoint)) { target = c; break; }
        }
        if (target == null) return false;

        float now = Time.time;
        if (!target.BeginPuff(now)) return false; // cooling — consume nothing

        // Apply the radial impulse to the candy via the solver's external-force hook.
        Vector2 candyPos = _driver.Sim.Candy.Pos;
        Vector2 impulse = AirPuff.ComputeImpulse(target.Origin, candyPos,
            target.PuffMagnitude, target.InnerRadius, target.MaxRadius);
        _driver.ApplyAirPuff(impulse);

        // gameplay.md: the puff also pushes free bubbles in range. The active (candy-held)
        // bubble follows the candy and so rides the impulse; nudging it again would double-
        // apply, so only free bubbles (not attached, not popped) within maxRadius are nudged
        // by a proportional translate. Bubbles have no physics velocity model, so this is a
        // direct transform translate — minimal but reads correctly in the gray-box scene.
        PushFreeBubbles(target.Origin, target.MaxRadius, target.PuffMagnitude,
            target.InnerRadius, target.MaxRadius);

        _driver.Events.RaiseAirPuffed(target.Origin);
        return true;
    }

    /// <summary>US-007 free-bubble push: a puff nudges any free (not attached, not popped)
    /// bubble whose collider overlaps the puff's reach by translating it radially outward by
    /// the same falloff-scaled magnitude (treated as a one-frame displacement). The candy's
    /// active bubble is skipped — it follows the candy and already received the impulse.</summary>
    void PushFreeBubbles(Vector2 origin, float reach, float magnitude, float inner, float max)
    {
        if (bubbleMask == 0) return;
        int count = Physics2D.OverlapCircle(origin, reach, CreateContactFilter(bubbleMask), _cushionBubbleHits);
        for (int i = 0; i < count; i++)
        {
            Bubble bubble = _cushionBubbleHits[i] != null ? _cushionBubbleHits[i].GetComponent<Bubble>() : null;
            if (bubble == null || bubble.Attached || bubble.Popped) continue;
            Vector2 bp = bubble.transform.position;
            Vector2 imp = AirPuff.ComputeImpulse(origin, bp, magnitude, inner, max);
            if (imp.sqrMagnitude > 1e-8f) bubble.transform.position = bp + imp * Time.fixedDeltaTime;
        }
    }

    static ContactFilter2D CreateContactFilter(LayerMask layerMask)
    {
        // Every interactable collider (star, grab zone, bubble) is a trigger, and a default
        // ContactFilter2D EXCLUDES triggers — so the overlap must opt in explicitly or it
        // silently returns nothing. (The mouth query dodges this by using the simple
        // Physics2D.OverlapCircle(point, radius, layerMask) overload, which honors the global
        // queriesHitTriggers instead of a filter.)
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(layerMask);
        filter.useTriggers = true;
        return filter;
    }
}
