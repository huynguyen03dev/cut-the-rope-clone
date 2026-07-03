# US-012 Save Service and Persistence (M3)

## Status

planned — provisional until M2 exit.

## Lane

normal — risk flags: data model, weak proof → stronger validation required.

## Product Contract

Player progress (best stars per level, settings) persists across sessions on
all platforms, including WebGL tab close. Saves survive catalog reordering
because they key off stable level GUIDs.

## Relevant Product Docs

- `docs/product/progression.md` (persistence, GUID rule)
- `docs/decisions/0009-persist-best-score-per-level.md`
- `DESIGN.md` §3 (persistence), §4 (IndexedDB)

## Acceptance Criteria

- `SaveService` (composed in Boot, injected explicitly): JSON to
  `Application.persistentDataPath`, schema
  `{ version: 1, bestStarsPerLevelId, bestScorePerLevelId, settings }` —
  best score is in v1 from the start, see decision 0009.
- Keys are level GUID strings — a test proves a reordered catalog still
  resolves saved stars.
- `version` int present; one test asserts a canned v1 payload loads. **No
  migration machinery until a v2 exists.**
- WebGL: explicit flush on every save (or verified filesystem autosync);
  proven by save → close tab → reopen → progress intact, on a real phone.
- Corrupt/missing save file degrades to a fresh save without crashing.
- Save data round-trip test lives in `Game.Core.Tests` (data model is plain
  C# in `Game.Core`).

## Design Notes

- Tables: none — single JSON document.
- Domain rules: best stars and best score only ever increase; settings
  write-through.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Round-trip (stars + score), v1-loads, GUID-keyed lookup after reorder, best-only-increases, corrupt-file fallback |
| Integration | Editor: win level → restart editor → stars retained |
| E2E | Full flow: play, quit, relaunch, continue |
| Platform | WebGL tab-close persistence on phone — the IndexedDB flush proof |

## Harness Delta

None expected.

## Evidence

Add after validation exists.
