# US-006 Bubble (M2a)

## Status

planned — provisional until M1 exit.

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

Add after validation exists.
