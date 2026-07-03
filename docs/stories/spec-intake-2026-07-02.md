# Spec Intake

Date: 2026-07-02

## Source

- User prompt: "I just set up a fresh harness skeleton so I can start dev on
  this game — what should I do now?"
- Attached file: `DESIGN.md` (revision 2, incorporates senior architecture
  review of 2026-07-02).
- External reference: `rope-prototype/index.html` — working browser Verlet
  prototype with the four feel parameters on sliders.

## Project Summary

A polished, portfolio-quality Cut the Rope clone in Unity 6 (URP 2D, New
Input System) with a playable WebGL build on itch.io. Audience: interviewers
and hiring managers; the goals in priority order are finish it, readable
codebase, interview talking points.

## Candidate Product Docs

| File | Purpose | Source sections |
| --- | --- | --- |
| `docs/product/overview.md` | Goals, platform, roadmap, non-goals | DESIGN §§ intro, 4, 6, 8, 9 |
| `docs/product/gameplay.md` | Mechanics, win/lose, interactables, input, feel bar | DESIGN §§ 1, 2 (cut rules), 5 |
| `docs/product/progression.md` | Scoring, boxes, level content model, saves, scene flow | DESIGN §§ 1, 3 |

## Candidate Epics

| Epic | Description | Status |
| --- | --- | --- |
| E01-rope-simulation | Verlet solver, cutting, interpolation, gray-box (M0) | sliced (US-001) |
| E02-core-loop | Candy, Om Nom, stars, win/lose, restart, first WebGL build (M1) | unsliced |
| E03-interactables | Auto-grab, bubble, air cushion, moving anchors, spikes + levels (M2a/M2b) | unsliced |
| E04-game-shell | Menu, level select, save, audio, UI flow (M3) | unsliced |
| E05-polish-release | Juice pass, public itch release, README write-up (M4) | unsliced |

## Architecture Questions

- Runtime stack: Unity 6 (6000.x LTS), URP 2D Renderer, New Input System,
  UniTask + PrimeTween (coroutines banned). Decided in DESIGN.md.
- Product surfaces: WebGL (itch.io) primary; editor is dev surface only.
- Storage: JSON save to persistentDataPath (IndexedDB on WebGL, explicit
  flush). No server.
- External providers: none (itch.io hosting only).
- Deployment target: itch.io WebGL, private at M1, public at M4.
- Security model: none needed — offline single-player, local saves.

## Validation Shape

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests in `Game.Core.Tests`: constraint convergence, segment–segment intersection, save round-trip |
| Integration | Editor-driven: level validation pass, sim-step query resolution order |
| E2E | Manual playthrough per level; win/lose/restart flow |
| Platform | Hosted WebGL build, touch-tested on a real phone (Safari + Chrome), every milestone from M1 |
| Release | Full test run + WebGL build + save persistence across tab close |

## Open Decisions

- None blocking M0. Recorded: 0008 (custom Verlet solver over 2D joints).
- Deferred until their milestone: exact level count per box (M2b), audio
  asset sourcing (M3), itch page/compression fallback verification (M1).

## First Story Candidates

- US-001 (created): M0 — project skeleton, two asmdefs, Boot scene, Verlet
  rope hanging + cutting in a gray-box scene, solver tests passing.
- US-002 (next, after US-001): M1 core loop slice.

## Harness Delta

- Initialized `harness.db` (`init` + `import brownfield`; 7 decisions
  imported).
- Product docs seeded; epic backlog populated; decision 0008 recorded;
  US-001 story packet created and registered.
