# 0008 Custom Verlet Rope Solver Over Unity 2D Joints

Date: 2026-07-02

## Status

Accepted

## Context

Rope physics is the core technical risk of the Cut the Rope clone — clones
live or die on rope feel and cut behavior. Two viable approaches existed and
the choice shapes the whole gameplay layer (collision handling, interpolation,
interactables, candy visuals).

## Decision

Implement a custom Verlet / position-based-dynamics rope solver in
`Game.Core`, running in `FixedUpdate` at an explicit 1/60 timestep, with
mass-weighted distance constraints (12–20 relaxation iterations), render
interpolation via a per-step `renderPos` snapshot, and the candy as a shared
terminal Verlet point across all attached ropes.

Consequence for collision: the candy is a kinematic body driven by the
solver, so candy↔world interaction uses **explicit physics queries inside the
sim step** (`OverlapCircle` for stars/mouth/bubbles/auto-grab, swept
`CircleCast` for spikes), never trigger callbacks. Full spec: `DESIGN.md` §2.

## Alternatives Considered

1. Unity 2D joint chains (`HingeJoint2D`/`DistanceJoint2D` +
   `Rigidbody2D`s). Rejected: chains stretch and jitter under a heavy end
   mass (endless solver-fighting via drag/mass tuning), and cutting mid-chain
   leaves unstable stubs. Tunneling was *not* a rejection reason — Unity has
   CCD.

## Consequences

Positive:

- Faithful rope feel (the original used position-based dynamics); cutting is
  just deleting a constraint; multiple ropes per candy are free via the
  shared terminal point.
- Deterministic query ordering in the sim step — no callback-timing
  ambiguity; swept spike test solves tunneling by construction.
- Strongest resume element: tested, readable solver math in `Game.Core`.

Tradeoffs:

- We own integration, constraints, interpolation, and external-force
  application (impulses via `prevPos` adjustment) — all must be tested.
- A Verlet point has no angular state: candy rotation/squash must be derived
  from velocity and rope direction.

## Follow-Up

- Feel lives in four numbers — segment count, iteration count, damping,
  candy invMass — exposed in the inspector from day one (US-001).
- A browser prototype validating the model exists at
  `rope-prototype/index.html`; port its tuned defaults as starting values.
