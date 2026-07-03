# US-003 Game Session, Win/Lose Flow, Restart (M1)

## Status

planned — provisional until US-001 (M0) exit.

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

Add after validation exists.
