# US-007 Air Cushion (M2a)

## Status

implemented — pure-core impulse + cooldown, driver/interactor tap-to-puff path,
scene wired; unit + integration green. E2E (level playthrough) covered by US-008; Platform
(WebGL rebuild + on-device tap/swipe threshold recheck) at M2a exit. See durable proof via
`scripts/bin/harness-cli query matrix`.

## Lane

normal

## Product Contract

A tappable air cushion emits a puff that applies a radial impulse to the
candy (and pushes bubbles), with a short cooldown between puffs.

## Relevant Product Docs

- `docs/product/gameplay.md` (interactable #3)
- `DESIGN.md` §2 (external forces via prevPos adjustment)

## Acceptance Criteria

- Tap on the cushion (same tap-vs-swipe resolution as US-006) triggers a puff.
- Impulse applied through the solver's external-force hook
  (`prevPos -= impulse * dt`, from US-002) with radial falloff; affects the
  candy and bubbled candy; pushes free bubbles.
- Cooldown timer (UniTask, cancellation-safe) blocks retriggering; visual
  state shows ready vs cooling.
- Puff VFX + sound hook; cushion animates on trigger.

## Design Notes

- Domain rules: impulse magnitude/falloff are inspector-tunable (same
  philosophy as the four rope feel numbers).
- UI surfaces: none.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Radial impulse falloff math; cooldown gate |
| Integration | Editor: puff moves hanging candy predictably; cooldown blocks spam |
| E2E | Covered by US-008 level playthroughs |
| Platform | Rebuilt WebGL check at M2a exit |

## Harness Delta

None expected.

## Evidence

- EditMode: 80/80 passed across two assemblies (Unity 6000.5.1f1, MCP `run_tests` EditMode,
  2026-07-06). `Game.Core.Tests` 77 — the 17 new `AirPuffTests` cover radial falloff math (full
  inside inner, linear to zero at max, zero beyond/degenerate-origin/non-positive) and the `PuffCooldown`
  gate (first fire always allowed, blocked within window, opens at ReadyAt exactly, ReadyAt
  advances on re-fire, Remaining/Ready01 track the gate, zero-cooldown always fires).
  Baseline before US-007 was 60; +17 = 77.
- Core (Game.Core, unit-testable): `AirPuff.ComputeImpulse(origin, target, magnitude,
  innerRadius, maxRadius)` returns the radial outward unit × falloff-scaled magnitude,
  full inside inner and linearly to zero at max; clamps a swapped inner>max to a step;
  returns zero when target==origin (radial outward undefined → push nothing, never NaN) or
  magnitude≤0 or maxRadius≤0. `PuffCooldown` struct (ReadyAt, Ready(now),
  Remaining(now), Ready01(now,cd), TryFire(now,cd)) is a pure wall-clock gate. The actual
  PrevPos mutation rides the US-002 external-force hook `RopeSimulation.ApplyCandyImpulse`
  (prevPos -= impulse * dt, a one-frame velocity nudge with no Pos/PrevPos touch beyond).
- Driver/Interactor/Input: `RopeSimulationDriver.TryPuffAirCushion(worldPoint)` forwards to
  `CandyInteractor.TryTapAirCushion` (mirrors `CutAt`/`TryPopBubble` — input goes through the
  driver). The interactor caches `AirCushion[]` in Awake, finds the cushion whose geometric
  tap radius contains the point (decision 0008 — one-shot input query, no trigger callback),
  calls `AirCushion.BeginPuff(now)` (cooldown gate + trigger visual), computes the impulse
  from the cushion config vs the candy's CURRENT sim position, applies it through
  `driver.ApplyAirPuff` → `Sim.ApplyCandyImpulse(impulse, Time.fixedDeltaTime)`, nudges free
  bubbles in range, and raises `GameEvents.AirPuffed(origin)`. `SwipeCutter.End` now routes a
  classified tap: `TryPopBubble` first, then `TryPuffAirCushion` — one touch is EITHER a cut
  OR a pop OR a puff, never more than one (gameplay.md input rule #1 extended).
- MonoBehaviour: `AirCushion` ([RequireComponent(CircleCollider2D)]) owns the cooldown state
  + config (radius, puffMagnitude, innerRadius, maxRadius, cooldownSeconds) + a dashed-circle
  LineRenderer visual whose shared material color lerps ready→cooling via `Ready01` each
  frame, plus a PrimeTween outward punch on puff. It does NOT mutate the solver — the
  interactor owns the candy state so the event bus has one place (mirrors Bubble/AutoGrabZone).
- Bug fixed during integration: `PuffCooldown Cooldown { get; private set; }` is a struct
  auto-property, so `Cooldown.TryFire(...)` mutated a throwaway getter copy and ReadyAt never
  persisted — the cooldown never blocked and every tap fired. `BeginPuff` now reads a local,
  mutates, assigns back. (Unit tests use a local `var cd = new PuffCooldown()` so they passed;
  the live play-mode integration caught this — exactly what integration is for.) No backlog
  item: a one-line MonoBehaviour-only fix, not a recurring pattern.

  Regression guard (follow-up to the code review): the bug was the one thing the pure-core
  `AirPuffTests` could NOT catch — they construct `var cd = new PuffCooldown()` (a local), so they
  stay green even if `BeginPuff` regresses to the buggy `Cooldown.TryFire(...)` getter-copy form.
  To durably guard the MonoBehaviour path, the gameplay MonoBehaviours were wrapped in a new
  `Game.Gameplay` runtime asmdef (Assets/_Project/Scripts/Gameplay/Game.Gameplay.asmdef; refs
  Game.Core, PrimeTween.Runtime, Unity.InputSystem, UniTask, UnityEngine.UI; all platforms) and a
  `Game.Gameplay.Tests` EditMode asmdef was added (Assets/_Project/Tests/Gameplay/). The new
  `AirCushionCooldownTests` constructs an `AirCushion` on an INACTIVE GameObject (Awake — the
  LineRenderer ring + PrimeTween setup — is deferred) so the cooldown contract is exercised in
  isolation, sets `cooldownSeconds` via reflection for a deterministic `ReadyAt`, and asserts:
  first tap fires + ReadyAt persists; a same-instant re-tap is blocked; re-fire at exactly ReadyAt
  opens. Mutation-verified: reverting `BeginPuff` to the buggy form makes all 3 tests fail with the
  exact bug signatures (ReadyAt=0.0, re-tap returns True); restoring the fix → 80/80 green.
- Integration (live play mode via `execute_code`, candy hanging from ropes at rest):
  - tap1 inside the cushion (`AirCushion` @ (1.8,-1.2,0), radius 0.7, mag 6, inner 0.5, max
    3.5) fired; candy velocity went 0 → (-1.56, 1.43) — pushed up-left, radially AWAY from
    the cushion (candy at distance ~2.43, falloff scale ~0.357, impulse ~2.14 along unit
    (-0.74, 0.68) = (-1.59, 1.45) ✓). The external-force hook nudged PrevPos only; Pos was
    untouched (zero-energy one-frame velocity change).
  - tap2 on the SAME frame returned `fired=False` → cooldown BLOCKED (the bug fix in action).
  - Scene: `AirCushion` layer (slot 13) added to TagManager; `AirCushion` entity @
    (1.8,-1.2,0) on that layer; `CandyInteractor.cushionMask` = 1<<13 = 8192 (alongside
    bubbleMask=4096, grabZoneMask=2048).
- Free-bubble push (gameplay.md interactable #3): implemented — the interactor overlaps the
  bubble layer within the puff's maxRadius and translates any free (not attached, not popped)
  bubble by the falloff-scaled impulse × dt. The candy's active bubble is skipped (it follows
  the candy and already received the impulse, so nudging it again would double-apply). Not
  exercised live: the scene's only Bubble @ (-2.2,0) is ~4.18 from the cushion (out of the
  3.5 reach), and it is not attached until the candy overlaps it. US-008 level
  playthroughs will exercise it.
- Screenshot: `Assets/Screenshots/us007-air-cushion.png` (cushion dashed ring lower-right of
  the candy, with a faint max-radius reach ring).
- Outcome `partial` (E2E level playthrough deferred to US-008; Platform WebGL rebuild +
  on-device tap/swipe threshold recheck at M2a exit per the Platform row).
- Durable command: `scripts/run-editmode-tests.sh` (tool unity-editmode-tests).