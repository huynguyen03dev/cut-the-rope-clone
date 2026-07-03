# US-002 Candy World Interaction via Explicit Queries (M1)

## Status

planned — provisional until US-001 (M0) exit; refine against the real solver API.

## Lane

normal

## Product Contract

The candy (the shared terminal Verlet point from US-001, now with a follower
GameObject and sprite) interacts with the world through explicit physics
queries inside the sim step: it collects stars on overlap, wins by reaching
Om Nom's mouth zone, and loses by leaving the playfield. Same-frame priority:
win beats lose; star collects before eat.

## Relevant Product Docs

- `docs/product/gameplay.md` (core loop, same-frame priority rules)
- `docs/decisions/0008-custom-verlet-rope-solver.md` (queries-not-callbacks)
- `DESIGN.md` §2 (candy ↔ world interaction)

## Acceptance Criteria

- `Physics2D.OverlapCircle` at the candy point each fixed step for stars and
  the mouth zone; results resolved at end of step in priority order (win
  beats lose; star, then eat). No `OnTriggerEnter2D` anywhere in the path.
- Swept-query infrastructure (`CircleCast` from prev step pos to pos against
  a hazard layer) is wired, even though the hazard layer stays empty until
  spikes (US-010); exercised once in play mode against a temporary test
  collider — full anti-tunneling proof lands in US-010.
- Playfield-exit detection, against the playfield rect defined in US-001,
  triggers the lose path.
- Plain C# event channels raised from gameplay: `RopeCut` (raised at the
  cut site with the cut position — audio/VFX in US-014/US-015 subscribe),
  `StarCollected`, `CandyEaten`, `CandyLost`.
- Candy follower GameObject mirrors the interpolated point position; visual
  swing rotation derived from velocity / attached-rope direction (full
  squash-and-stretch deferred to US-015).
- External-force hook: impulses applied via `prevPos` adjustment, unit-tested
  (needed by US-006/US-007).

## Design Notes

- Commands: none. Queries: OverlapCircle (stars/mouth), CircleCast (hazards).
- API: query + resolution pass lives at the end of the fixed step, in the sim
  driver; priority resolution order is pure C# in `Game.Core` so it is
  unit-testable.
- Domain rules: resolution order is decided once, encoded in code order.
- UI surfaces: none (HUD lands in US-003).

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Priority-resolution logic: star+mouth same step → star then eat; win+lose same step → win |
| Integration | Editor play test: star collect, eat, out-of-bounds each fire exactly one event; hazard sweep hits a temporary test collider |
| E2E | Covered by US-003 full-loop manual test |
| Platform | n/a (US-004) |

## Harness Delta

None expected.

## Evidence

Add after validation exists.
