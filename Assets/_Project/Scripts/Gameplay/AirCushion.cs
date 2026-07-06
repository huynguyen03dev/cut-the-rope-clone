using Game.Core;
using PrimeTween;
using UnityEngine;

/// <summary>
/// US-007 air cushion (DESIGN §2 external forces, gameplay.md interactable #3). A static
/// circular tap target: a classified tap (<see cref="SwipeCutter"/>) emits a radial puff
/// that nudges the candy via the solver's external-force hook
/// (<see cref="RopeSimulation.ApplyCandyImpulse"/> — "prevPos -= impulse * dt"), with
/// magnitude that is full inside <see cref="innerRadius"/> and falls off linearly to zero at
/// <see cref="maxRadius"/>. A <see cref="PuffCooldown"/> blocks retriggering and drives the
/// ready-vs-cooling visual.
///
/// Like <see cref="Bubble"/> and <see cref="AutoGrabZone"/>, the cushion does NOT mutate the
/// solver itself — <see cref="CandyInteractor.TryTapAirCushion"/> computes the impulse from
/// this cushion's config + the candy's current sim position, applies it through the driver,
/// and raises <see cref="GameEvents.AirPuffed"/> so the event bus has one owner. The cushion
/// owns only the cooldown gate + the visual so a per-entity ready state survives across the
/// sim-step boundary. Decision 0008 applies: tap resolution is a one-shot input query, not a
/// trigger callback; the <see cref="CircleCollider2D"/> exists only as a tap target and a
/// scene-authoring aid.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public sealed class AirCushion : MonoBehaviour
{
    [Header("Tap target")]
    [Tooltip("Tap radius (the CircleCollider2D is sized to this). A classified tap inside " +
             "this radius emits a puff.")]
    [SerializeField] float radius = 0.7f;

    [Header("Puff (DESIGN §2 external force)")]
    [Tooltip("Impulse magnitude applied to the candy at the inner radius (units/sec; the " +
             "Verlet nudge is prevPos -= impulse * dt, so this is independent of timestep). " +
             "Feel-tuned in US-015.")]
    [SerializeField] float puffMagnitude = 6f;
    [Tooltip("Distance from the cushion within which the puff is at full magnitude.")]
    [SerializeField] float innerRadius = 0.5f;
    [Tooltip("Distance at which the puff falls off to zero. Linear falloff between inner and " +
             "max. Must be >= innerRadius (clamped in the math either way).")]
    [SerializeField] float maxRadius = 3.5f;

    [Header("Cooldown")]
    [Tooltip("Seconds the cushion is inert after a puff before it can fire again. " +
             "Cancellation-safe: the gate is pure wall-clock, no UniTask coroutine to leak.")]
    [Range(0f, 5f)] [SerializeField] float cooldownSeconds = 0.6f;

    [Header("Visual")]
    [SerializeField] int dashCount = 24;
    [SerializeField] float dashArcDegrees = 10f;
    [SerializeField] float dashWidth = 0.12f;
    [SerializeField] Color readyColor = new Color(1f, 0.85f, 0.35f, 1f);
    [SerializeField] Color coolingColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
    [SerializeField] string sortingLayer = "Rope";
    [Tooltip("Punch scale on puff; settles back to 1.")]
    [SerializeField] float puffPunch = 0.25f;
    [SerializeField] float puffPunchDuration = 0.18f;

    /// <summary>The cushion origin for the radial puff (its world position).</summary>
    public Vector2 Origin => transform.position;

    /// <summary>Tap-target radius (matches the trigger collider).</summary>
    public float Radius => radius;

    public float PuffMagnitude => puffMagnitude;
    public float InnerRadius => innerRadius;
    public float MaxRadius => maxRadius;
    public float CooldownSeconds => cooldownSeconds;

    /// <summary>The cooldown gate. The interactor calls <see cref="BeginPuff"/> to consume a
    /// tap (which advances the gate and plays the trigger visual); the visual reads
    /// <see cref="PuffCooldown.Ready01"/> each frame to lerp ready -> cooling color.</summary>
    public PuffCooldown Cooldown { get; private set; }

    Transform _visualRoot;
    Material _dashMat;
    Color _currentTint;

    void Awake()
    {
        EnsureCollider();
        BuildVisual();
        _currentTint = readyColor;
        ApplyTint(_currentTint);
    }

    void OnDestroy()
    {
        if (_visualRoot != null) Tween.StopAll(_visualRoot);
        if (_dashMat != null) Destroy(_dashMat);
    }

    void Update()
    {
        // Lerp the ring color from cooling (just fired, Ready01=0) back to ready (Ready01=1).
        float r = Cooldown.Ready01(Time.time, cooldownSeconds);
        Color target = Color.Lerp(coolingColor, readyColor, r);
        if (target != _currentTint)
        {
            _currentTint = target;
            ApplyTint(target);
        }
    }

    void OnDrawGizmos()
    {
        bool ready = !Application.isPlaying || Cooldown.Ready(Time.time);
        Gizmos.color = ready ? new Color(1f, 0.85f, 0.35f, 0.9f) : new Color(0.45f, 0.45f, 0.45f, 0.6f);
        DrawDashedGizmo(transform.position, radius, dashCount, dashArcDegrees);
        // Faint falloff reach ring so designers can see where the puff ends.
        Gizmos.color = new Color(1f, 0.85f, 0.35f, 0.18f);
        Gizmos.DrawWireSphere(transform.position, maxRadius);
    }

    static void DrawDashedGizmo(Vector3 center, float r, int dashes, float arcDeg)
    {
        float gap = 360f / dashes;
        for (int i = 0; i < dashes; i++)
        {
            float a0 = i * gap;
            float a1 = a0 + arcDeg;
            Gizmos.DrawLine(center + DegToXZ(a0, r), center + DegToXZ(a1, r));
        }
    }

    static Vector3 DegToXZ(float deg, float r)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * r;
    }

    /// <summary>Geometric point-in-cushion tap test (matches the trigger collider radius), used
    /// to resolve a tap against the cushion at its current position — no physics-sync
    /// dependency, unlike an OverlapPoint from the input thread (mirrors <see cref="Bubble"/>).
    /// </summary>
    public bool Contains(Vector2 worldPoint)
        => ((Vector2)transform.position - worldPoint).sqrMagnitude <= radius * radius;

    /// <summary>Called by <see cref="CandyInteractor.TryTapAirCushion"/> when a tap lands on
    /// this cushion. Consumes the cooldown (returns false and does nothing if still cooling)
    /// and plays the trigger visual; the interactor then computes and applies the impulse via
    /// the driver and raises <see cref="GameEvents.AirPuffed"/>. The solver is NOT touched
    /// here — the interactor owns the candy state so the event bus is raised in one place.</summary>
    public bool BeginPuff(float now)
    {
        // PuffCooldown is a struct and Cooldown is an auto-property, so TryFire would mutate
        // a throwaway copy returned by the getter — ReadyAt would never persist and the
        // cooldown would never block. Pull a local, mutate, assign back.
        //
        // Regression guard: `Game.Gameplay.Tests.AirCushionCooldownTests` exercises this
        // exact method (inactive GameObject so Awake/visuals never run) and asserts ReadyAt
        // persists + a same-instant re-tap is blocked. Reverting to the buggy
        // `Cooldown.TryFire(...)` form fails those tests (ReadyAt stays 0, re-tap fires).
        var cd = Cooldown;
        if (!cd.TryFire(now, cooldownSeconds)) return false;
        Cooldown = cd;
        PlayPuffEffect();
        return true;
    }

    void PlayPuffEffect()
    {
        if (_visualRoot == null) return;
        Tween.StopAll(_visualRoot);
        // Snap to the punch scale then settle back to 1 — a single outward pulse on tap.
        Tween.Scale(_visualRoot, 1f + puffPunch, puffPunchDuration * 0.5f, Ease.OutBack)
            .Chain(Tween.Scale(_visualRoot, Vector3.one, puffPunchDuration, Ease.OutCubic));
    }

    void EnsureCollider()
    {
        var col = GetComponent<CircleCollider2D>();
        col.radius = radius;
        col.isTrigger = true;
    }

    void ApplyTint(Color c)
    {
        if (_dashMat == null) return;
        _dashMat.color = c;
    }

    void BuildVisual()
    {
        _dashMat = new Material(Shader.Find("Sprites/Default"));
        var rootGo = new GameObject("CushionRing");
        rootGo.transform.SetParent(transform, false);
        rootGo.transform.localPosition = Vector3.zero;
        _visualRoot = rootGo.transform;

        float gap = 360f / Mathf.Max(1, dashCount);
        for (int i = 0; i < dashCount; i++)
        {
            float a0 = i * gap;
            float a1 = a0 + dashArcDegrees;
            CreateDashArc(rootGo.transform, a0, a1);
        }
    }

    void CreateDashArc(Transform parent, float a0Deg, float a1Deg)
    {
        var go = new GameObject("dash");
        go.transform.SetParent(parent, false);
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = _dashMat;
        line.widthMultiplier = dashWidth;
        line.startColor = Color.white; // tint comes from the shared material color
        line.endColor = Color.white;
        line.numCapVertices = 2;
        line.sortingLayerName = sortingLayer;
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.SetPosition(0, DegToXZ(a0Deg, radius));
        line.SetPosition(1, DegToXZ(a1Deg, radius));
    }

    void OnValidate()
    {
        var col = GetComponent<CircleCollider2D>();
        if (col != null) col.radius = radius;
    }
}
