# Progression, Levels, and Persistence

## Scoring and progression

- Score = stars collected + a time bonus, computed at win and shown on the
  win overlay. Best score per level persists (decision 0009).
- Levels are grouped into "boxes" (worlds); boxes unlock at star thresholds.

## Level content model

- Each level is a prefab: a `Level` root component plus child entities
  (anchors, ropes, stars, Om Nom, hazards) placed in the scene view, with
  custom gizmos.
- The `Level` component carries a **stable GUID string ID** — saves key off
  it, never off prefab names or catalog indices.
- A `LevelCatalog` ScriptableObject lists boxes → level prefab references +
  star-unlock thresholds.
- An editor validation pass checks: every rope has an anchor, exactly one
  candy, one Om Nom, unique level IDs.

## Persistence

- `SaveService`: JSON to `Application.persistentDataPath`, schema
  `{ version, bestStarsPerLevelId, bestScorePerLevelId, settings }`
  (best score in v1 from the start — decision 0009).
- Keep the `version` int and one test asserting v1 loads; no migration
  machinery until a v2 exists.
- WebGL: persistentDataPath is IndexedDB with async flushing — flush
  explicitly on save (or enable filesystem autosync) or data is lost on tab
  close.

## Scene flow

```
Boot (composition root) → MainMenu (box/level select) → Game
```

One Game scene; levels are content loaded into it. Pressing Play directly in
the Game scene must work without Boot (lazy service init fallback).

Game flow state machine:
`Loading → Intro (camera pan) → Playing → Won | Lost → (restart/next)`.

Restart rule: all mutable state lives inside the level prefab instance;
restart = destroy + re-instantiate. Nothing else to reset, ever.
