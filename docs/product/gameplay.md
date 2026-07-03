# Gameplay

Player-facing behavior contract. Solver internals live in `DESIGN.md` §2 and
`docs/decisions/0008-custom-verlet-rope-solver.md`.

## Core loop

- A candy hangs from one or more ropes, each pinned to an anchor.
- The player swipes to cut ropes. Cutting is continuous: the finger path is
  tested against rope segments every frame, not on release.
- Win: the candy reaches Om Nom's mouth zone.
- Lose: the candy leaves the playfield or hits spikes.
- Up to 3 stars per level, collected by overlapping the candy with them.

## Same-frame priority rules

Resolved once per sim step, in this order: win beats lose; if the candy
touches a star and the mouth in the same step, collect the star, then eat.

## Cutting rules

- The swipe-cut test runs against the rope positions the player *sees*
  (interpolated), not raw sim positions.
- Cut aftermath: the stub dangling from the candy retracts/fades over ~0.5 s;
  a doubly-cut free middle piece despawns offscreen or fades on the stub
  timer; stubs are never cuttable.

## Interactables (build in this order)

1. **Auto-grab rope** — dashed circle; candy entering the radius attaches a
   new rope from anchor to candy. Attach injects zero energy: rest length =
   anchor→candy distance at grab time, spawned points start at rest.
2. **Bubble** — envelops the candy, inverts gravity; tap to pop.
3. **Air cushion** — tap to emit a puff; radial impulse on candy (and
   bubbles); short cooldown.
4. **Moving anchor** — anchor slides along a rail/path (ping-pong).
5. **Spikes** — static first, then moving/rotating; destroy candy on contact.
6. *(Stretch)* Spider crawling down a rope toward the candy.

## Input rules

New Input System, pointer-based, multi-touch: every active finger is a cutter
with its own trail VFX. Decided details:

1. **Tap vs swipe:** taps resolve first via a distance+time threshold; only
   past it does a pointer become a cutter. One touch must never both pop a
   bubble and cut its rope.
2. **UI occlusion per pointer:** a swipe crossing UI (e.g. pause button) must
   not cut ropes — check `IsPointerOverGameObject(pointerId)`.
3. First frame of a touch has no previous position — skip the cut test.

## Game feel checklist (the polish bar)

- Candy squash-and-stretch + swing rotation derived from the sim.
- Om Nom: eyes track candy, mouth opens when near, eat + gulp on win, sad on
  lose.
- Rope cut: snap sound, particle burst at cut point, 2-frame hitstop.
- Star collect: scale punch + sparkle + rising pitch per star.
- Bubble: wobble float, satisfying pop.
