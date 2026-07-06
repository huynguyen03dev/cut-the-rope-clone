# US-006 Bubble (M2a)

## Status

implemented — core buoyancy + tap/swipe classifier + driver/interactor + scene wired;
unit + integration green. E2E (swing-into-bubble playthrough) covered by US-008. See
durable proof via `scripts/bin/harness-cli query matrix`.

## Lane

normal

## Product Contract

A bubble envelops the candy on contact and inverts its gravity (buoyancy) so
it floats upward; tapping the bubble pops it and normal gravity resumes.

## Relevant Product Docs

- `docs/product/gameplay.md` (interactable #2, tap-vs-swipe input rule)
- `DESIGN.md` §2 (per-point gravity flip), §3 (input details)

## Acceptance Criteria

- Bubble pickup detected in the sim step's overlap pass; on contact the
  bubble attaches to the candy and the candy point's gravity flips to
  buoyancy (per-point gravity, not global).
- Tap pops the bubble: tap resolves via the distance+time threshold before
  the pointer can become a cutter — one touch never both pops a bubble and
  cuts a rope.
- Popped bubble restores normal gravity with no energy injection; pop VFX +
  sound hook.
- A bubbled candy still collects stars, wins, and loses per US-002 rules.
- Bubble wobble float visual (feel-pass polish in US-015 may extend it).

## Design Notes

- API: per-point gravity lives in the solver (`Game.Core`); bubble state is
  level-side.
- Domain rules: air cushion puffs push bubbles (US-007 dependency, encode the
  hook now).

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Gravity-flip on attach/pop; tap-vs-swipe threshold classification |
| Integration | Editor: candy enters bubble → floats; tap pops; swipe near bubble cuts rope without popping |
| E2E | Covered by US-008 level playthroughs |
| Platform | Tap-vs-swipe re-verified on phone at M2a exit (thresholds differ on touch) |

## Harness Delta

None expected.

## Evidence

- EditMode: Game.Core.Tests 60/60 passed (Unity 6000.5.1f1, MCP `run_tests` EditMode,
  2026-07-04). New pure-core suites: `BubbleBuoyancyTests` (default scale 1 → candy falls;
  negative scale → rises; full inversion `-1` mirrors gravity displacement exactly; setting
  and restoring the scale inject zero candy velocity — the zero-energy attach/pop rule) and
  `PointerGestureTests` (commit-to-swipe past the distance threshold is strict `>`; a brief
  local release is a tap; far travel or a long hold is not).
- Core (Game.Core, approach B / unit-testable): `RopeSimulation.CandyGravityScale`
  (default 1) multiplies gravity for the candy point ONLY inside `Integrate` — DESIGN §2
  "gravity can be flipped per-point". It scales only future acceleration (never Pos/PrevPos),
  so flipping it mid-swing injects zero energy. `PointerGesture` is a pure tap-vs-swipe
  classifier (`HasCommittedToSwipe` / `IsTap`). `GameEvents.CandyBubbled(Vector2)` +
  `BubblePopped(Vector2)` channels added and cleared in `Rebuild` (US-003 lifecycle).
- Driver/Interactor: `Bubble` ([RequireComponent(CircleCollider2D)]) exposes `BuoyancyScale`
  (default -0.6), `Attach()`/`Pop()`/`Follow()`, single-use `Attached`/`Popped`, procedural
  ring visual + PrimeTween envelop punch → wobble and pop fade. `CandyInteractor.ResolveBubbles`
  runs a `Physics2D.OverlapCircle` grab pass (decision 0008 — explicit sim-step query, no
  trigger callbacks) BEFORE win/lose; on contact it flips `CandyGravityScale` to the bubble's
  buoyancy and raises `CandyBubbled`; the active bubble then follows the candy so a tap lands
  on it while floating. `CandyInteractor.TryPopBubble` (via `RopeSimulationDriver.TryPopBubble`,
  mirroring `CutAt`) restores gravity to 1 and raises `BubblePopped`.
- Input: `SwipeCutter` now classifies each pointer (screen-pixel distance + time). A pointer
  commits to a swipe past `swipeDistancePixels` (default 24) and only then cuts; a sub-threshold
  release within `tapMaxDuration` (0.3 s) is a tap routed to `TryPopBubble`. One touch is
  therefore EITHER a cut OR a pop, never both (gameplay.md input rule #1). UI-occlusion and
  first-frame-no-segment rules preserved.
- Bug fixed en route: the shared `CandyInteractor.CreateContactFilter` used a default
  `ContactFilter2D`, which EXCLUDES trigger colliders — every interactable (star, grab zone,
  bubble) is a trigger, so the filter-based overlaps silently returned nothing. Verified live:
  `OverlapCircle(bubble, defaultFilter)=0` vs `useTriggers=true → 1`, and
  `OverlapPoint(grabZone, defaultFilter)=0`. Added `filter.useTriggers = true`; strictly
  additive on the dedicated per-interactable layers. Also hardens US-002 star and US-005 grab
  detection. Backlog #6 tracks the durable note.
- Integration (live play mode via `execute_code`, candy detached from ropes, from rest):
  baseline normal gravity fell to y≈-1.58 in 20 steps; the sim-step overlap attached the
  bubble (`CandyGravityScale` 1 → -0.6, `CandyBubbled` fired once, candy implied-velocity
  unchanged across attach), candy then floated UP to y≈+0.95 (vy≈+4.4); `TryPopBubble` at the
  candy popped it (`Popped=true`, scale → 1, `BubblePopped` fired once), and the candy fell
  again (vy≈-3.7). Bubble followed the candy each step (follow distance ≈ 0).
- Scene: `Bubble` layer (slot 12) + `Bubble` entity @ (-2.2, 0, 0), radius 0.6, trigger
  CircleCollider2D on the Bubble layer; `CandyInteractor.bubbleMask` = Bubble bit.
- Screenshot: `Assets/Screenshots/us006-bubble.png` (bubble ring at left, candy on ropes,
  star, mouth, and the US-005 grab zone at right).
- Outcome `partial` (E2E swing-into-bubble playthrough deferred to US-008; tap-vs-swipe
  thresholds re-verified on device at M2a exit per the Platform row).
