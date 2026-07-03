# Product Overview

A Cut the Rope clone: a physics puzzle game where the player swipes to cut
ropes so a hanging candy reaches Om Nom, collecting up to three stars per
level along the way.

Source spec: `DESIGN.md` (revision 2, 2026-07-02). That file is intake input;
this doc and its siblings are the living contract.

## Purpose and priorities

Portfolio-quality Unity project with a playable WebGL build. In priority
order:

1. The game feels good and gets finished.
2. The codebase reads well to an interviewer who opens the repo.
3. Good interview talking points.

## Platform and stack

- Unity 6 (6000.x LTS), URP with the 2D Renderer, New Input System.
- **WebGL is the platform, not a port.** A hosted, phone-touch-tested itch.io
  build is the exit criterion from M1 onward and is rebuilt every milestone.
- Custom Verlet rope solver, not Unity 2D joints — see
  `docs/decisions/0008-custom-verlet-rope-solver.md`.

## Roadmap (milestones)

| # | Deliverable | Proves |
| --- | --- | --- |
| M0 | Setup, two asmdefs, Boot scene, Verlet rope hanging + cutting gray-boxed, solver tests | The hard part works |
| M1 | Candy, Om Nom, stars, win/lose, restart; WebGL build on itch, phone-tested | Complete loop, platform de-risked |
| M2a | Auto-grab rope, bubble, air cushion, ~6 levels | Shippable game — the cut line |
| M2b | Moving anchors, spikes, 10–12 levels in 2 boxes | Content pipeline at scale |
| M3 | Menu, level select, save, audio, full UI flow | It's a game, not a demo |
| M4 | Juice pass, public itch release, README with GIFs + solver write-up | The portfolio artifact |

M2a is the shippable cut line. The spider interactable is stretch.

## Non-goals (written down so they don't creep in)

- Ropes do not collide with scenery.
- No extra asmdefs beyond `Game.Core` + `Game.Core.Tests`, no save-migration
  machinery before a v2 schema exists, no generic pool framework, no
  ScriptableObject event assets, no DI framework.
