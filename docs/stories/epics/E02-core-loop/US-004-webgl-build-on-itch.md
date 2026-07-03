# US-004 First WebGL Build on itch.io, Phone-Tested (M1 exit)

## Status

planned — provisional until US-001 (M0) exit.

## Lane

normal (risk flags: cross-platform, weak proof — stronger validation required)

## Product Contract

The M1 game loop runs as a hosted WebGL build on itch.io (private), playable
by touch on a real phone in Safari and Chrome. This is the M1 exit criterion
and the build is refreshed every milestone thereafter.

## Relevant Product Docs

- `docs/product/overview.md` (WebGL is the platform, not a port)
- `DESIGN.md` §4 (WebGL traps)

## Acceptance Criteria

- WebGL build (URP 2D) uploads to a private itch.io page and reaches
  playable in ~10 s or less on a normal connection (same bar US-016 must
  hold at release); Brotli compression verified working on itch, gzip
  fallback documented if not.
- Audio init gated behind the first user tap (browser AudioContext unlock) —
  verified with a placeholder sound if AudioService (US-014) doesn't exist yet.
- Touch swipes cut ropes and taps register correctly on a real phone in both
  mobile Safari and Chrome; the itch iframe does not fight page scroll
  (`touch-action` verified).
- Multi-touch: two simultaneous fingers each cut independently on device.
- Fixed-timestep behavior sane on tab refocus (Maximum Allowed Timestep clamp
  verified — no death spiral).
- No visible GC hitches during normal play (no per-frame allocations in HUD
  or rope rendering — profiler or frame-time check).
- Build steps documented (README or `docs/stories/` progress note) so every
  later milestone can repeat them mechanically.

## Design Notes

- Domain rules: none — this story is platform proof, not behavior.
- Consider a `scripts/` build command; if added, register it as a harness
  tool and set it as this story's `--verify`.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | n/a |
| Integration | n/a |
| E2E | Manual playthrough in desktop browser on the hosted build |
| Platform | Touch test on a real phone, Safari + Chrome, findings noted in Evidence |

## Harness Delta

First platform-proof story: whatever build/deploy steps emerge should become
a registered tool or documented command so `plat` proof is repeatable.

## Evidence

Add device/browser test notes, build size, and the itch URL after validation.
