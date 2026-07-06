using PrimeTween;
using UnityEngine;

/// <summary>
/// US-006 bubble (DESIGN §2, gameplay.md interactable #2). A translucent circle: when the
/// candy makes contact, the sim-step overlap pass (<see cref="CandyInteractor"/> — NOT an
/// OnTriggerEnter2D callback, per decision 0008) attaches this bubble and flips the candy
/// to buoyancy (<see cref="Game.Core.RopeSimulation.CandyGravityScale"/>). A classified
/// tap (<see cref="SwipeCutter"/>) pops it, restoring normal gravity with zero energy.
///
/// Like <see cref="AutoGrabZone"/>, the bubble does NOT mutate the solver itself — the
/// interactor owns the candy gravity state so the event bus is raised in one place. While
/// attached the bubble follows the candy (it "envelops" it); US-008 folds it into the
/// level prefab and US-015 extends the wobble/pop juice.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public sealed class Bubble : MonoBehaviour
{
    [Header("Contact")]
    [Tooltip("Contact radius (the CircleCollider2D is sized to this).")]
    [SerializeField] float radius = 0.6f;

    [Header("Buoyancy")]
    [Tooltip("Gravity multiplier applied to the candy while bubbled (DESIGN §2 per-point " +
             "gravity flip). Negative = floats up; -1 fully inverts gravity, a gentler " +
             "magnitude floats more slowly. Feel-tuned in US-015.")]
    [Range(-1f, 0f)] [SerializeField] float buoyancyScale = -0.6f;

    [Header("Visual")]
    [SerializeField] int circleSegments = 40;
    [SerializeField] float lineWidth = 0.05f;
    [SerializeField] Color color = new Color(0.6f, 0.85f, 1f, 0.7f);
    [SerializeField] string sortingLayer = "Rope";

    [Header("Wobble float (US-015 extends)")]
    [SerializeField] float wobbleScale = 0.06f;
    [SerializeField] float wobbleDuration = 1.1f;
    [SerializeField] float attachPunch = 0.25f;
    [SerializeField] float attachPunchDuration = 0.25f;
    [SerializeField] float popDuration = 0.18f;

    /// <summary>True once this bubble is holding the candy (buoyancy active).</summary>
    public bool Attached { get; private set; }

    /// <summary>True once popped; the interactor skips a popped bubble.</summary>
    public bool Popped { get; private set; }

    /// <summary>The candy gravity scale to apply while this bubble holds the candy.</summary>
    public float BuoyancyScale => buoyancyScale;

    /// <summary>Geometric point-in-bubble test (matches the collider radius), used to
    /// resolve a tap against the bubble at its current position — no physics-sync
    /// dependency, unlike an OverlapPoint from the input thread.</summary>
    public bool Contains(Vector2 worldPoint)
        => ((Vector2)transform.position - worldPoint).sqrMagnitude <= radius * radius;

    Transform _visualRoot;
    LineRenderer _ring;
    Material _ringMat;

    void Awake()
    {
        EnsureCollider();
        BuildVisual();
    }

    void OnDestroy()
    {
        if (_visualRoot != null) Tween.StopAll(_visualRoot);
        if (_ringMat != null) Destroy(_ringMat);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Popped ? new Color(0.5f, 0.5f, 0.5f, 0.3f) : new Color(0.6f, 0.85f, 1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    /// <summary>Called by <see cref="CandyInteractor"/> when the candy makes contact.
    /// Marks the bubble attached and starts the wobble; the interactor applies the candy
    /// gravity flip and raises the event so the buoyancy state has one owner.</summary>
    public void Attach()
    {
        if (Attached || Popped) return;
        Attached = true;
        PlayAttachEffect();
    }

    /// <summary>Keeps the bubble centered on the candy while attached — it "envelops" the
    /// candy and floats with it. Called each sim step by the interactor.</summary>
    public void Follow(Vector2 candyPos)
    {
        Vector3 p = transform.position;
        transform.position = new Vector3(candyPos.x, candyPos.y, p.z);
    }

    /// <summary>Called by <see cref="CandyInteractor.TryPopBubble"/> when a tap lands on the
    /// bubble. Plays the pop and removes the bubble; the interactor restores normal gravity.</summary>
    public void Pop()
    {
        if (Popped) return;
        Popped = true;
        Attached = false;
        PlayPopEffect();
    }

    void PlayAttachEffect()
    {
        if (_visualRoot == null) return;
        Tween.StopAll(_visualRoot);
        // Envelop punch, settle back to rest, then a gentle endless wobble around rest so it
        // reads as a floating bubble (US-015 extends the feel).
        Tween.Scale(_visualRoot, 1f + attachPunch, attachPunchDuration, Ease.OutBack)
            .Chain(Tween.Scale(_visualRoot, 1f, attachPunchDuration * 0.6f, Ease.OutCubic))
            .OnComplete(this, b => b.StartWobble());
    }

    void StartWobble()
    {
        if (_visualRoot == null || Popped) return;
        Tween.Scale(_visualRoot, 1f + wobbleScale, wobbleDuration, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
    }

    void PlayPopEffect()
    {
        var col = GetComponent<CircleCollider2D>();
        if (col != null) col.enabled = false; // stop enveloping immediately
        if (_visualRoot == null) return;
        Tween.StopAll(_visualRoot);
        Tween.Scale(_visualRoot, 1.4f, popDuration, Ease.OutQuad);
        FadeOutAndDisable();
    }

    void FadeOutAndDisable()
    {
        if (_ringMat == null) return;
        Tween.MaterialAlpha(_ringMat, 0f, popDuration, Ease.InQuad)
            .OnComplete(this, b => { if (b._visualRoot != null) b._visualRoot.gameObject.SetActive(false); });
    }

    void EnsureCollider()
    {
        var col = GetComponent<CircleCollider2D>();
        col.radius = radius;
        col.isTrigger = true;
    }

    void BuildVisual()
    {
        _ringMat = new Material(Shader.Find("Sprites/Default"));
        var rootGo = new GameObject("BubbleRing");
        rootGo.transform.SetParent(transform, false);
        rootGo.transform.localPosition = Vector3.zero;
        _visualRoot = rootGo.transform;

        _ring = rootGo.AddComponent<LineRenderer>();
        _ring.sharedMaterial = _ringMat;
        _ring.widthMultiplier = lineWidth;
        _ring.startColor = color;
        _ring.endColor = color;
        _ring.numCapVertices = 2;
        _ring.sortingLayerName = sortingLayer;
        _ring.useWorldSpace = false;
        _ring.loop = true;
        int count = Mathf.Max(3, circleSegments);
        _ring.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            float a = (i / (float)count) * Mathf.PI * 2f;
            _ring.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius);
        }
    }

    void OnValidate()
    {
        var col = GetComponent<CircleCollider2D>();
        if (col != null) col.radius = radius;
    }
}
