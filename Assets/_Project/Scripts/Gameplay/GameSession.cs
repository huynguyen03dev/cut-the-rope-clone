using System;
using Cysharp.Threading.Tasks;
using Game.Core;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owns the per-level game flow (DESIGN §3 / docs/product/progression.md):
/// Loading → Intro (camera pan) → Playing → Won | Lost → (restart/next).
///
/// Hard rules honored here:
/// - The <see cref="GameEvents"/> bus is instance-owned and rebuilt per level — never
///   static — so subscribers on destroyed level objects cannot leak across restarts.
/// - Restart = destroy + re-instantiate the level prefab. All mutable state lives inside
///   that instance; nothing else resets. Two consecutive restarts leak no subscribers
///   and throw nothing (US-003 acceptance criterion).
/// - Every async method takes <see cref="UniTask.destroyCancellationToken"/>
///   (<c>MonoBehaviour.destroyCancellationToken</c>), so a level restart cancels all
///   in-flight sequences. Zero <c>IEnumerator</c> coroutines.
/// </summary>
public sealed class GameSession : MonoBehaviour
{
    [Header("Level")]
    [Tooltip("The level prefab. If left null the session adopts a Level already in the scene (gray-box / direct-Play fallback).")]
    [SerializeField] Level levelPrefab;
    [Tooltip("Where the spawned level instance is parented. May be this transform.")]
    [SerializeField] Transform levelContainer;
    [Tooltip("Level catalog (US-008). When levelPrefab is null but loadLevelId is set, the level is resolved from here for play-testing. The box/level-select menu (US-013) replaces this.")]
    [SerializeField] LevelCatalog catalog;
    [Tooltip("Play-test hook: load this level id from the catalog on start. The menu (US-013) will replace this.")]
    [SerializeField] string loadLevelId;

    [Header("Camera intro")]
    [SerializeField] Camera mainCamera;
    [Tooltip("Intro pan: camera starts offset by this many world units from its rest position and eases back.")]
    [SerializeField] float introPanOffset = 4f;
    [SerializeField] float introPanDuration = 1.2f;
    [Tooltip("Easing for the intro pan (PrimeTween).")]
    [SerializeField] Ease introEase = Ease.OutCubic;
    [Tooltip("Seconds the win/lose overlay waits before allowing a restart.")]
    [SerializeField] float beatHoldDuration = 0.8f;

    [Header("HUD")]
    [SerializeField] HudController hud;

    Level _levelInstance;
    bool _instanceIsSpawned; // true when the session Instantiated it (prefab/catalog) → destroy on restart; false when adopted from the scene (gray-box)
    GameEvents _events;
    SessionState _state = SessionState.Loading;

    int _starsCollected;
    float _elapsed;     // accumulated only while Playing
    float _winElapsed; // frozen at the moment of win

    public SessionState State => _state;
    public Level ActiveLevel => _levelInstance;

    async UniTaskVoid Start()
    {
        _events = new GameEvents();
        // Lazy fallback (DESIGN §3: pressing Play in the Game scene works without Boot).
        if (mainCamera == null) mainCamera = Camera.main;
        if (hud == null) hud = FindAnyObjectByType<HudController>(FindObjectsInactive.Include);
        await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, destroyCancellationToken);
        // Loading → Intro → Playing handled inside BeginLevel().
        await BeginLevel();
    }

    void Update()
    {
        if (_state == SessionState.Playing) _elapsed += Time.deltaTime;
        if (_state == SessionState.Won || _state == SessionState.Lost)
        {
            // New Input System only — the project disables the legacy Input class.
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) RequestRestart();
        }
    }

    /// <summary>Public entry for the HUD restart button.</summary>
    public void RequestRestart() => RestartFlow().Forget();

    // ── per-level lifecycle ──────────────────────────────────────────────

    async UniTask BeginLevel()
    {
        // destroy any prior instance and rebuild the event bus (restart rule).
        DestroyLevelInstance();
        _events.Rebuild();
        _starsCollected = 0;
        _elapsed = 0f;

        _levelInstance = InstantiateLevel();
        if (_levelInstance == null)
        {
            // No level prefab and no Level in scene: nothing to play (M3 catalog wires prefabs).
            return;
        }

        RopeSimulationDriver driver = _levelInstance.Driver ?? FindAnyObjectByType<RopeSimulationDriver>();
        if (driver != null) driver.UseEvents(_events); // bind the live per-level bus

        SubscribeEvents();
        if (hud != null)
        {
            hud.Bind(_events, _levelInstance.TotalStars);
            hud.WireRestartButton(RequestRestart);
        }

        TransitionTo(SessionEvent.Loaded); // Loading → Intro
        await PlayIntroPan(destroyCancellationToken); // camera pan resolves (awaitable tween)
        TransitionTo(SessionEvent.IntroDone);          // Intro → Playing
        if (hud != null) hud.OnIntroComplete();
    }

    /// <summary>Subscribes the session's flow handlers to the current per-level bus.
    /// Re-subscribed after every <see cref="GameEvents.Rebuild"/> so handlers don't double up.</summary>
    void SubscribeEvents()
    {
        _events.StarCollected += OnStarCollected;
        _events.CandyEaten += OnCandyEaten; // win
        _events.CandyLost += OnCandyLost;   // lose
    }

    // ── driver outcomes (raised by CandyInteractor at end of sim step) ──

    void OnStarCollected(GameObject star)
    {
        if (_state != SessionState.Playing) return;
        _starsCollected++;
        if (hud != null) hud.SetStarCount(_starsCollected);
    }

    void OnCandyEaten()
    {
        if (_state != SessionState.Playing) return;
        _winElapsed = _elapsed;
        TransitionTo(SessionEvent.Win);
        WinFlow().Forget();
    }

    void OnCandyLost()
    {
        // Only a Playing→Lost transition starts the lose beat (mirrors OnCandyEaten). A
        // repeat CandyLost while already resolved is ignored, so the overlay can't double-fire.
        if (_state != SessionState.Playing) return;
        TransitionTo(SessionEvent.Lose);
        if (_state == SessionState.Lost) LoseFlow().Forget();
    }

    async UniTaskVoid WinFlow()
    {
        int score = LevelScoring.Compute(_starsCollected, _winElapsed, _levelInstance.ParTime);
        var result = new LevelResult(_starsCollected, score, _winElapsed);
        _events.RaiseLevelCompleted(result); // US-013 consumes, US-012 persists
        if (hud != null) await hud.ShowWinAsync(result, destroyCancellationToken);
        else await UniTask.Delay(TimeSpan.FromSeconds(beatHoldDuration), cancellationToken: destroyCancellationToken);
    }

    async UniTaskVoid LoseFlow()
    {
        if (hud != null) await hud.ShowLoseAsync(destroyCancellationToken);
        else await UniTask.Delay(TimeSpan.FromSeconds(beatHoldDuration), cancellationToken: destroyCancellationToken);
    }

    async UniTask RestartFlow()
    {
        // Cancel every in-flight tween — a restart boundary: the level is destroyed and
        // HUD overlays reset, so beat/intro tweens must not resume against destroyed targets.
        Tween.StopAll();
        if (hud != null) hud.HideOverlays();
        TransitionTo(SessionEvent.Restart); // Won/Lost → Loading
        await BeginLevel();
    }

    // ── intro pan (awaitable PrimeTween) ──────────────────────────────────

    async UniTask PlayIntroPan(System.Threading.CancellationToken ct)
    {
        if (mainCamera == null) return;
        Transform t = mainCamera.transform;
        Vector3 rest = t.position;
        Vector3 start = rest + Vector3.up * introPanOffset;
        t.position = start;
        // PrimeTween tweens are directly awaitable; cancellation stops the tween via the
        // registered callback, which the awaiter observes as a normal completion.
        using var reg = ct.Register(() => Tween.StopAll(t));
        await Tween.Position(t, rest, introPanDuration, introEase);
        t.position = rest;
    }

    void TransitionTo(SessionEvent ev) => _state = SessionStateMachine.Transition(_state, ev);

    // ── level instantiation / teardown ────────────────────────────────────

    Level InstantiateLevel()
    {
        Transform parent = levelContainer != null ? levelContainer : transform;

        // Play-test hook (US-008): resolve a level prefab from the catalog by id. The box/level
        // menu (US-013) replaces this direct id field.
        if (levelPrefab == null && catalog != null && !string.IsNullOrEmpty(loadLevelId)
            && catalog.TryGetLevel(loadLevelId, out Level fromCatalog))
        {
            _instanceIsSpawned = true;
            return Instantiate(fromCatalog, parent);
        }

        if (levelPrefab != null)
        {
            _instanceIsSpawned = true;
            return Instantiate(levelPrefab, parent);
        }
        // Direct-Play fallback: an editor-placed Level already in the scene.
        Level existing = FindAnyObjectByType<Level>();
        if (existing != null)
        {
            _instanceIsSpawned = false; // adopted, not spawned — leave intact on restart
            existing.transform.SetParent(parent, true);
            return existing;
        }
        // Gray-box fallback (pre-US-008): wrap the existing scene entities into a Level
        // at runtime so the session flow still drives them. The prefab path replaces this.
        return WrapExistingSceneAsLevel();
    }

    void DestroyLevelInstance()
    {
        // Only instances the session spawned (prefab / catalog) are destroyed on restart;
        // an adopted in-scene Level and the gray-box wrapper are left intact.
        if (_levelInstance != null && _instanceIsSpawned)
        {
            Destroy(_levelInstance.gameObject);
        }
        _levelInstance = null;
        _instanceIsSpawned = false;
    }

    Level WrapExistingSceneAsLevel()
    {
        // In the gray-box Game scene there is no Level root; create one and reparent the
        // loose gameplay entities so the session can destroy+recreate on restart. The
        // real US-008 level prefab supersedes this.
        var go = new GameObject("Level (graybox)");
        go.transform.SetParent(levelContainer != null ? levelContainer : transform, false);
        // Reparent the gameplay entities BEFORE adding the Level component: Level.Awake
        // resolves its driver via GetComponentInChildren, so the driver must already be a
        // child at AddComponent time.
        RopeSimulationDriver driver = FindAnyObjectByType<RopeSimulationDriver>();
        if (driver != null) driver.transform.SetParent(go.transform, true);
        CandyFollower candy = FindAnyObjectByType<CandyFollower>();
        if (candy != null) candy.transform.SetParent(go.transform, true);
        Playfield playfield = FindAnyObjectByType<Playfield>();
        if (playfield != null) playfield.transform.SetParent(go.transform, true);
        _instanceIsSpawned = false; // gray-box wrapper: not destroyed on restart (replaced by prefabs)
        var level = go.AddComponent<Level>();
        // Stars, the mouth, and hazards are picked up by CandyInteractor's physics
        // queries on their own layers; they stay in-scene for the gray box and are
        // authored into the level prefab by US-008.
        return level;
    }

    void OnDestroy()
    {
        Tween.StopAll();
        DestroyLevelInstance();
    }
}