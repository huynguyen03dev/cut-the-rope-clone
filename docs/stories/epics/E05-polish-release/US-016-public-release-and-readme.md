# US-016 Public itch Release + Portfolio README (M4 exit)

## Status

planned — provisional until M3 exit.

## Lane

normal

## Product Contract

The project is a public portfolio artifact: the itch page plays well in a
browser within ten seconds, and the README sells the engineering.

## Relevant Product Docs

- `docs/product/overview.md` (purpose and priorities — the resume checklist)
- `DESIGN.md` §9

## Acceptance Criteria

- itch.io page public: title, cover, description, correct embed size,
  mobile-friendly settings; load-to-playable under ~10 s on a normal
  connection.
- README: gameplay GIF at the top; "how the rope solver works" section with
  one diagram; honest trade-off notes including why 2D joints were rejected;
  link to the itch page; build/run instructions.
- `Game.Core` reads clean in ten minutes: final naming/comment pass on the
  solver, tests green (`story verify-all` passes across all stories with
  verify commands).
- Commit history visibly tracks milestones (tags or clearly-worded merge
  commits M0–M4).
- Final full pass: every level, save persistence, audio, on desktop browser
  + phone.

## Design Notes

- The README solver write-up can lift from
  `docs/decisions/0008-custom-verlet-rope-solver.md` and DESIGN §2.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Full suite green |
| Integration | `story verify-all` green |
| E2E | Complete playthrough on the public build |
| Platform | Phone Safari + Chrome on the public URL |

## Harness Delta

Project retrospective: run `scripts/bin/harness-cli audit` and `propose`,
close or carry backlog items, and record a final trace summarizing harness
friction across the project.

## Evidence

Public URL, README link, final test output.
