# US-009 Moving Anchors (M2b)

## Status

planned — provisional until M2a exit.

## Lane

normal

## Product Contract

Anchors can slide along a rail or path (ping-pong), carrying their rope and
the candy naturally, without injecting energy into the simulation.

## Relevant Product Docs

- `docs/product/gameplay.md` (interactable #4)
- `DESIGN.md` §2 (moving anchors)

## Acceptance Criteria

- Anchor (invMass 0) position is updated **inside the fixed step, before the
  constraint solve**, with the path parameter evaluated at sim time — never
  from `Update` or an Animation clip.
- Anchor speed clamped relative to rope length so fast rails cannot whip the
  candy unrealistically; clamp is inspector-tunable.
- Path types: linear rail ping-pong first; waypoint path if a level needs it.
- Authoring: rail drawn as a gizmo; validation pass (US-008) checks anchors
  with rails have valid endpoints.
- Rendering of the anchor visual uses interpolated position like everything
  else.

## Design Notes

- Domain rules: "moved pre-solve at sim time" is the contract — a test
  should assert rope energy stays bounded under a moving anchor.
- UI surfaces: none.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Path evaluation at sim time; speed clamp; energy-bounded under motion |
| Integration | Editor: candy hangs stably from a ping-ponging anchor |
| E2E | Covered by US-011 level playthroughs |
| Platform | Rebuilt WebGL check at M2b exit |

## Harness Delta

None expected.

## Evidence

Add after validation exists.
