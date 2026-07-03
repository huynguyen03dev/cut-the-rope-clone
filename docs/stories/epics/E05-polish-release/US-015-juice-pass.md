# US-015 Juice Pass (M4)

## Status

planned — provisional until M3 exit.

## Lane

normal

## Product Contract

The game reads as polished: every item on the game-feel checklist in
`docs/product/gameplay.md` is implemented. This is what gets the link
forwarded to a hiring manager.

## Relevant Product Docs

- `docs/product/gameplay.md` (game feel checklist — the contract list)
- `DESIGN.md` §5

## Acceptance Criteria

- Candy: squash-and-stretch from speed delta + swing rotation from
  velocity/rope direction (completing the US-002 basics).
- Om Nom: eyes track the candy; mouth opens when candy is near; eat
  animation + gulp on win; sad reaction on lose.
- Rope cut: snap sound, particle burst at the cut point, 2-frame hitstop.
- Star collect: scale punch + sparkle (pitch ramp already in US-014).
- Bubble: wobble float and satisfying pop.
- Per-finger swipe slice VFX: each active pointer draws its own
  `TrailRenderer` trail (multi-touch cutting itself ships in US-001).
- All tweens are PrimeTween, lifecycle-linked (killed on owner destroy);
  restart mid-animation never throws or leaks.
- No per-frame allocations added — WebGL frame-time spot-check before/after.

## Design Notes

- Pool the cut VFX only if profiling shows it matters (no generic pool
  framework — written non-goal).
- UI surfaces: none new; this story animates existing ones.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | n/a (visual work) |
| Integration | Restart-during-every-animation stress check |
| E2E | Feel checklist walked item by item, checked off in Evidence |
| Platform | Rebuilt WebGL build; frame-time spot-check on phone |

## Harness Delta

None expected.

## Evidence

Checklist walkthrough + before/after GIFs.
