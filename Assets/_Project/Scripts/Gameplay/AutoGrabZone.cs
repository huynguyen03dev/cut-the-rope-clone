using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

/// <summary>
/// US-005 auto-grab zone (DESIGN §2, gameplay.md interactable #1). A dashed-circle
/// trigger: when the candy's center enters the radius, the sim-step query pass
/// (<see cref="CandyInteractor"/>, US-002 infrastructure — NOT an OnTriggerEnter2D
/// callback, per decision 0008) attaches a new zero-energy rope from this zone's
/// position to the candy. The rope is cuttable and shares the candy terminal point.
///
/// The zone uses its own <see cref="Transform.position"/> as the anchor. It MUST NOT
/// carry a <see cref="RopeAnchor"/> under the <see cref="RopeSimulationDriver"/>
/// hierarchy, or the driver's load-time scan would spawn a rope for it immediately.
/// Place the zone as a sibling gameplay entity (like Star / OmNomMouth); US-008 folds
/// it into the level prefab.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public sealed class AutoGrabZone : MonoBehaviour
{
    [Header("Rope")]
    [Tooltip("Verlet segments of the spawned rope (interior + anchor; candy is shared).")]
    [Range(4, 24)] [SerializeField] int segments = 12;

    [Header("Grab")]
    [Tooltip("Grab radius (the CircleCollider2D is sized to this).")]
    [SerializeField] float radius = 1f;
    [Tooltip("Single-use (default, matching the original). Uncheck for a re-arming zone " +
             "that grabs once per entry.")]
    [SerializeField] bool singleUse = true;

    [Header("Dashed-circle visual")]
    [SerializeField] int dashCount = 24;
    [SerializeField] float dashArcDegrees = 8f;
    [SerializeField] float dashWidth = 0.10f;
    [SerializeField] Color color = new Color(0.40f, 0.90f, 1f, 1f);
    [SerializeField] string sortingLayer = "Rope";

    [Header("Attach effect")]
    [SerializeField] float punchScale = 1.3f;
    [SerializeField] float punchDuration = 0.2f;
    [SerializeField] float fadeOutDuration = 0.35f;

    /// <summary>True once a single-use zone has grabbed; the interactor skips it.
    /// For a multi-use zone this is reset to false whenever the candy leaves the radius
    /// (one grab per entry).</summary>
    public bool Used { get; private set; }

    /// <summary>Anchor position for <c>RopeSimulation.AddRope</c>.</summary>
    public Vector2 AnchorPos => transform.position;
    public int Segments => segments;

    /// <summary>True for the default single-use zone; false for a re-arming zone.</summary>
    public bool SingleUse => singleUse;

    /// <summary>Geometric point-in-zone test (matches the trigger collider radius),
    /// used by the interactor to re-arm a multi-use zone once the candy has left —
    /// no per-step allocation, unlike tracking the overlap set.</summary>
    public bool Contains(Vector2 worldPoint)
        => ((Vector2)transform.position - worldPoint).sqrMagnitude <= radius * radius;

    Transform _visualRoot;
    readonly List<LineRenderer> _dashes = new List<LineRenderer>();
    Material _dashMat;

    void Awake()
    {
        EnsureCollider();
        BuildVisual();
    }

    void OnDestroy()
    {
        if (_visualRoot != null) Tween.StopAll(_visualRoot);
        if (_dashMat != null) Destroy(_dashMat);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Used ? new Color(0.5f, 0.5f, 0.5f, 0.4f) : new Color(0.35f, 0.85f, 1f, 0.8f);
        // Dashed wire circle: draw arcs so it reads as dashed in-editor too.
        DrawDashedGizmo(transform.position, radius, dashCount, dashArcDegrees);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.10f); // anchor marker
    }

    static void DrawDashedGizmo(Vector3 center, float r, int dashes, float arcDeg)
    {
        float gap = 360f / dashes;
        for (int i = 0; i < dashes; i++)
        {
            float a0 = i * gap;
            float a1 = a0 + arcDeg;
            Vector3 p0 = center + DegToXZ(a0, r);
            Vector3 p1 = center + DegToXZ(a1, r);
            Gizmos.DrawLine(p0, p1);
        }
    }

    static Vector3 DegToXZ(float deg, float r)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * r;
    }

    /// <summary>Called by <see cref="CandyInteractor"/> when the candy enters the radius.
    /// Marks the zone consumed and plays the attach effect. Does NOT spawn the rope —
    /// the interactor does that via <c>RopeSimulation.AddRope</c> so the event bus is
    /// raised in one place.</summary>
    public void Consume()
    {
        bool firstGrab = !Used;
        Used = true;
        PlayAttachEffect(firstGrab);
    }

    /// <summary>Multi-use re-arm: called by the interactor when the candy is no longer
    /// inside the radius. Single-use zones stay consumed.</summary>
    public void RearmIfMultiUse()
    {
        if (!singleUse) Used = false;
    }

    void PlayAttachEffect(bool firstGrab)
    {
        if (_visualRoot == null) return;
        Tween.StopAll(_visualRoot); // cancel any in-flight pulse/sequence
        if (singleUse)
        {
            // Pulse then settle; the fade coroutine disables the visual once spent.
            Tween.Scale(_visualRoot, punchScale, punchDuration, Ease.OutBack)
                .Chain(Tween.Scale(_visualRoot, Vector3.one, punchDuration * 0.6f, Ease.OutCubic));
            FadeOutAndDisable(firstGrab).Forget();
        }
        else
        {
            // Quick pulse; the dashed circle stays for the next entry.
            Tween.PunchScale(_visualRoot, Vector3.one * (punchScale - 1f), punchDuration, 6);
        }
    }

    async UniTaskVoid FadeOutAndDisable(bool firstGrab)
    {
        if (!firstGrab) return; // already fading
        await UniTask.Delay((int)(punchDuration * 1000), cancellationToken: destroyCancellationToken);
        if (this == null || _dashMat == null) return;
        // All dashes share _dashMat, so fading the material alpha fades the whole ring once.
        await Tween.MaterialAlpha(_dashMat, 0f, fadeOutDuration, Ease.InCubic);
        if (_visualRoot != null) _visualRoot.gameObject.SetActive(false);
    }

    void EnsureCollider()
    {
        var col = GetComponent<CircleCollider2D>();
        col.radius = radius;
        col.isTrigger = true;
    }

    void BuildVisual()
    {
        _dashMat = new Material(Shader.Find("Sprites/Default"));
        var rootGo = new GameObject("DashedCircle");
        rootGo.transform.SetParent(transform, false);
        rootGo.transform.localPosition = Vector3.zero;
        _visualRoot = rootGo.transform;

        float gap = 360f / Mathf.Max(1, dashCount);
        for (int i = 0; i < dashCount; i++)
        {
            float a0 = i * gap;
            float a1 = a0 + dashArcDegrees;
            _dashes.Add(CreateDashArc(rootGo.transform, a0, a1));
        }
    }

    LineRenderer CreateDashArc(Transform parent, float a0Deg, float a1Deg)
    {
        var go = new GameObject("dash");
        go.transform.SetParent(parent, false);
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = _dashMat;
        line.widthMultiplier = dashWidth;
        line.startColor = color;
        line.endColor = color;
        line.numCapVertices = 2;
        line.sortingLayerName = sortingLayer;
        line.useWorldSpace = false; // dashes live in the zone's local space
        line.positionCount = 2;
        line.SetPosition(0, DegToXZ(a0Deg, radius));
        line.SetPosition(1, DegToXZ(a1Deg, radius));
        return line;
    }

    void OnValidate()
    {
        // Keep the collider in sync while editing.
        var col = GetComponent<CircleCollider2D>();
        if (col != null) col.radius = radius;
    }
}
