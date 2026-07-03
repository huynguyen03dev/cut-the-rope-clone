# US-013 Main Menu, Box and Level Select (M3)

## Status

planned — provisional until M2 exit.

## Lane

normal

## Product Contract

The full scene flow works: Boot → MainMenu (box select → level select) →
Game → back. Boxes unlock at star thresholds; the level grid shows earned
stars; locked content reads clearly as locked.

## Relevant Product Docs

- `docs/product/progression.md` (scene flow, boxes, thresholds)

## Acceptance Criteria

- Boot scene composes services and loads MainMenu; box list renders from
  `LevelCatalog` + save data; locked boxes show the star threshold needed.
- Level select grid per box: stars earned per level (0–3), locked levels
  gated by sequence or threshold per catalog rules.
- Selecting a level loads the Game scene with that level; win screen offers
  next-level; completing a level returns updated stars to the select screen
  without a restart.
- Minimal pause surface in the Game HUD: pause button (the per-pointer
  UI-occlusion case from gameplay.md), with resume, restart, and
  quit-to-menu; pausing halts the sim cleanly.
- Back navigation at every depth; browser-refresh on WebGL lands somewhere
  sane (MainMenu with save intact).
- All transitions are UniTask sequences with cancellation; UI animated with
  PrimeTween; no per-frame allocations in menu code.

## Design Notes

- UI surfaces: MainMenu canvas (box carousel or list, level grid), shared
  fade/transition layer.
- Domain rules: unlock math is pure C# in `Game.Core` (testable).

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Unlock-threshold math; star display mapping |
| Integration | Editor: full flow Boot→menu→level→win→next→menu |
| E2E | Manual flow on hosted build |
| Platform | Touch nav on phone; refresh-recovery check |

## Harness Delta

None expected.

## Evidence

Add after validation exists.
