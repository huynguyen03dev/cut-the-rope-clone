# US-014 Audio Service and Sound Set (M3)

## Status

planned — provisional until M2 exit.

## Lane

normal

## Product Contract

The game has sound: rope snap, star pickup with rising pitch, bubble pop,
air puff, eat/gulp, win/lose stingers, UI clicks, and background music —
all working on WebGL after the first-tap unlock.

## Relevant Product Docs

- `docs/product/gameplay.md` (feel checklist)
- `DESIGN.md` §3 (audio), §4 (AudioContext unlock)

## Acceptance Criteria

- `AudioService` composed in Boot with a pooled `AudioSource` set; no
  sources instantiated per shot.
- Sound defs as ScriptableObjects: clip + volume/pitch jitter ranges.
- Star-collect pitch ramps per star collected within a level, resetting on
  restart (classic CtR).
- WebGL AudioContext unlocked on first tap (formalizing the US-004
  placeholder); no console errors before unlock.
- Mute/volume setting persisted via `SaveService` settings.
- Sourced or placeholder-final clips for every event in the contract list;
  licenses noted in Evidence.

## Design Notes

- UI surfaces: settings toggle (menu and/or pause).
- Domain rules: audio subscribes to the same event channels as everything
  else — gameplay code never calls audio directly.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Pitch-ramp sequence logic |
| Integration | Editor: each event fires its sound exactly once |
| E2E | Hosted build: sound works after first tap |
| Platform | Phone Safari + Chrome: unlock works, no stutter on first play |

## Harness Delta

None expected.

## Evidence

Clip sources and licenses; validation notes.
