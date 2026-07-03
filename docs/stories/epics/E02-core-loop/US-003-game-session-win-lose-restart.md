# US-003 Game Session, Win/Lose Flow, Restart (M1)

## Status

in_progress — implementation landed; unit + integration green; e2e manual full-loop
GIF pending (needs a human swipe cut through a win). See durable proof via
`scripts/bin/harness-cli query matrix`.

## Lane

normal

## Product Contract

One full gray-box level is playable end to end: intro camera pan, play, win
by feeding Om Nom or lose, then restart or continue. Restarting is always
clean because all mutable state lives in the level prefab instance.

## Relevant Product Docs

- `docs/product/progression.md` (scene flow, state machine, restart rule)
- `docs/product/gameplay.md` (win/lose, priority rules)
- `DESIGN.md` §3 (events & lifecycle, async rules)

## Acceptance Criteria

- Plain-C# `GameSession` state machine: `Loading → Intro → Playing → Won |
  Lost → (restart/next)`; transitions driven by the US-002 events.
- Events object is instance-owned by `GameSession`, rebuilt per level — not
  static. Restart = destroy + re-instantiate the level prefab; nothing else
  resets. Two consecutive restarts leak no subscribers and throw nothing.
- Minimal `Level` root component and a first level prefab (full authoring
  pipeline comes in US-008).
- Om Nom placeholder with a mouth zone; eat = win (charm animations are
  US-015).
- Minimal HUD: star counter, win/lose overlay, restart button. A swipe
  crossing HUD buttons must not cut ropes (per-pointer UI occlusion).
- UniTask + PrimeTween installed; intro pan and win/lose beats are UniTask
  sequences; every async method takes `destroyCancellationToken`; zero
  `IEnumerator` coroutines in the repo.
- Boot scene composes services; pressing Play directly in the Game scene
  still works via the lazy-init fallback.
- Cutting aftermath completed (deferred from US-001): candy-side stubs
  retract/fade over ~0.5 s; a doubly-cut free middle piece despawns
  offscreen or on the stub timer.
- Level timer and time-bonus scoring: score = stars + time bonus, computed
  at win and shown on the win overlay (best-score persistence is US-012,
  decision 0009).
- `LevelCompleted` raised on the Won transition with stars + score payload
  (US-013's next-level flow consumes it).

## Design Notes

- API: `GameSession` and event channel types are plain C#; only thin
  MonoBehaviour drivers touch the scene.
- Domain rules: win beats lose is already resolved in US-002's query pass;
  the session just consumes ordered events.
- UI surfaces: HUD canvas, win/lose overlay.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | State machine transition table; events-rebuilt-per-level test; time-bonus math |
| Integration | Editor: play → win → restart → lose → restart with no errors/leaks |
| E2E | Manual full-loop playthrough recorded as GIF |
| Platform | n/a (US-004) |

## Harness Delta

UniTask/PrimeTween arrive as packages — record versions in the packet
evidence when installed.

## Evidence

- EditMode: Game.Core.Tests 43/43 passed (Unity 6000.5.1f1, MCP `run_tests EditMode`,
  2026-07-03). New suites: `SessionStateMachineTests`, `GameEventsLifecycleTests`
  (two-restart no-leak/no-throw), `LevelScoringTests`; updated `RopeCutTests` with
  candy-stub fade despawn + free-piece guard.
- Integration (mechanical, live play mode via MCP `execute_code`): `Playing`
  → `RaiseCandyEaten` → `Won`; `RequestRestart` → `Loading`→`Intro`→`Playing`;
  `RaiseCandyLost` → `Lost`; second win + restart repeated with no errors/leaks
  thrown. `driver.Events` confirmed bound to the per-level session bus.
- Packages: UniTask 2.5.11 (`com.cysharp.unitask`), PrimeTween
  (`com.kyrylokuzyk.primetween` via OpenUPM scoped registry).
- Cut aftermath (deferred from US-001): candy-side stub stays attached and swinging,
  fades and despawns after `StubFadeDuration`; free-floating piece despawns via
  `DetectFreePieces`. The retract/shrink visuals are render-time (RopeRenderer, US-015).
- Screenshots: `Assets/Screenshots/us003-session-hud.png`, `us003-win-overlay.png`
  ("LEVEL CLEAR!" + score + restart button, full-screen dim), `us003-lose-overlay.png`
  ("CANDY LOST! Tap Restart").
- HUD fix (post-integration): the gray-box HUD had three bugs — the RestartButton
  was a default white 100x100 Image left active dead-center during play (appeared as a
  persistent white square), WinOverlay/LoseOverlay were tiny squares not full-screen dim
  panels, and winScoreText resolved null (`transform.Find` misses nested children). Fixed
  via recursive `FindDeep`, `SetRestartVisible` (hidden during play, shown after overlay
  fade-in), composed titles, portrait CanvasScaler, full-screen 75%-black dim overlays,
  repositioned/labeled button + counter. Verified in play mode: gameplay shows only the
  star counter (no white square), win/lose overlays render correctly, restart click returns
  to Playing, zero console errors. Harness interventions #2 (input-system legacy fix) and
  #3 (HUD fix).
- Scope (gray-box accepted): no real Level prefab yet — `WrapExistingSceneAsLevel`
  reparents scene entities at runtime; `Boot.unity` is an empty stub. Both are owned by
  US-008 (prefab pipeline) and US-013 (Boot/scene composition) and tracked there.
- Trace #5; outcome `partial` (e2e manual GIF intentionally deferred).
