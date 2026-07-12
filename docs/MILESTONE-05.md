# Milestone 5 — Noise System (completion, tuning & validation)

**Status:** ✅ Complete · **Depends on:** M1, M3

The noise **core** shipped in M3 (`NoiseSystem`, `NoiseStimulus`, `INoiseService`, `INoiseListener`,
`ImpactNoiseEmitter`, `DebugNoiseListener`). Milestone 5 completes it into the finished acoustic
stealth system: one canonical channel, all player noise integrated, machinery/ambient emitters, a
finished propagation model, a propagation visualizer, extracted+tested maths, and a stress tester.

All new work is in the existing **`Nordo.Noise`** assembly (→ `Nordo.Core`). No new assemblies.

---

## Scripts created / changed — what each does

### New
- **`NoiseAttenuation`** (static) — the pure propagation maths (`DistanceFalloff`, `Occlude`,
  `Perceive`), extracted so the model is deterministic and **unit-tested**.
- **`PlayerNoiseEmitter`** — the single translator of player events → `NoiseEvent`: footsteps
  (stance→priority), heavy exhales (only above a threshold), and jump effort. One place to tune all
  player noise.
- **`PeriodicNoiseEmitter`** — reusable environmental/ambient emitter (drips, creaks, wind) firing on
  a jittered interval; optional one-shot audio; `SetActive` toggle.
- **`MachineNoiseEmitter`** — machinery (generator/vent) with a running state: a loud start spike, a
  steady hum-noise pulse + looping audio while running, `StartMachine`/`StopMachine` for M8 puzzles.
- **`NoisePropagationVisualizer`** — Scene-view gizmos of every sound as an expanding wavefront +
  range sphere, coloured by priority, fading over a lifetime. Fixed ring buffer (no runtime alloc).
- **`NoiseStressTester`** — context-menu tool: registers N dummy listeners, fires M noises through the
  live service, reports µs/noise and total deliveries (performance + reliability).

### Changed
- **`NoiseSystem`** — now subscribes to the **single** `NoiseEvent` channel (footstep translation moved
  to `PlayerNoiseEmitter`). Propagation finished: tunable `AnimationCurve` distance falloff,
  **multi-occluder** attenuation (compounds per wall) via a preallocated `RaycastNonAlloc` buffer, an
  audibility floor, and a `ListenerCount` diagnostic. Delegates its maths to `NoiseAttenuation`.

---

## The complete emitter inventory (consistency check)

Every gameplay system now emits through the same `NoiseEvent` channel:

| Source | Emitter | Kind | Typical priority |
| --- | --- | --- | --- |
| Footsteps (walk/sprint/crouch/land) | `PlayerNoiseEmitter` ← `FootstepEvent` | Footstep | Minor→Alarming |
| Heavy breathing | `PlayerNoiseEmitter` ← `BreathEvent` | Voice | Minor |
| Jumping | `PlayerNoiseEmitter` ← `PlayerJumpedEvent` | Generic | Minor |
| Thrown/dropped props | `ImpactNoiseEmitter` (M3) | Impact | Minor→Alarming |
| Doors / drawers / cabinets | `OpenableBase` (M3) | Door | Notable |
| Locked-object rattle | `Lockable` (M3) | Door | Minor |
| Item / battery pickup | `ItemPickup` / `BatteryPickup` (M3/M4) | Object | Minor |
| Generators / machines | `MachineNoiseEmitter` | Machine | Notable→Alarming |
| Environment (drips, creaks) | `PeriodicNoiseEmitter` | Generic/Ambient | Ambient |

---

## How to test — complete guide

> Scene setup: a **`NoiseSystem`** on your `Systems` object; a **`PlayerNoiseEmitter`** on the player;
> a **`NoisePropagationVisualizer`** anywhere (watch the **Scene** view during Play). Optionally a
> **`DebugNoiseListener`** to represent "the enemy's ears".

### 1. Player noise integration
- **Walk / sprint / crouch** → the visualizer shows a wavefront per step: small yellow (crouch),
  orange (walk), large red (sprint). The `DebugNoiseListener` flashes when in range.
- **Sprint until winded, then stop** → heavy exhales produce small `Voice` blips; calm breathing is
  silent (below the audible threshold).
- **Jump** → a small `Generic` blip at takeoff; the landing footfall produces a larger step blip.

### 2. Machinery
- Add a **`MachineNoiseEmitter`** (assign loop/one-shot AudioSources + clips). Call `StartMachine()`
  (e.g. a temporary context-menu or trigger) → a big red start spike, then steady orange hum rings on
  the interval. `StopMachine()` silences it. This is the generator behaviour M8 will drive.

### 3. Ambient environment
- Add a **`PeriodicNoiseEmitter`** (Ambient priority, low loudness, long jittered interval) near a
  pipe/beam → faint blue rings at irregular intervals. Toggle with `SetActive(false)`.

### 4. Finished propagation model
- **Distance:** stand a listener far out — the `DebugNoiseListener` gizmo shrinks with distance and
  vanishes past the effective range.
- **Occlusion (per wall):** place one wall between a sound and the listener → perceived loudness drops
  by the transmission factor; place a **second** wall → it drops again (compounds). Enable
  *Log Heard Noises* on the NoiseSystem to see the occluder count and value.
- **Falloff curve:** edit the NoiseSystem's *Falloff Curve* (e.g. sharpen the near field) → the
  perceived values change accordingly.

### 5. Propagation visualizer
- With `NoisePropagationVisualizer` active and the Scene view open in Play mode, every sound draws an
  **expanding ring** (the wavefront) growing to its full range, a source dot sized by loudness, and a
  faint range sphere — all colour-coded by priority and fading out. This is the tuning workhorse.

### 6. Stress test (performance + reliability)
- Add **`NoiseStressTester`** to any object. Enter Play mode, then use its component context-menu →
  **Run Stress Test**. The Console reports e.g. `20,000 noises × 50 listeners in X ms (Y µs/noise).
  Deliveries: N`. Expect **sub-microsecond-to-low-µs per noise** and a non-zero, stable delivery count
  (listeners in range reliably hear audible noises). Increase counts to probe scaling — cost stays
  linear thanks to the distance early-out and non-alloc occlusion.

### 7. Automated tests
**Window → General → Test Runner → EditMode → Run All** — the new **`NoiseAttenuationTests`**
(source/edge/linear falloff, zero-range silence, per-wall occlusion compounding, combined model,
custom curve) pass alongside all prior suites.

If all of the above hold, **Milestone 5 is verified** and the stealth foundation is complete.

---

## Design notes & decisions
- **One channel.** Collapsing everything onto `NoiseEvent` (and moving footstep translation into
  `PlayerNoiseEmitter`) means `NoiseSystem` has a single responsibility and every emitter is consistent
  — no special cases to forget when the AI arrives.
- **Testable maths.** Pulling propagation into `NoiseAttenuation` turns the stealth rules into pure
  functions we can prove correct without a scene.
- **Performance by construction.** Distance early-out before any raycast; occlusion via a preallocated
  `RaycastNonAlloc` buffer; ring-buffer visualizer; zero per-noise heap allocation. The stress tester
  exists to keep us honest.
- **Ready for content.** Machines and ambience are real, exploitable sounds now, so level design and
  the M7 enemy can lean on the acoustic layer immediately.

## Next: the Vertical Slice
Per the new priority, upcoming work builds a **finishable** experience — inventory, puzzles, **The
Listener** AI (consuming this noise channel), one complete level, objectives, menus, and save/load —
over more framework.
*(Awaiting approval.)*
