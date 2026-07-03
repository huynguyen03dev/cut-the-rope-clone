# US-001 Verlet Rope Hanging and Cutting (M0)

## Status

planned

## Lane

normal

## Product Contract

In a gray-box Game scene, one or more ropes hang from anchors with a heavy
candy point at the end. The rope sags and swings believably (no stretch, no
jitter), renders smoothly at any frame rate, and any active pointer's swipe
cuts the rope segment it visually crosses — every finger is a cutter. Cutting leaves the upper part hanging from
the anchor and the lower part swinging from the candy. The four feel
parameters — segment count, iteration count, damping, candy invMass — are
inspector-exposed.

## Relevant Product Docs

- `docs/product/overview.md` (M0 row, non-goals)
- `docs/product/gameplay.md` (cutting rules, input rules)
- `docs/decisions/0008-custom-verlet-rope-solver.md`
- `DESIGN.md` §2 (full solver spec), §3 (asmdefs, folder layout), §6 (M0)

## Acceptance Criteria

- Project layout per DESIGN §7: `Assets/_Project/...` with exactly two
  asmdefs (`Game.Core`, `Game.Core.Tests`); Fixed Timestep set to 1/60,
  Maximum Allowed Timestep clamped (~0.1 s).
- Verlet solver in `Game.Core`: integration with damping, mass-weighted
  distance constraints (12–20 iterations), invMass 0 anchors, low-invMass
  candy shared across ropes.
- Render interpolation via per-fixed-step `renderPos` snapshot; rope drawn
  from interpolated positions with a reused position buffer.
- Swipe cut: frame-to-frame segment vs rope segment test against
  interpolated positions, **per pointer** — every active pointer is an
  independent cutter with its own previous-position state; first frame of
  each touch skipped; cut = constraint removal; both halves swing
  correctly and cut stubs leave the swipe test set immediately (never
  cuttable). Scope: single-cut split only — stub retract/fade and
  double-cut middle-piece despawn are US-003's (they need the M1
  tween/async tooling; coroutines are banned).
- Rendering baseline for the gray-box scene: orthographic camera with fixed
  logical playfield height, letterboxed/expanded per aspect; sorting layers
  created (Background, Rope, Candy, Foreground, VFX, UI); a defined
  playfield rect (consumed by US-002's out-of-bounds lose check).
- Boot scene exists; pressing Play directly in the gray-box scene works.
- EditMode tests pass: constraint solver convergence, segment–segment
  intersection, cut-splits-rope behavior.
- Four feel parameters inspector-exposed and tuned until the rope feels
  good gray-boxed (compare against `rope-prototype/index.html`).

## Design Notes

- Commands: none (no CLI surface).
- Queries: none in M0 — candy↔world queries (stars/spikes) start in M1.
- API: `RopePoint { pos, prevPos, renderPos, invMass }`; solver pure C# in
  `Game.Core`, MonoBehaviour drivers in `Assembly-CSharp`.
- Tables: none.
- Domain rules: stubs not cuttable from the moment of the cut; stub
  retract/fade and double-cut free-piece despawn are explicitly deferred to
  US-003 (named there — senior review finding #1).
- UI surfaces: none (gray-box).

## Validation

When updating durable proof status, use numeric booleans:
`scripts/bin/harness-cli story update --id US-001 --unit 1 --integration 0 --e2e 0 --platform 0`.

| Layer | Expected proof |
| --- | --- |
| Unit | `Game.Core.Tests` EditMode suite green (Unity Test Runner CLI once wired) |
| Integration | n/a for M0 |
| E2E | Manual: swipe-cut in gray-box scene feels right; recorded GIF as evidence |
| Platform | n/a — first WebGL build is the M1 exit criterion |
| Release | n/a |

## Harness Delta

- First story to need a Unity batch-mode test command; when wired, set it as
  this story's `--verify` and register it as a harness tool.

## Evidence

Add commands, reports, screenshots, or links after validation exists.
