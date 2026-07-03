# Story Backlog

Populated from `docs/stories/spec-intake-2026-07-02.md` (DESIGN.md rev 2).
Full roadmap sliced 2026-07-02 at the human's request; every story after the
active one is **provisional** — refine its packet at the preceding
milestone's exit before starting it.

## Candidate Epics

| Epic | Description | Status |
| --- | --- | --- |
| E01-rope-simulation | Verlet solver, cutting, interpolation, gray-box (M0) | sliced |
| E02-core-loop | Candy, Om Nom, stars, win/lose, restart, first WebGL build (M1) | sliced |
| E03-interactables | Auto-grab, bubble, air cushion, moving anchors, spikes + levels (M2a/M2b) | sliced |
| E04-game-shell | Menu, level select, save, audio (M3) | sliced |
| E05-polish-release | Juice pass, public itch release, README write-up (M4) | sliced |

## Story Order

Implementation order = story number. M2a (through US-008) is the shippable
cut line. The spider interactable is stretch and has no story.

| Story | Milestone | Packet |
| --- | --- | --- |
| US-001 Verlet rope hanging + cutting | M0 | `epics/E01-rope-simulation/US-001-verlet-rope-hanging-and-cutting.md` |
| US-002 Candy world queries | M1 | `epics/E02-core-loop/US-002-candy-world-queries.md` |
| US-003 Game session, win/lose, restart | M1 | `epics/E02-core-loop/US-003-game-session-win-lose-restart.md` |
| US-004 WebGL build on itch (M1 exit) | M1 | `epics/E02-core-loop/US-004-webgl-build-on-itch.md` |
| US-005 Auto-grab rope | M2a | `epics/E03-interactables/US-005-auto-grab-rope.md` |
| US-006 Bubble | M2a | `epics/E03-interactables/US-006-bubble.md` |
| US-007 Air cushion | M2a | `epics/E03-interactables/US-007-air-cushion.md` |
| US-008 Level pipeline + ~6 levels (M2a exit) | M2a | `epics/E03-interactables/US-008-level-pipeline-first-levels.md` |
| US-009 Moving anchors | M2b | `epics/E03-interactables/US-009-moving-anchors.md` |
| US-010 Spikes | M2b | `epics/E03-interactables/US-010-spikes.md` |
| US-011 Two boxes, 10–12 levels (M2b exit) | M2b | `epics/E03-interactables/US-011-boxes-and-remaining-levels.md` |
| US-012 Save service | M3 | `epics/E04-game-shell/US-012-save-service.md` |
| US-013 Menu + level select | M3 | `epics/E04-game-shell/US-013-menu-and-level-select.md` |
| US-014 Audio service | M3 | `epics/E04-game-shell/US-014-audio-service.md` |
| US-015 Juice pass | M4 | `epics/E05-polish-release/US-015-juice-pass.md` |
| US-016 Public release + README (M4 exit) | M4 | `epics/E05-polish-release/US-016-public-release-and-readme.md` |

## Refinement Rule

At each milestone exit, before starting the next story: re-read its packet,
update acceptance criteria against what was actually built, and only then
move it to `in_progress`. A provisional packet is a plan, not truth.
