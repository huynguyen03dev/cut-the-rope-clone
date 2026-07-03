# US-010 Spikes, Static and Moving (M2b)

## Status

planned — provisional until M2a exit.

## Lane

normal

## Product Contract

Spikes destroy the candy on contact (lose condition #2). Static spikes
first, then moving/rotating variants. The candy can never tunnel through
spikes at any speed.

## Relevant Product Docs

- `docs/product/gameplay.md` (lose conditions, interactable #5)
- `DESIGN.md` §2 (swept CircleCast), §8 (tunneling risk)

## Acceptance Criteria

- Spike contact detected by the swept `Physics2D.CircleCast` from the candy's
  previous step position to its current position against the hazard layer
  (infrastructure from US-002) — a fling at maximum plausible speed cannot
  pass through.
- Contact resolves in the priority pass: win still beats lose in the same
  step.
- Candy-destroyed beat: burst VFX, Om Nom sad reaction hook, `CandyLost`
  event → Lost state.
- Moving/rotating spikes update their colliders inside the fixed step (same
  rule as moving anchors) so sweeps test against true positions.
- Authoring + validation-pass support; spikes visually unambiguous.

## Design Notes

- Domain rules: the sweep, not collider thickness, is the anti-tunneling
  mechanism — never "fix" tunneling by fattening colliders.
- UI surfaces: none.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Sweep hit at high speed (simulated large per-step displacement); win-beats-lose with spike+mouth same step |
| Integration | Editor: drop/fling candy onto static and moving spikes → always lose, never pass through |
| E2E | Covered by US-011 level playthroughs |
| Platform | Rebuilt WebGL check at M2b exit |

## Harness Delta

None expected.

## Evidence

Add after validation exists.
