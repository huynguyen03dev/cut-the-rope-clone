# US-005 Auto-Grab Rope (M2a)

## Status

implemented — core layer + driver + scene wired; unit + integration green. E2E
(swing-into-zone playthrough) covered by US-008. See durable proof via
`scripts/bin/harness-cli query matrix`.

## Lane

normal

## Product Contract

A dashed-circle auto-grab zone: when the candy enters the radius, a new rope
instantly attaches from the zone's anchor to the candy, with zero energy
injected — no yank on grab.

## Relevant Product Docs

- `docs/product/gameplay.md` (interactable #1, zero-energy rule)
- `DESIGN.md` §2 (auto-grab attach)

## Acceptance Criteria

- Grab detected via a sim-step `OverlapPoint` pass (US-002 infrastructure) —
  the grab fires when the candy *center* enters the zone radius — not via
  trigger callbacks.
- On grab: new rope's `restLength` = anchor→candy distance at grab time (not
  the trigger radius); every spawned point initializes `prevPos = pos`.
  Result: candy velocity is visually unchanged at the attach frame.
- The new rope is cuttable and behaves identically to authored ropes
  (multiple ropes share the candy terminal point — free by design).
- A zone grabs once; it re-arms only if the level defines it that way
  (default: single-use, matching the original).
- Dashed-circle visual with a subtle attach effect.

## Design Notes

- Domain rules: zero-energy attach is the contract — a test should assert
  candy speed delta ≈ 0 across the attach step.
- UI surfaces: none.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Attach math: restLength from distance, spawned points at rest, speed delta ≈ 0 |
| Integration | Editor: swing candy through zone → rope attaches, is cuttable |
| E2E | Covered by US-008 level playthroughs |
| Platform | Rebuilt WebGL check at M2a exit |

## Harness Delta

None expected.

## Evidence

- EditMode: Game.Core.Tests 48/48 passed (Unity 6000.5.1f1, MCP `run_tests EditMode`,
  2026-07-03). New suite `AutoGrabAttachTests` pins the zero-energy attach contract:
  rest length == anchor→candy distance / segments; every spawned point PrevPos == Pos;
  candy implied-velocity unchanged across `AddRope`; new rope is `AttachedToCandy &&
  Cuttable`; multiple ropes share the candy terminal point.
- Core: `GameEvents.RopeAttached(Vector2 anchorPos)` channel added (+ cleared in
  `Rebuild`, consistent with `RopeCut`). `RopeSimulation.AddRope` already implemented
  the zero-energy rule (`RopePoint.At` sets PrevPos = Pos; restLength = distance/segments).
- Driver: `AutoGrabZone` ([RequireComponent(CircleCollider2D)]) — exposes
  `AnchorPos`/`Segments`; `Consume()` + `Used` (single-use default) and
  `RearmIfMultiUse()` for re-arming zones; dashed-circle visual (24 procedural
  LineRenderer arc segments, shared material, `MaterialAlpha` fade on spend);
  PrimeTween attach pulse. NOT a `RopeAnchor` under the driver (avoids load-time spawn).
- Interactor: `CandyInteractor.ResolveStep` runs a `Physics2D.OverlapPointNonAlloc`
  grab pass (decision 0008 — explicit sim-step query, no trigger callbacks) BEFORE
  win/lose resolution; zones cached in `Awake` (rebuild every restart). `DebugEventLogger`
  logs `RopeAttached`.
- Scene: `GrabZone` layer (slot 11) + `AutoGrabZone` entity @ (2.5, 0, 0), radius 1.2,
  CircleCollider2D trigger on GrabZone; `CandyInteractor.grabZoneMask` = GrabZone bit.
- Integration (live play mode via `execute_code`): positioning the zone on the candy
  → next FixedUpdate `ResolveStep` attached a rope (ropes 2→3, `zone.Used=True`);
  candy velocity delta ≈ 0 (zero-energy); new rope `attached && cuttable`; `CutAt`
  through the grabbed rope returned `True` and detached the candy from it. Single-use
  zone did not re-grab after consumption.
- Screenshot: `Assets/Screenshots/us005-auto-grab-zone.png` (dashed cyan circle visible
  at the zone on the right side of the gray-box scene).
- Outcome `partial` (e2e swing-into-zone deferred to US-008).
