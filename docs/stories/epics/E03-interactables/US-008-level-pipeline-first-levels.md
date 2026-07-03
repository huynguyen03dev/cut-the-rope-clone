# US-008 Level Authoring Pipeline + First ~6 Levels (M2a exit — shippable cut line)

## Status

planned — provisional until M1 exit.

## Lane

normal

## Product Contract

Levels are authorable as prefabs with editor validation, and ~6 real levels
exist using ropes, stars, auto-grab, bubbles, and air cushions. With this
story done the game is shippable (M2a is the cut line).

## Relevant Product Docs

- `docs/product/progression.md` (level content model)
- `docs/product/gameplay.md` (interactables)

## Acceptance Criteria

- `RopeAuthoring` component: anchor transform, target (candy or free end),
  rest length, segment count (~10–16); bakes into solver points at load.
- `Level` root component carries a stable GUID string ID (auto-generated,
  never hand-edited); custom gizmos for anchors, ropes, zones, mouth.
- Editor validation pass: every rope has an anchor, exactly one candy,
  exactly one Om Nom, unique level IDs across the catalog — failures are
  loud (console errors or a validation window).
- `LevelCatalog` ScriptableObject: boxes → level prefab references +
  star-unlock thresholds (one box populated for now).
- ~6 levels of graduated difficulty, each completable with 3 stars,
  exercising every M2a interactable at least twice across the set.
- Each level manually play-tested: 3-star path, win, both lose paths where
  applicable.

## Design Notes

- Domain rules: saves will key off the GUID (US-012) — the GUID contract
  starts here and must never change.
- Timebox (senior review finding #10): this is a deliberately large M2a-exit
  slice; if pipeline + content overruns, split into two stories (pipeline
  first, levels second) rather than letting the cut line slip.
- UI surfaces: none new; editor tooling lives in `Editor/`.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | GUID stability; catalog lookup |
| Integration | Validation pass catches each seeded authoring error (test scene or editor test) |
| E2E | Manual: all ~6 levels completed at 3 stars; notes per level |
| Platform | Rebuilt WebGL build with all levels, phone-checked (M2a exit) |

## Harness Delta

Editor validation could become a batch-mode command — if so, register it as a
harness tool and wire it into this story's `--verify`.

## Evidence

Add after validation exists.
