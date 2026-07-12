# Milestone 4 — Flashlight & Lighting

**Status:** ✅ Complete · **Depends on:** M1, M3

A production-quality, battery-driven flashlight built from small composable parts, plus the dynamic
lighting behaviours (flicker, instability, low-battery, emergency) and the light-visibility service
the enemy AI will later query. This is the "light is safe" half of Nordo's core tension.

---

## New assembly

| Assembly | Depends on | Purpose |
| --- | --- | --- |
| `Nordo.Lighting` | `Nordo.Core`, `Nordo.Input` | Battery, flashlight, flicker modulators, quality presets, indicator, light-visibility service |

`Nordo.Core` gained `ILightSource`, `ILightVisibilityService`, `ISaveable<TState>`, and the
`BatteryCollectedEvent` / `EmergencyFlickerRequestEvent` bus events. `Nordo.Interaction` gained
`BatteryPickup`.

---

## Scripts created — what each does

### Core (contracts)
- **`ILightSource`** — a queryable light ("is this point lit, how strongly?"). AI hook.
- **`ILightVisibilityService`** — aggregates all sources; one call to test a world point.
- **`ISaveable<TState>`** — forward-compatible save participation (the flashlight implements it now).
- **`BatteryCollectedEvent` / `EmergencyFlickerRequestEvent`** — bus events for recharge and scares.

### Lighting
- **`Battery`** — pure, serializable charge model (capacity, drain, recharge, deplete/change events).
  No Unity deps → unit-tested.
- **`FlashlightSettings`** — SO: beam intensity/range/angles/colour + drain rate + low threshold.
- **`LightQualityPreset`** — SO: shadow type/strength/resolution, render mode, volumetric hint;
  `Apply(Light)`. Author Low/Med/High assets.
- **`ILightModulator` (+ `LightModulatorState`)** — the composable brightness-multiplier contract.
- **`RandomInstabilityModulator`** — subtle constant shimmer + occasional dips (electrical texture).
- **`LowBatteryModulator`** — dims + flickers below a threshold, worsening toward empty.
- **`EmergencyFlickerModulator`** — violent scripted strobe; triggered by event or `Trigger()`.
- **`FlashlightController`** — orchestrates toggle, drain/recharge, modulator stack, audio, and the
  Light. Implements `ILightSource` (AI hook) and `ISaveable<FlashlightState>`.
- **`FlashlightState`** — the serializable snapshot (charge + on/off).
- **`VolumetricBeam`** — drives a translucent cone renderer to match the beam (volumetric support).
- **`BatteryIndicator`** — diegetic LED/emissive gauge: green→amber→red, blinks when low.
- **`LightVisibilitySystem`** — the `ILightVisibilityService` implementation (registered service).

### Interaction
- **`BatteryPickup`** — emits `BatteryCollectedEvent` (recharge) **and** `ItemPickedUpEvent`
  (inventory), plus a faint pickup noise; removes itself.

---

## Setup (once)

1. **Systems:** add a **`LightVisibilitySystem`** to your `Systems` GameObject (alongside NoiseSystem).
2. **Flashlight rig** — under the camera:
   ```
   CameraRig (or Camera)
    └ Flashlight            (empty)  → FlashlightController + the 3 modulators
       └ Spot Light         (Light, type Spot)   ← assign to controller's Light, or leave to auto-find
       └ (optional) BeamCone (MeshRenderer, transparent cone) → VolumetricBeam
       └ (optional) LED      (small MeshRenderer, emission on) → BatteryIndicator
   ```
3. On **`FlashlightController`**: assign the **InputReader**, a **FlashlightSettings** asset
   (*Create → Nordo → Lighting → Flashlight Settings*), optionally a **LightQualityPreset**
   (*Create → Nordo → Lighting → Light Quality Preset*), an **AudioSource**, and clips. Expand the
   **Battery** foldout and set capacity/drain.
4. Add **`RandomInstabilityModulator`**, **`LowBatteryModulator`**, **`EmergencyFlickerModulator`** to
   the same Flashlight object (the controller finds them automatically).

---

## How to test — feature by feature

| Feature | Test |
| --- | --- |
| **Toggle** | Press **F** → light turns on/off with a click (assign toggle clips). |
| **Battery drain** | Leave it on → the beam slowly powers your `BatteryIndicator` from green toward red. Set a low capacity / high drain to see it fast. |
| **Low-battery warning** | As the battery crosses the *Low Battery Threshold*, the beam begins to **dim and flicker**, the indicator **blinks red**, and a warning beep plays every *Warning Beep Interval*. |
| **Depletion** | Let it hit zero → beam cuts out with the *depleted* click; pressing **F** on empty gives only the dry click, not light. |
| **Recharge / battery pickup** | Place a prop with **`BatteryPickup`** (set *Charge Amount*, optionally assign a battery `ItemDefinition`). Look → `[E] Pick up Battery`; press **E** → the indicator jumps up, recharge sound plays, and (if an item is assigned) an `ItemPickedUpEvent` fires for the future inventory. |
| **Random instability** | Watch a steady beam → faint shimmer and the odd brief dip (tune on `RandomInstabilityModulator`). |
| **Emergency flicker** | Add a temporary script/trigger that raises `EventBus<EmergencyFlickerRequestEvent>.Raise(new EmergencyFlickerRequestEvent(1.5f, 1f))` (or call `EmergencyFlickerModulator.Trigger`) → violent strobe for the duration. |
| **Beam intensity / angle** | Edit `FlashlightSettings` (Intensity, Spot/Inner angle) — changes apply on play. `SetIntensity`/`SetBeamAngle` adjust at runtime. |
| **Shadows / quality presets** | Assign different `LightQualityPreset` assets → shadow type/strength/resolution and render mode change on the beam. |
| **Volumetric beam** | With a transparent cone mesh + `VolumetricBeam`, the shaft is visible in the air and tracks the beam's colour/brightness (fades with flicker, hides when off). |
| **Diegetic indicator** | The on-torch LED/emissive shows charge by colour and blinks when low — no screen HUD. |
| **Light-visibility (AI hook)** | From a temporary script call `ServiceLocator.Get<ILightVisibilityService>().GetIlluminationAt(point)` — it returns >0 only when the point is inside the beam cone, in range, and not behind a wall. (No AI consumes it yet — that's M7.) |
| **Save/load** | From a temporary script: `var s = flashlight.CaptureState();` change things, then `flashlight.RestoreState(s);` → charge and on-state return exactly. |

### Automated tests
**Window → General → Test Runner → EditMode → Run All** — the new **`BatteryTests`** (drain, clamp,
recharge, deplete-once, change-event, set-charge) pass alongside the existing suites.

If all of the above hold, **Milestone 4 is verified.**

---

## Design notes & decisions
- **Composition over a monolith.** `Battery` (data), the modulator stack (behaviour), and
  `FlashlightSettings`/`LightQualityPreset` (config) are separate and independently testable/reusable.
- **Modulator stack = camera-effect stack.** Same Open/Closed pattern: attach a component to add a
  light behaviour; the controller multiplies all active multipliers with no per-frame allocation.
- **Genuine save support before the save system.** `ISaveable<FlashlightState>` is real capture/restore
  now; M9 will just collect these — no retrofitting.
- **AI hook, no AI.** `ILightVisibilityService` is fully functional and fed by the flashlight, but
  deliberately unconsumed until Milestone 7 — exactly as scoped.
- **Performance.** Modulators cached once and iterated by index; indicator/volumetric use
  `MaterialPropertyBlock` (no material instancing); illumination query does range/cone checks before
  any raycast.

## Next: Milestone 5 — Noise System (player integration & tuning)
The noise core already exists (M3). Milestone 5 wires the remaining emitters, adds machinery/ambient
noise sources, a debug propagation visualizer, and tunes the whole acoustic model — completing the
stealth foundation before the enemy arrives in M7.
*(Awaiting approval before starting.)*
