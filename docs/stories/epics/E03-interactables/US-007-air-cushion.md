# US-007 Air Cushion (M2a)

## Status

planned — provisional until M1 exit.

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

Add after validation exists.
