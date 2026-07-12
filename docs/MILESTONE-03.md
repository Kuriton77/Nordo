# Milestone 3 — Interaction & Physics

**Status:** ✅ Complete · **Depends on:** M1

A complete, reusable interaction framework and the central **noise system** that the enemy AI will
consume in Milestone 7. Everything is interface-driven, composition-based, and production-ready — no
placeholders, no TODOs.

---

## New assemblies

| Assembly | Depends on | Purpose |
| --- | --- | --- |
| `Nordo.Items` | `Nordo.Core` | Item definitions + pickup event (inventory-ready foundation) |
| `Nordo.Noise` | `Nordo.Core` | Central hearing/propagation service + physics impact emitter |
| `Nordo.Interaction` | `Core`, `Input`, `Items`, `InputSystem`, `TextMeshPro` | The whole interaction framework |

`Nordo.Core` gained the noise model (`NoiseStimulus`, `SoundPriority`, `INoiseService`,
`INoiseListener`, `NoiseEvent`), the `InteractionPromptEvent`, and a reusable `ControlLockEvent`.

---

## Scripts created (grouped) — what each does

### Core — shared contracts
- **`SoundPriority` / `NoiseSourceKind`** — the enum vocabulary for arbitration and source weighting.
- **`NoiseStimulus`** — immutable "one sound" value (position, loudness, range, priority, kind).
- **`INoiseListener` / `INoiseService`** — the hear/report seam between sounds and the AI.
- **`NoiseEvent`** — bus event any emitter raises to make a sound.
- **`InteractionPromptEvent`** — decouples prompt UI from interaction logic.
- **`ControlLockEvent`** — reusable request to freeze player look/movement (used by inspection).

### Items — inventory-ready foundation
- **`ItemDefinition`** — ScriptableObject: id, name, description, icon, category, stacking, **weight**,
  world prefab. One asset = one item type.
- **`ItemPickedUpEvent`** — raised on collection; the Milestone-6 inventory will consume it.

### Noise — the stealth core
- **`NoiseSystem`** — subscribes to `NoiseEvent` + `FootstepEvent`, attenuates by distance and
  optional **occlusion** raycast, dispatches to listeners. Registered as `INoiseService`. Optimised:
  cheap distance early-out before any raycast, zero per-noise allocation.
- **`NoiseListenerBase`** — auto-(un)registers a listener with the service; subclass and implement
  `OnHeardNoise`. (The enemy's ears in M7.)
- **`DebugNoiseListener`** — draws gizmos of the last heard sound + hearing radius; logs to console.
- **`ImpactNoiseEmitter`** — turns Rigidbody collisions into noise scaled by impact energy
  (**weight × speed**), with optional impact SFX.

### Interaction — the framework
- **`IInteractable` / `InteractionContext`** — the contract + the per-interaction data.
- **`IInteractionOverride`** — lets hold/inspect temporarily own the interact button.
- **`InteractableBase`** — reusable base: highlight-on-focus, lock-aware prompting/blocking.
- **`Lockable`** — composable lock (key id, rattle SFX, locked-attempt noise, `Unlocked` event).
- **`Highlighter`** — emission highlight via `MaterialPropertyBlock` (no material instancing).
- **`PlayerInteractor`** — raycast focus, prompt publishing, and interact routing (overrides first).
- **`InteractionPromptPresenter`** — TMP prompt UI driven purely by events.
- **`OpenableBase`** — smooth eased open/close + audio + noise + lock, shared by:
  - **`Door`** (hinge rotate), **`Drawer`** (slide), **`Cabinet`** (multi-leaf rotate).
- **`ItemPickup`** — grants an `ItemDefinition`, emits pickup event + noise, removes itself.
- **`Grabbable` + `HeldItemController`** — pick up a physics prop, carry it, **throw** it (interact
  while holding) or drop it; respects a max carry weight.
- **`Inspectable` + `InspectionController`** — lift a prop into view, rotate it with the mouse, locks
  player control; release to restore everything exactly.

### Updated
- **`FirstPersonMotor` / `PlayerLook`** — now honour `ControlLockEvent` (inspection freezes control).

---

## How to test — feature by feature

> Continue from your M1/M2 test scene. Ensure a **`NoiseSystem`** exists in the scene (add it to the
> `Systems` object) and add a **`DebugNoiseListener`** on an empty GameObject near your props with a
> generous *Hearing Range* (e.g. 20). Keep the Scene view visible to watch its gizmos.

### Player setup (once)
On the **Player root**, add:
- **`PlayerInteractor`** — assign the **Camera** and the **InputReader**.
- **`HeldItemController`** — assign a **Hold Anchor** (empty child of the camera ~0.7 m forward) and
  the **Camera**.
- **`InspectionController`** — assign the **Camera** and the **InputReader**.

For prompts: add a **Canvas** with a child panel + **TMP Text**, put **`InteractionPromptPresenter`**
on the panel and assign the label. (Any UI canvas works.)

### 1. Raycast interaction + prompt + highlight
- Put a **Door** in front of the player (see #3). Look at it → the prompt appears (`[E] Open`) and, if
  the door's material has **Emission enabled** and it has a `Highlighter`, it glows. Look away → both clear.

### 2. Lockable
- Add a **`Lockable`** to the door; tick *Is Locked*. Look at it → prompt shows **"Locked"**. Press
  **E** → rattle SFX (if assigned) + the DebugNoiseListener registers a small noise. Call
  `Lockable.Unlock()` (e.g. from a temporary test button or another script) → it opens normally.

### 3. Doors / Drawers / Cabinets
- **Door:** empty root + a child mesh as the *Hinge*; add `Door`, set *Open Angle* 95. Press **E** →
  smooth swing open; **E** again → closes. Each operation pings the DebugNoiseListener.
- **Drawer:** child mesh as *Slide*, `Drawer`, *Open Distance* 0.45. Opens by sliding.
- **Cabinet:** `Cabinet`, add two *Leaves* (two door child transforms) with angles +90 / −90 → both
  swing apart together.

### 4. Item pickup (inventory-ready)
- Create an **Item Definition** (*Create → Nordo → Items → Item Definition*). On a small prop add
  **`ItemPickup`**, assign the definition. Look → `[E] Pick up <name>`. Press **E** → it disappears,
  a pickup noise fires, and the Console shows the `ItemPickedUpEvent` if you log it. (Inventory storage
  arrives in M6; the event is already emitted.)

### 5. Grab + throw physics + impact noise
- Make a prop with a **Rigidbody**, a **Collider**, **`Grabbable`**, and **`ImpactNoiseEmitter`**.
  Look → `[E]`; press **E** to pick it up (it floats at the hold anchor). Press **E** again to **throw**.
- When it hits a wall/floor, the DebugNoiseListener flashes a gizmo — **bigger/redder for harder hits**
  (throw it fast or use a heavier Rigidbody to compare). This is the physics-driven noise pillar.

### 6. Object inspection
- On a prop add **`Inspectable`** (optionally a *Caption*). Look → `[E] Inspect`; press **E** → the
  object lifts into view, **player movement/look freeze**, and moving the mouse **rotates the object**.
  Press **E** → it returns exactly to its place and control unlocks.

### 7. Noise propagation + occlusion + priority (the big one)
- With the **DebugNoiseListener** placed, do each action and watch the Scene gizmo:
  - **Crouch-walk** near it → tiny grey/yellow blips (Minor/Ambient).
  - **Walk** → orange blips (Notable). **Sprint** → red blips (Alarming) reaching further.
  - **Throw a prop** → a red blip at the impact point.
  - **Put a wall between** the sound and the listener (enable *Use Occlusion* on the NoiseSystem) →
    the perceived loudness drops (smaller gizmo). Move to line-of-sight → it jumps back up.

### 8. Automated tests
**Window → General → Test Runner → EditMode → Run All** — `EventBus`, `CameraEffectSample`, and the
new **`NoiseStimulus`** tests (effective range, clamping, priority ordering) all pass.

If all of the above hold, **Milestone 3 is verified.**

---

## Design notes & decisions
- **Interfaces + composition, not deep inheritance.** `Lockable` and `Highlighter` are components you
  add to *any* interactable; the interactor knows only `IInteractable`. New interactables cost one
  small class.
- **Emitters never reference the AI.** Everything that makes a sound raises a `NoiseEvent`; the
  `NoiseSystem` mediates. This is what will let the M7 enemy "just work" by subclassing
  `NoiseListenerBase`.
- **Optimised for large levels.** Highlighting uses `MaterialPropertyBlock` (SRP-batch friendly,
  no instancing); noise does a distance early-out before any raycast and allocates nothing per sound.
- **Weight matters.** Rigidbody mass gates what you can carry and — via collision impulse — how loud an
  impact is, tying physics directly to stealth.
- **Reusable control lock.** `ControlLockEvent` is a general gate (inspection today; menus/cutscenes
  later) so no system reaches into the player to freeze it.

## Next: Milestone 4 — Flashlight & Lighting
Battery-driven flashlight, dynamic light rig, battery pickups (built on this milestone's item system),
and a diegetic charge readout — the light half of Nordo's "light is safe, noise is lethal" tension.
*(Awaiting approval before starting.)*
