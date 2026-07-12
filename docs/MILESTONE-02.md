# Milestone 2 — Camera Feel & Footsteps

**Status:** ✅ Complete · **Depends on:** M1

This milestone turns the functional M1 character into one that *feels* AAA: a layered camera-feel
system, a surface-aware footstep system with correct speed-driven timing, and a breath /
cold-breath system. Everything is modular, event-driven, and free of placeholders.

---

## New assemblies

| Assembly | Depends on | Purpose |
| --- | --- | --- |
| `Nordo.CameraFeel` | `Nordo.Core` | The additive camera-effect stack and its effects |
| `Nordo.Audio` | `Nordo.Core` | Surface data + the footstep system |

The Player assembly gained the breath components; `Nordo.Core` gained the shared
`ILocomotionState` interface, `SurfaceKind`, and the `FootstepEvent`/`BreathEvent` types.

---

## Scripts created (and what each does)

### Core (shared contracts)
- **`ILocomotionState`** — read-only view of player movement (stance, speed, grounded, move input,
  stamina). The Dependency-Inversion seam: camera & audio depend on this, not on `FirstPersonMotor`.
- **`SurfaceKind`** — enum of material categories (Wood, Concrete, Metal, Snow…), shared by audio
  and the future stealth layer.
- **`FootstepEvent` / `BreathEvent`** — event payloads carrying position, surface, stance and a
  stealth **loudness**, so the M5 hearing system can reuse them unchanged.

### Camera feel (`Nordo.CameraFeel`)
- **`CameraEffectSample`** — an additive (position + Euler) offset; the unit of composition.
- **`CameraRigState`** — the per-frame snapshot passed to every effect.
- **`ICameraEffect`** — the one-method contract each effect implements (`Evaluate` → sample).
- **`CameraRig`** — discovers all effects on its GameObject, sums + smooths their samples, applies
  them after aim (late execution order). This is the whole stack's engine.
- **`HeadBobEffect`** — procedural bob with per-stance profiles (walk / sprint / crouch); cadence
  scales with real speed.
- **`CameraSwayEffect`** — look-driven sway (view trails and counter-rolls on turns) + always-on
  Perlin idle drift so the camera feels alive.
- **`LandingImpactEffect`** — damped-spring dip + pitch kick on landing, scaled by fall speed;
  driven by `PlayerLandedEvent`.
- **`MoveTiltEffect`** — leans the camera into strafes and adds a small forward lean when sprinting.

### Audio / footsteps (`Nordo.Audio`)
- **`SurfaceDefinition`** — ScriptableObject: per-surface footstep/landing clips, pitch/volume
  shaping, and a stealth loudness. New surfaces = new assets, no code.
- **`SurfaceIdentifier`** — component that tags a collider with its surface (highest priority).
- **`SurfaceLibrary`** — resolves a raycast hit → surface via tag → physics-material map → default.
- **`FootstepController`** — distance-accumulator that fires footsteps at the right cadence per
  stance, resolves the surface underfoot, plays a randomized non-repeating clip, and raises a
  `FootstepEvent`. Also plays landing sounds.

### Player (breath)
- **`BreathController`** — models breathing rate/intensity from exertion (stamina + sprint), plays
  optional breath audio, and raises `BreathEvent` on each inhale/exhale.
- **`ColdBreathVisualizer`** — emits fog particles on exhale while in a cold environment; exposes
  `SetColdEnvironment(bool)` for a future temperature system.

### Also updated
- **`PlayerLook`** — added configurable, jitter-reducing look **smoothing** (0 = raw).
- **`FirstPersonMotor`** — now implements `ILocomotionState` (adds `MoveInput`, `NormalizedPlanarSpeed`).
- **`GameBootstrapper`** — optional console logging of footstep/breath events for QA.

---

## How to test it

> Build on the M1 test scene (see `docs/MILESTONE-01.md`). If you don't have it, make that first.

### 1. Restructure the camera into a rig (2 min)
Arrange the player hierarchy like this:
```
Player            (CharacterController, FirstPersonMotor, PlayerLook, PlayerController)
 └ CameraPivot    (empty, at ~1.6 m — assign as PlayerLook's camera pivot)
    └ CameraRig   (empty — add CameraRig + the four *Effect components)
       └ Camera   (the actual Camera)
```
- On **`CameraRig`**: set **Yaw Source** = `Player`, **Pitch Source** = `CameraPivot`. Leave
  Locomotion Source empty (it finds `FirstPersonMotor` in the parents automatically).
- Add **`HeadBobEffect`**, **`CameraSwayEffect`**, **`LandingImpactEffect`**, **`MoveTiltEffect`**
  to the `CameraRig` object.

### 2. Set up footsteps
1. Create a few surfaces: **Create → Nordo → Audio → Surface Definition** (e.g. `Surface_Concrete`,
   `Surface_Wood`, `Surface_Metal`). Drop any short footstep clips into *Walk Footsteps* (and
   optionally Run/Crouch/Landing). *No clips yet? The system runs silently but still emits events —
   see step 4.*
2. Create **Create → Nordo → Audio → Surface Library**; assign a **Default** surface.
3. On the **Player**, add **`FootstepController`**; assign the **Surface Library**. (An AudioSource
   is created automatically if you don't assign one.)
4. To hear different surfaces: put a `SurfaceIdentifier` on a floor object and assign a surface, or
   add physics-material mappings to the library.

### 3. Set up breath (optional visual)
1. On the **Player** (or the CameraPivot), add **`BreathController`**; set **Head** to the
   CameraPivot.
2. Add **`ColdBreathVisualizer`**; assign a small fog **ParticleSystem** child at head height
   (any wispy particle works). Leave *Cold Environment* ticked.

### 4. Enable QA logging
On the **`GameBootstrapper`** object, tick **Log Footsteps** and **Log Breaths**.

### 5. Press Play — what you should observe
| Action | Expected |
| --- | --- |
| Walk (WASD) | Gentle head-bob; footstep cadence matches walking speed; `Footstep on … (Walking)` logs |
| Sprint (Shift) | Faster, punchier bob; forward camera lean; quicker/louder steps; higher loudness in log |
| Crouch (Ctrl) | Shallow, slow bob; slow, quiet steps (low loudness) |
| Strafe (A/D) | Camera **rolls** into the strafe direction |
| Turn quickly | Camera **sways/counter-rolls**, then settles — never rigid |
| Stand still | Camera still subtly **drifts** (idle life); no footsteps |
| Jump & land | Camera **dips + pitches** on impact and springs back; landing sound; harder falls dip more |
| Move mouse | Smooth, jitter-free look (tune *Smoothing Time* on PlayerLook to taste) |
| Watch breath | Fog puffs on exhale; after sprinting, breathing is faster/heavier and fog is denser |

### 6. Run automated tests
**Window → General → Test Runner → EditMode → Run All.** The `EventBus` and new
`CameraEffectSample` tests (identity, component-wise sum, commutativity) should all pass.

If all the above hold, **Milestone 2 is verified.**

---

## Design notes & decisions
- **Additive effect stack over a monolithic camera script.** Each effect is single-responsibility
  and independently toggleable (`IsActive`) — accessibility (disable head-bob), tuning, and future
  effects (weapon-lower, damage shake) all drop in without touching the rig.
- **Footsteps are distance-driven, not timed.** Cadence is automatically correct at any speed and
  never desyncs from movement; very fast frames still emit the right number of steps.
- **Everything the enemy will need is already emitted.** `FootstepEvent.Loudness` and
  `BreathEvent.Intensity` exist now so Milestone 5's hearing system is a pure consumer — no
  retrofitting the player.
- **No placeholders:** breath/cold hooks are functional systems that emit real events and VFX; they
  simply expose presentation and the "is it cold?" input for later systems to drive.

## Next: Milestone 3 — Interaction & Physics
`IInteractable`, a raycast interactor, doors/drawers/pickups, and throwable physics props — plus
the first physics-driven noise emitters (dropped objects) that feed the same stealth event stream.
*(Awaiting approval before starting.)*
