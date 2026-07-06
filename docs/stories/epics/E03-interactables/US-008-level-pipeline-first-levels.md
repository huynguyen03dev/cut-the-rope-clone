# US-008 Level Authoring Pipeline + First ~6 Levels (M2a exit — shippable cut line)

## Status

in_progress — authoring pipeline + 6 levels implemented and unit/integration-verified.
Manual 3-star E2E play-throughs and the M2a-exit WebGL rebuild remain.

## Lane

normal

## Decisions (recorded at intake #8)

- **Free-end ropes deferred.** `RopeAuthoring` author anchor→candy ropes only (every
  M2a gameplay rope is anchor→candy; the solver is candy-centric). A forward-compatible
  `RopeTarget` enum keeps the door open for a decorative/hanging target in a later story
  without re-authoring levels.
- **Pipeline-first, one story.** Per the senior-review split clause: pipeline + validation +
  catalog landed first, then the gray-box scene was converted into `Level_01` as the
  checkpoint. The cut line held, so no split was needed; levels 02–06 followed.
- **Validation = pure static `LevelValidator` + EditMode tests** (matches the US-007 pattern).
  The API is editor-dep-free so a future batch CLI / editor menu can wrap it (the harness-delta
  option is still open).

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

### Unit (EditMode, MCP `run_tests`, Unity 6000.5.1f1)

**93/93 passed** (baseline 80 from prior stories + 13 new):
- `VerletSolverTests` +3: `AddRope(anchorPos, segments, restLengthPerSegment)` overload honors
  per-segment rest; ≤0 falls back to distance/segments; 2-arg path still = distance/segments.
- `LevelValidatorTests` +10: valid level passes; missing candy / missing OmNom / two OmNom /
  no ropes / empty id / missing driver are each flagged; catalog duplicate IDs flagged; catalog
  null entry flagged; valid catalog passes.

**GUID stability:** `Level` auto-generates a stable GUID on `Reset`/`OnValidate` (editor-guarded,
  never regenerated once set). Duplicating `Level_01` and forcing a new GUID per level produced
  6 unique IDs across the catalog (`ValidateCatalog` → Ok). The GUID contract for US-012 starts here.

### Integration

- **Validation pass catches each seeded authoring error** — the 10 `LevelValidatorTests`.
- **Catalog → session load** (MCP play-mode): `GameSession` resolves the level by `loadLevelId`
  from the `LevelCatalog` asset and instantiates it (no more gray-box wrapping).
- **`Level_01` end-to-end** (committed `b2f2bb8`): `Level_01(Clone)` loads → state `Playing` →
  2 ropes from `RopeAuthoring` (12 pts, rest 0.283 distance-fallback) → candy hangs at rest.
  `RequestRestart` → exactly 1 `Level` after restart (old destroyed, new created — no leak);
  state `Playing`; ropes 2. **Zero console errors/warnings.**
- **All 6 levels load to `Playing`** with correct rope / star / interactable counts (play-mode smoke).
  Screenshot: `docs/evidence/us008_level01_playmode.png`.

### E2E (manual — partial)

- 6 level prefabs authored at `Assets/_Project/Levels/Level_0{1..6}.prefab`, each rooted on a
  `Level` with a unique auto-gen GUID; `LevelCatalog` Box 1 holds all six.
- Coverage of M2a interactables across the set: **auto-grab ×4** (01, 02, 03, 06), **bubble ×3**
  (01, 04, 06), **air-cushion ×3** (01, 05, 06) — each ≥ 2×.
- **Caveat (honest):** level geometry was authored via automation (duplicate-and-modify from
  the proven gray-box layout), not hand-tuned. A play-mode smoke caught and fixed a real defect
  — `Level_04`'s bubble overlapped the candy at start → instant envelop → candy floated out
  the playfield top → `Lost` on load; repositioned the bubble below the candy so it loads to
  `Playing`. **Full 3-star solvability tuning (intended cut path, both lose paths) is the
  remaining human E2E task** — geometry is not guaranteed winnable at 3 stars as authored.

### Platform

Deferred to the M2a-exit WebGL rebuild (phone-checked) — separate from this story.

## Implementation notes

- `RopeAuthoring.cs` (per-rope anchor/restLength/segmentCount, candy-target enum) +
  `RopeSimulation.AddRope` 3-arg overload; `RopeSimulationDriver` prefers authored ropes and
  falls back to gray-box `RopeAnchor`s (backward compatible).
- `Level.cs` auto-GUID + root gizmo; `OmNom.cs` marker (gizmo + validator hook, no behavior
  change to `CandyInteractor`'s layer-mask eat detection).
- `LevelCatalog.cs` SO (`TryGetLevel`/`TryGetBox`/`AllLevels`) + asset at
  `Assets/_Project/Data/LevelCatalog.asset`.
- `LevelValidator.cs` pure static `ValidateLevel`/`ValidateCatalog`.
- `GameSession.cs` catalog hook (`loadLevelId`) + restart-ownership fix: catalog/prefab
  instances destroy+recreate on restart; adopted in-scene levels left intact.
- `Game.unity` shelled to `Main Camera` + `Session` + `HUD Canvas`; all gameplay content lives
  in the level prefabs (progression.md "one Game scene; levels are content loaded into it").
