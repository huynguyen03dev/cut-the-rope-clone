using Game.Core;
using UnityEngine;

/// <summary>
/// Owns the rope simulation: builds it from authored anchors, steps it on the
/// fixed timestep, and exposes the interpolation alpha for everything that
/// renders or tests against what the player sees.
/// </summary>
[DefaultExecutionOrder(-100)]
public sealed class RopeSimulationDriver : MonoBehaviour
{
    [Header("Feel — the four numbers (DESIGN §6)")]
    [Tooltip("Points per rope. Applied on (re)build, not live.")]
    [Range(4, 24)] [SerializeField] int segmentCount = 12;
    [Tooltip("Constraint relaxation iterations per fixed step. Live.")]
    [Range(1, 40)] [SerializeField] int iterationCount = 16;
    [Tooltip("Velocity kept per step. Live.")]
    [Range(0.8f, 1f)] [SerializeField] float damping = 0.99f;
    [Tooltip("Candy inverse mass — lower is heavier. Live.")]
    [Range(0.01f, 1f)] [SerializeField] float candyInvMass = 0.05f;

    [Header("World")]
    [SerializeField] float gravity = 20f;
    [SerializeField] Transform candyStart;
    [Tooltip("Left empty, anchors are collected from children at Awake.")]
    [SerializeField] RopeAnchor[] anchors;

    [Header("Cut aftermath (US-003, DESIGN §2)")]
    [Tooltip("Seconds the candy-side stub retracts/fades before despawning.")]
    [SerializeField] float stubFadeDuration = 0.5f;
    [Tooltip("Per-second lerp factor retracting the stub toward the candy.")]
    [SerializeField] float retractRate = 6f;

    public RopeSimulation Sim { get; private set; }

    /// <summary>Event channels. Owned by GameSession and rebuilt per level (US-003);
    /// a lazy default keeps direct Play-in-Game-scene (no Boot) working.</summary>
    GameEvents _events;
    public GameEvents Events => _events ??= new GameEvents();

    /// <summary>Inject a GameSession-owned event bus. Called after the level is
    /// instantiated, before Start, so subscribers bind to the live per-level bus.</summary>
    public void UseEvents(GameEvents events) { if (events != null) _events = events; }

    /// <summary>Candy position at the start of the current fixed step — the
    /// 'from' end of swept hazard queries (US-002/US-010).</summary>
    public Vector2 CandyPrevStepPos { get; private set; }

    /// <summary>Unity's fixed-time accumulator fraction for render interpolation.</summary>
    public float Alpha => (Time.time - Time.fixedTime) / Time.fixedDeltaTime;

    public int MaxPointsPerRope => segmentCount + 1; // points + shared candy

    CandyInteractor _interactor;

    /// <summary>The one cut entry point: severs at most one rope crossed by the
    /// swipe segment and raises RopeCut at the cut site. Input (SwipeCutter) and
    /// tests go through here so the event always accompanies the cut.</summary>
    public bool CutAt(Vector2 swipeFrom, Vector2 swipeTo, float alpha)
    {
        if (!Sim.TryCut(swipeFrom, swipeTo, alpha, out Vector2 cutPos)) return false;
        Events.RaiseRopeCut(cutPos);
        return true;
    }

    /// <summary>Tap-to-pop entry point (US-006): forwards a classified tap to the
    /// interactor's bubble query. Mirrors <see cref="CutAt"/> so input goes through the
    /// driver. Returns true when a bubble was popped.</summary>
    public bool TryPopBubble(Vector2 worldPoint)
        => _interactor != null && _interactor.TryPopBubble(worldPoint);

    void Awake()
    {
        Sim = new RopeSimulation(candyStart.position, candyInvMass);
        if (anchors == null || anchors.Length == 0)
        {
            anchors = GetComponentsInChildren<RopeAnchor>();
        }
        foreach (RopeAnchor anchor in anchors)
        {
            Sim.AddRope(anchor.transform.position, segmentCount);
        }
        CandyPrevStepPos = Sim.Candy.Pos;
        PushFeelParams();
        _interactor = GetComponent<CandyInteractor>();
    }

    void FixedUpdate()
    {
        PushFeelParams();
        CandyPrevStepPos = Sim.Candy.Pos;
        Sim.Step(Time.fixedDeltaTime);
        // explicit queries resolve at the end of the sim step (decision 0008)
        if (_interactor != null) _interactor.ResolveStep();
    }

    // pushed every step so inspector tuning works live in play mode (the M0
    // feel loop); segment count is the one build-time parameter.
    void PushFeelParams()
    {
        Sim.Iterations = iterationCount;
        Sim.Damping = damping;
        Sim.Gravity = new Vector2(0f, -gravity);
        Sim.Candy.InvMass = candyInvMass;
        Sim.StubFadeDuration = stubFadeDuration;
        Sim.RetractRate = retractRate;
    }
}
