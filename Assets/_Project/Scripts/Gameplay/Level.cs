using UnityEngine;

/// <summary>
/// Root of a level prefab instance. Carries a stable GUID string ID so saves key
/// off the level, never off prefab names or catalog indices (DESIGN §3). All
/// mutable gameplay state lives under this transform — restart is destroy +
/// re-instantiate, nothing else resets. The level-prefab authoring pipeline
/// (LevelCatalog, editor validation pass) lands in US-008; this is the root the
/// session flow and catalog key off.
///
/// The GUID is auto-generated once (on AddComponent / inspector validate) and then
/// never changed by code: saves (US-012) key off <see cref="Id"/>, so reusing or
/// regenerating it breaks persisted progress. Designers must not hand-edit it.
/// </summary>
public sealed class Level : MonoBehaviour
{
    [Tooltip("Stable level identity — auto-generated GUID. Saves key off this; never edit or reuse (reuse breaks saves).")]
    [SerializeField] string levelId = "";
    [Tooltip("Par time (seconds) for the time-bonus; under par earns bonus points.")]
    [SerializeField] float parTime = 12f;
    [Tooltip("Total stars this level awards; the HUD counter maxes at this.")]
    [SerializeField] int totalStars = 1;

    /// <summary>Stable identity — never reorder or repurpose; reuse breaks saves.</summary>
    public string Id => levelId;
    public float ParTime => parTime;
    public int TotalStars => totalStars;

    /// <summary>The sim driver living somewhere under this level instance. Resolved lazily
    /// so a driver reparented after AddComponent&lt;Level&gt; is still found on first access —
    /// Awake runs synchronously at AddComponent time and may run before the reparent.</summary>
    public RopeSimulationDriver Driver
    {
        get { return _driver ??= GetComponentInChildren<RopeSimulationDriver>(); }
        private set { _driver = value; }
    }
    RopeSimulationDriver _driver;

    void Awake()
    {
        _driver = GetComponentInChildren<RopeSimulationDriver>();
#if !UNITY_EDITOR
        // Runtime fallback: a Level created via AddComponent (e.g. the gray-box scene wrapper)
        // never went through the editor auto-gen, so give it a transient non-persisted id.
        // Authored level prefabs always carry a real serialized GUID from the editor path.
        if (string.IsNullOrEmpty(levelId)) levelId = System.Guid.NewGuid().ToString("N");
#endif
    }

#if UNITY_EDITOR
    // Auto-generate the GUID exactly once. Reset fires when the component is added; OnValidate
    // covers prefab creation paths. Both no-op once an id exists, so the GUID is stable forever.
    void Reset() => EnsureLevelId();
    void OnValidate() => EnsureLevelId();

    void EnsureLevelId()
    {
        if (string.IsNullOrEmpty(levelId))
        {
            levelId = System.Guid.NewGuid().ToString("N");
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    void OnDrawGizmos()
    {
        Vector3 p = transform.position;
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(p, 0.2f);
        string label = string.IsNullOrEmpty(levelId)
            ? "Level (no id)"
            : "Level " + levelId.Substring(0, System.Math.Min(8, levelId.Length));
        UnityEditor.Handles.Label(p + Vector3.up * 0.35f, label);
    }
#endif
}
