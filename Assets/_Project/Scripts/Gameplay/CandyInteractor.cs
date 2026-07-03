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

    /// <summary>Set once a win or lose resolved; the session flow (US-003) will
    /// own what happens next. Queries stop immediately.</summary>
    public bool Finished { get; private set; }

    RopeSimulationDriver _driver;
    readonly Collider2D[] _starHits = new Collider2D[8]; // reused — no per-step allocations

    void Awake() => _driver = GetComponent<RopeSimulationDriver>();

    /// <summary>Called by the driver at the end of each fixed step.</summary>
    public void ResolveStep()
    {
        if (Finished) return;

        RopeSimulation sim = _driver.Sim;
        Vector2 pos = sim.Candy.Pos;
        Vector2 prev = _driver.CandyPrevStepPos;

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
}
