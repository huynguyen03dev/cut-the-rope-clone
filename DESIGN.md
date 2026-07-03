# Cut the Rope Clone — Design Document

Target: a polished, portfolio-quality Unity project with a playable WebGL build.
Unity 6 (6000.x LTS), URP with the 2D Renderer, New Input System.

Goals, in priority order:
1. The game feels good and gets **finished**.
2. The codebase reads well to an interviewer who opens the repo.
3. Good interview talking points.

*Revision 2 — incorporates senior architecture review (2026-07-02): candy
collision moved from trigger callbacks to explicit physics queries, sim loop
and interpolation pinned down, WebGL pulled forward to M1, asmdefs reduced to
two, milestones resliced, over-engineering trimmed.*

---

## 1. What the game actually is (mechanics inventory)

- A **candy** hangs from one or more **ropes**, each pinned to an **anchor**.
- The player **swipes** to cut ropes. Cutting is continuous — the finger path is
  tested against rope segments every frame, not on release.
- The candy must reach **Om Nom** (the frog). Reaching his mouth zone = win.
- Up to **3 stars** per level are collected by overlapping them with the candy.
- **Lose conditions:** candy leaves the playfield, or hits spikes.
- **Interactables** (build in this order):
  1. *Auto-grab rope* — a dashed circle; when the candy enters the radius, a new
     rope instantly attaches from the anchor to the candy.
  2. *Bubble* — envelops the candy, inverts gravity (buoyancy); tap to pop.
  3. *Air cushion* — tap to emit a puff; applies a radial impulse to the candy
     (and pushes bubbles); short cooldown.
  4. *Moving anchor* — anchor slides along a rail or path (ping-pong).
  5. *Spikes* — static first, then moving/rotating. Destroy candy on contact.
  6. *(Stretch)* Spider that crawls down the rope toward the candy; cut the rope
     it's on to drop it.

**Scoring:** stars collected, plus a time bonus.
**Progression:** levels grouped into "boxes" (worlds); boxes unlock at star
thresholds. This gives a save system and a level-select UI.

**Same-frame priority rules** (decide once, encode in the resolution order of
the sim step): win beats lose; if the candy touches a star and the mouth in the
same step, collect the star, then eat.

**Explicit non-goal:** ropes do not collide with scenery. The original barely
does this either; written down here so it doesn't creep in.

---

## 2. The core technical decision: rope physics

This is where clones live or die. Two viable approaches:

### Option A — Unity 2D joints (rejected)
Rope = chain of small `Rigidbody2D`s connected with `HingeJoint2D`/`DistanceJoint2D`.
Fast to prototype, but rejected for two specific reasons: joint chains
**stretch and jitter under a heavy end mass** (you fight the solver with
drag/mass tuning forever), and **cutting mid-chain leaves unstable stubs**.
(Note: tunneling is *not* a reason to reject it — Unity rigidbodies have
continuous collision detection. The stretch and the cutting are.)

### Option B — Custom Verlet rope (chosen)
The original game used custom position-based dynamics, and it's the standard
approach for faithful clones. Also the strongest resume element of the project:
*"implemented a position-based dynamics rope solver with mass-weighted
constraints and fixed-timestep interpolation"* is a real interview conversation.

**The simulation:**

```csharp
struct RopePoint {
    public Vector2 pos;
    public Vector2 prevPos;    // Verlet integration state — NOT a render snapshot
    public Vector2 renderPos;  // snapshot taken at the start of each fixed step
    public float   invMass;    // 0 = pinned anchor; candy has low invMass (heavy)
}
```

- **Integration (Verlet):** each fixed step,
  `vel = (pos - prevPos) * damping; prevPos = pos; pos += vel + gravity * dt*dt;`
  Damping ~0.99. Gravity can be flipped per-point (bubble) or globally.
- **Constraints:** a distance constraint between each consecutive pair,
  solved by relaxation, 12–20 iterations per step:

```csharp
Vector2 delta = b.pos - a.pos;
float dist = delta.magnitude;
float error = (dist - restLength) / dist;
float wSum = a.invMass + b.invMass;
a.pos += delta * error * (a.invMass / wSum);
b.pos -= delta * error * (b.invMass / wSum);
```

  The **invMass weighting is the trick**: the anchor (invMass 0) never moves,
  rope points (invMass 1) absorb most correction, the candy (invMass ~0.05)
  barely moves — so the rope feels light and the candy feels heavy.
- **The candy is just another Verlet point** — the terminal point shared by
  every rope attached to it. Multiple ropes on one candy is then free: each
  rope's last constraint targets the same shared `CandyBody` point.
- **Cutting:** deleting one constraint. The upper part stays hanging from the
  anchor; the lower part stays attached to the candy and swings with it.

### Sim loop and interpolation (pinned down, not optional)

- The solver runs in **`FixedUpdate`**. Set the project **Fixed Timestep to
  1/60 explicitly** (Unity defaults to 0.02 = 50 Hz). Clamp **Maximum Allowed
  Timestep** (~0.1 s) so a throttled browser tab doesn't death-spiral on
  refocus.
- **Render interpolation:** `prevPos` is integration state and cannot double as
  a render snapshot (multiple fixed steps can run in one frame). At the start
  of each fixed step, copy `pos → renderPos`; in `Update`, render
  `lerp(renderPos, pos, alpha)` where alpha is Unity's fixed-time accumulator
  fraction (`(Time.time - Time.fixedTime) / Time.fixedDeltaTime`).
- **The swipe-cut test runs against the interpolated positions**, not raw sim
  positions — players cut the rope they *see*.

### Candy ↔ world interaction: explicit queries, not trigger callbacks

A kinematic `Rigidbody2D` does **not** generate trigger callbacks against
static or other kinematic colliders by default, so "just use OnTriggerEnter2D"
silently fails for stars, spikes, and the mouth. Instead, since we own the
solver, the sim step queries the world explicitly — deterministic ordering,
no callback-timing ambiguity, ~10 lines:

- `Physics2D.OverlapCircle(candy.pos, r, layer)` for **stars, mouth zone,
  bubbles, and auto-grab radii**.
- `Physics2D.CircleCast(candy.prevStepPos → candy.pos)` against the hazard
  layer for **spikes** — the sweep genuinely solves tunneling at any speed.
- Query results are resolved at the end of the step in priority order (see §1).

The candy still has a follower GameObject (transform for the sprite, VFX,
query origin), but nothing depends on physics callbacks. External forces
(air puffs, buoyancy) are applied by adjusting the candy point's implicit
velocity (`prevPos -= impulse * dt`).

### Cutting aftermath

- The stub left dangling **from the candy** retracts/fades over ~0.5 s (as in
  the original) — it must not flail off the candy forever.
- Two cuts on one rope create a **free-falling middle piece** with no pins:
  despawn it once offscreen (or fade with the stub timer).
- Stubs are **not cuttable** — remove them from the swipe test set immediately.

### Moving anchors

- Anchors (invMass 0) are moved **inside the fixed step, before the constraint
  solve**, with the path parameter evaluated at sim time. An Update-driven or
  Animation-driven mover teleports the anchor between steps and injects energy
  into the rope.
- Clamp anchor speed relative to rope length, or fast rails whip the candy.

### Auto-grab attach (zero-energy rule)

When the candy enters the radius: the new rope's `restLength` is the
**anchor→candy distance at grab time** (not the trigger radius), and every
spawned point initializes with `prevPos = pos`. Both together mean the attach
injects zero energy — no yank on grab.

### Rope rendering

The point chain sags naturally. Render with a `LineRenderer` fed
Catmull-Rom–smoothed *interpolated* positions, or (nicer) a custom mesh strip
with a tiling rope texture. Reuse the position buffer — no per-frame
allocations (see WebGL notes).

### Candy visuals

A Verlet point has no angular state. Derive visual **rotation** from velocity
direction (or the average direction of attached ropes when hanging), and
**squash-and-stretch** from speed delta. Cheap, but it's a deliberate system,
not free from the sim.

---

## 3. System architecture

### Assemblies: exactly two asmdefs

```
Game.Core        — Verlet solver, swipe/segment math, save data model.
                   Referencing UnityEngine for Vector2/Mathf is fine — the
                   value is tested math, not engine-independence purity.
Game.Core.Tests  — EditMode tests: constraint solver convergence, segment–
                   segment intersection, save-data round-trip.
```

Everything else — MonoBehaviours, UI, services — lives in plain
`Assembly-CSharp`. Editor tooling goes in a normal `Editor/` folder (works
without an asmdef). Rationale: the test assembly *requires* Core to be an
asmdef; further splitting buys compile-time friction and a Gameplay↔UI
dependency fight on a project this size. (Interview answer: "I isolated the
simulation core for testability and deliberately didn't fragment the rest —
the project doesn't have the scale to pay for it.")

### Scene & flow

```
Boot (composition root) → MainMenu (box/level select) → Game
```

One **Game scene**; levels are content loaded into it. Add a
`[RuntimeInitializeOnLoadMethod]` fallback (or lazy service initializer) so
pressing Play directly in the Game scene works without Boot — iteration speed
depends on it.

Game flow is an explicit plain-C# state machine:

```
GameSession: Loading → Intro (camera pan) → Playing → Won | Lost → (restart/next)
```

### Events & lifecycle

A small set of plain **C# event channels** — `RopeCut`, `StarCollected`,
`CandyEaten`, `CandyLost`, `LevelCompleted`. Gameplay raises them; UI, audio,
save subscribe. No SO-event assets (hidden editor wiring; plain events read
better in a repo).

**Lifecycle rules (hard rules, not conventions):**
- The events object is **instance-owned by `GameSession` and rebuilt per
  level** — never a static class, or subscribers on destroyed level objects
  leak and throw across restarts.
- **All mutable state lives inside the level prefab instance. Restart =
  destroy + re-instantiate.** Nothing else to reset, ever.

### Dependency wiring

A hand-rolled composition root in Boot constructs services (`SaveService`,
`AudioService`, level catalog access) and injects them explicitly. No DI
framework — zero reflection cost, fully traceable, correctly sized for the
project.

### Level content

- Each level is a **prefab**: a `Level` root component plus child entities
  (anchors, ropes, stars, Om Nom, hazards) placed in the scene view, with
  custom gizmos. An editor **validation pass** checks: every rope has an
  anchor, exactly one candy, one Om Nom, and unique level IDs.
- The `Level` component carries a **stable GUID string ID** — saves key off it,
  never off prefab names or catalog indices (reordering the catalog must not
  corrupt saves).
- A `LevelCatalog` ScriptableObject lists boxes → level prefab references +
  star-unlock thresholds.
- `RopeAuthoring` component: anchor transform, target (candy or free end),
  rest length, segment count (~10–16); bakes into solver points at load.

### Persistence

`SaveService`: JSON to `Application.persistentDataPath`, schema
`{ version, bestStarsPerLevelId, settings }`. Keep the `version` int and one
test asserting v1 loads; **do not build migration machinery until a v2
exists**. On WebGL, persistentDataPath is IndexedDB with async flushing —
enable filesystem autosync (or flush explicitly on save) or data is lost on
tab close.

### Input

New Input System, pointer-based, multi-touch (every active finger is a cutter,
each with its own `TrailRenderer` slice VFX). Three decided details:

1. **Tap vs swipe:** a tap on a bubble/air cushion is also a tiny swipe
   segment. Resolve taps first with a distance+time threshold; only past the
   threshold does a pointer become a cutter. One touch must never both pop a
   bubble and cut its rope.
2. **UI occlusion per pointer:** check `IsPointerOverGameObject(pointerId)` —
   a swipe crossing the pause button must not cut ropes.
3. **First frame of a touch has no previous position** — no segment, skip the
   cut test that frame.

### Async & sequencing: UniTask, no coroutines

**UniTask** (free, MIT, Cysharp) replaces coroutines entirely. Two-tool rule:
PrimeTween animates *properties* (star punch, mouth open, stub fade, fades);
UniTask orchestrates *sequences* — intro camera pan, win flow (eat → star
count-up → win screen), lose beat, level transitions, cooldown timers.
PrimeTween tweens are awaitable, so the two compose (`await Tween.Scale(...)`
inside an async flow). Zero-allocation and player-loop-based, so WebGL-safe
(plain `Task` is not).

**Hard rule:** every async method takes a `CancellationToken` — use
`destroyCancellationToken` — so a level restart cancels all in-flight
sequences. Same lifecycle rule as tweens: nothing runs against a destroyed
object. Raw `IEnumerator` coroutines: banned in this codebase.

### Audio, pooling, tweens (deliberately small)

- `AudioService` with a pooled source set; sound defs as ScriptableObjects
  (clip + volume/pitch jitter). Pitch ramps per star collected — classic CtR.
- **No generic pool framework.** A handful of one-shot particles per level
  doesn't need one; pool the cut VFX only if profiling says so.
- **Tweens: PrimeTween** (free, MIT, Asset Store / `KyryloKuzyk/PrimeTween`).
  DOTween-style API but zero-allocation — chosen deliberately because WebGL's
  GC makes per-frame allocations visible as hitches (see §4). Covers the star
  punch, mouth-open, and UI animations. Rule: link every tween to its owner's
  lifecycle (kill on destroy) — untracked tweens on destroyed objects are the
  classic tween-library bug.

### Camera & rendering

Orthographic, fixed logical playfield height, width letterboxed or expanded
per aspect. Sorting layers: Background, Rope, Candy, Foreground, VFX, UI.

---

## 4. WebGL is the platform, not a port (pulled forward to M1)

Everything platform-specific only surfaces in a real hosted build. **Exit
criterion for M1: a playable WebGL build on itch.io (private), touch-tested on
a real phone.** Rebuild and re-test every milestone thereafter. Known traps:

- **Audio** requires a user gesture to unlock the browser AudioContext — gate
  audio init behind the first tap.
- **Saves** go to IndexedDB asynchronously — see Persistence above.
- **Touch input** through the New Input System behaves differently in mobile
  browsers (and the itch iframe can fight page scroll) — test swipes in Safari
  and Chrome on a device, not just the editor.
- **Compression:** verify Brotli works on itch; fall back to gzip if not.
- **GC hitches:** the WebGL GC makes per-frame allocations visible. No
  per-frame string building in the HUD, no closures allocated in hot paths,
  reuse LineRenderer position buffers.

---

## 5. Game feel checklist (what makes it read as "polished")

- Candy squash-and-stretch + swing rotation (derived — see §2 Candy visuals).
- Om Nom's eyes track the candy; mouth opens when it's near; eat animation +
  gulp on win; sad reaction on lose. Cheap, huge for charm — *this is what
  gets the link forwarded to a hiring manager.*
- Rope cut: snap sound, particle burst at the cut point, 2-frame hitstop.
- Star collect: scale punch + sparkle + rising pitch.
- Bubble: wobble float, satisfying pop.

---

## 6. Milestones

| # | Deliverable | Proves |
|---|-------------|--------|
| M0 | Project setup, two asmdefs, Boot scene, **Verlet rope hanging + cutting in a gray-box scene**, solver tests passing | The hard part works |
| M1 | Candy, Om Nom, stars, win/lose, restart — one full level. **Exit: WebGL build on itch (private), touch-tested on a phone** | Complete loop, platform de-risked |
| M2a | Auto-grab rope, bubble, air cushion + ~6 levels | Shippable game — this is the cut line |
| M2b | Moving anchors, spikes + remaining levels (10–12 total, 2 boxes) | Content pipeline at scale |
| M3 | Menu, level select, save, audio, full UI flow | It's a *game*, not a demo |
| M4 | Juice pass, public itch release, README with GIFs + solver write-up | The portfolio artifact |

Do M0 first and alone. If the rope feels good gray-boxed, everything
downstream is normal Unity work. Feel lives in four numbers — segment count,
iteration count, damping, candy invMass — **expose all four in the inspector
from day one** and budget real tuning time.

---

## 7. Folder layout

```
Assets/_Project/
  Scripts/
    Core/            (asmdef: Game.Core)
    Gameplay/  UI/   (plain Assembly-CSharp)
    Editor/          (editor folder, no asmdef)
  Tests/             (asmdef: Game.Core.Tests)
  Prefabs/{Entities, Levels, UI, VFX}
  Art/  Audio/  Scenes/  Settings/  Data/ (LevelCatalog, sound defs)
```

---

## 8. Risks & mitigations

- **Rope feel is wrong** → almost always: iteration count too low (rubber
  band), damping too low (jitter), or candy invMass too high (rope drags the
  candy). The four inspector-exposed numbers are the fix loop.
- **Fast swipes miss ropes** → test the full frame-to-frame swipe segment
  (segment–segment), never point-vs-segment; and test against interpolated
  positions.
- **Candy tunnels through spikes** → the swept `CircleCast` in the sim step
  handles any speed by construction.
- **WebGL surprises at the end** → structurally prevented: build at M1,
  rebuild every milestone.
- **Scope creep** → M2a is the shippable cut line; the spider is stretch;
  rope-vs-scenery collision is a written non-goal.

---

## 9. What the repo must show (resume checklist, in order of impact)

1. **The itch link plays well in a browser within ten seconds.** ~80% of the
   value.
2. **README**: a GIF up top, a "how the rope solver works" section with one
   diagram, honest trade-off notes (including why joints were rejected).
3. **`Game.Core`**: tight, tested solver math readable in ten minutes.
4. **Commit history** that visibly tracks the milestones.

Explicitly *not* on the list (no interviewer will ever see them): extra
asmdefs, save-migration frameworks, generic pools, SO-event plumbing.
