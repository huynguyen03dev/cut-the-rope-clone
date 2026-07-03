using UnityEngine;

/// <summary>
/// Root of a level prefab instance. Carries a stable GUID string ID so saves key
/// off the level, never off prefab names or catalog indices (DESIGN §3). All
/// mutable gameplay state lives under this transform — restart is destroy +
/// re-instantiate, nothing else resets. The full level-prefab authoring pipeline
/// (LevelCatalog, editor validation pass) lands in US-008; this is the minimal
/// root the session flow needs.
/// </summary>
public sealed class Level : MonoBehaviour
{
    [Tooltip("Stable level identity — saves key off this, never the prefab name.")]
    [SerializeField] string levelId = "level-001";
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

    void Awake() => _driver = GetComponentInChildren<RopeSimulationDriver>();
}