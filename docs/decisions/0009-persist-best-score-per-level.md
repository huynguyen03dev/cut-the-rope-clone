# 0009 Persist Best Score Per Level

Date: 2026-07-02

## Status

Accepted

## Context

Senior review of the story plan (intervention #1, finding #3) flagged
time-bonus scoring as orphaned: DESIGN.md §1 defines score = stars + time
bonus, but no story implemented it and the pinned v1 save schema
(`{ version, bestStarsPerLevelId, settings }`) had no score field, leaving
best-score persistence undefined. The human chose persistence over
session-only score.

## Decision

Best score per level persists. The v1 save schema becomes
`{ version: 1, bestStarsPerLevelId, bestScorePerLevelId, settings }` — score
is part of v1 from the start. Because nothing has shipped, this is a schema
amendment, not a migration; the no-migration-machinery-before-v2 rule
stands. Timer, bonus computation, and win-screen display land in US-003;
persistence and its tests land in US-012. Both best-stars and best-score
only ever increase.

## Alternatives Considered

1. Session-only score (win-overlay display, nothing saved). Simpler and kept
   the schema exactly as DESIGN.md wrote it, but loses the classic CtR
   best-score chase and would likely become a v2 schema change later anyway.

## Consequences

Positive:

- Closer to the original game; score has a reason to exist beyond the win
  screen.
- Decided before US-012 exists — no schema churn or migration cost.

Tradeoffs:

- US-012 carries extra tests (score round-trip, best-only-increases).
- US-013's level grid may want to show best scores — optional, decide at its
  refinement.

## Follow-Up

- US-003: timer + bonus math + win-overlay display, `LevelCompleted` carries
  stars + score.
- US-012: schema field + tests.
