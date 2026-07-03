# US-011 Two Boxes, 10–12 Total Levels (M2b exit)

## Status

planned — provisional until M2a exit.

## Lane

normal

## Product Contract

The full level set exists: 10–12 levels across 2 boxes in the
`LevelCatalog`, with star-unlock thresholds set, using the complete
interactable set (auto-grab, bubble, air cushion, moving anchors, spikes).

## Relevant Product Docs

- `docs/product/progression.md` (boxes, thresholds, catalog)
- `docs/product/gameplay.md` (interactables)

## Acceptance Criteria

- 10–12 levels total, organized as 2 boxes in the `LevelCatalog`; box 2's
  star-unlock threshold set and tuned (reachable without perfecting box 1).
- Difficulty curve: box 1 teaches one mechanic per level; box 2 combines
  mechanics; at least 2 levels use moving anchors, at least 2 use spikes.
- Every level passes the editor validation pass and is completable with
  3 stars (documented per level).
- Time-bonus scoring parameters set per level.
- No level exceeds a comfortable phone session (~2 min once understood).

## Design Notes

- Content-heavy story: the proof is the play-test table in Evidence, not
  code.
- Spider interactable remains stretch — explicitly out of scope here.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Catalog/threshold lookups |
| Integration | Validation pass green across all levels |
| E2E | Manual: every level 3-starred; per-level notes table |
| Platform | Rebuilt WebGL build with both boxes, phone-tested (M2b exit) |

## Harness Delta

If per-level play-test tracking gets tedious, propose a checklist template
via `backlog add`.

## Evidence

Per-level play-test table goes here.
