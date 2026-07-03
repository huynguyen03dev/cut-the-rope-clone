# US-005 Auto-Grab Rope (M2a)

## Status

planned — provisional until M1 exit.

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

- Grab radius detected via the sim step's `OverlapCircle` pass (US-002
  infrastructure), not trigger callbacks.
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

Add after validation exists.
