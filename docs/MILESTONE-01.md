# Milestone 1 — Core Foundation & First-Person Movement

**Status:** ✅ Complete · **Depends on:** nothing

This milestone stands up the skeleton the whole game hangs from, and delivers a fully
controllable first-person character. Everything compiles and is testable in isolation.

---

## What shipped

### Project skeleton
- Git-friendly layout under `Assets/_Project/` (see `docs/ARCHITECTURE.md`).
- Package manifest (`Packages/manifest.json`) pinning URP, Input System, TMP, Addressables,
  AI Navigation, Cinemachine, Test Framework, etc.
- One assembly per subsystem, with a strictly one-directional dependency graph:
  `Nordo.Core` → `Nordo.Input` → `Nordo.Player` → `Nordo.Game` (+ `Nordo.Tests.EditMode`).

### Core services (`Nordo.Core`)
- **`EventBus<T>`** — allocation-free, typed publish/subscribe over `struct` events, with a
  registry that resets cleanly between play sessions (domain-reload-safe).
- **`IGameEvent`** — marker constraining events to value types.
- **`ServiceLocator`** — interface-keyed registry for global services (used from M5 onward).
- **Player locomotion events** — `PlayerJumpedEvent`, `PlayerLandedEvent`,
  `PlayerStanceChangedEvent`, and the `LocomotionStance` enum (Idle/Crouch/Walk/Sprint).

### Input (`Nordo.Input`)
- **`NordoControls.inputactions`** — full control map (Move, Look, Sprint, Crouch, Jump,
  Interact, Flashlight, Pause) for Keyboard&Mouse **and** Gamepad.
- **`InputReader`** — a `ScriptableObject` facade: continuous inputs polled via `Tick()`,
  discrete inputs exposed as C# events. Decouples all gameplay from the Input System.

### Player (`Nordo.Player`)
- **`MovementSettings`** — `ScriptableObject` tuning for speeds, crouch, jump, gravity, stamina.
- **`FirstPersonMotor`** — `CharacterController`-based walk / sprint / crouch (with ceiling
  check) / jump / gravity / stamina; authoritative over stance; raises locomotion events.
- **`PlayerLook`** — clamped mouse-look (yaw on body, pitch on camera), pause-aware.
- **`PlayerController`** — cursor lock and a lightweight time-freeze pause.

### Game & Tests
- **`GameBootstrapper`** — composition root; in M1 it logs locomotion events for easy QA.
- **`EventBusTests`** (EditMode) — proves delivery, unsubscribe, multi-subscriber, empty-raise.

---

## How to test it

### A. Open the project
1. Open the folder in **Unity 2022.3 LTS** (or newer LTS). Unity resolves packages and
   generates `.meta` files and `ProjectSettings/` on first import.
2. Confirm there are **no compile errors** in the Console. (Foundation goal met.)

### B. Run the automated tests
1. **Window → General → Test Runner → EditMode → Run All.**
2. All four `EventBusTests` should pass — this validates the event backbone.

### C. Build a quick test scene (2 minutes)
1. Create a new scene. Add a **Plane** as the floor.
2. Create the player rig:
   - Empty GameObject **"Player"** → add **Character Controller**, **`FirstPersonMotor`**,
     **`PlayerLook`**, **`PlayerController`**.
   - As a child, add a **Camera** named **"CameraPivot"** at head height (~1.6 m).
3. Create the input & settings assets:
   - **Right-click → Create → Nordo → Input → Input Reader**; assign `NordoControls` to its
     *Actions* field.
   - **Right-click → Create → Nordo → Player → Movement Settings**.
4. Wire the inspector references:
   - On `FirstPersonMotor`: assign the **InputReader** and **MovementSettings**.
   - On `PlayerLook`: assign the **InputReader** and the **CameraPivot** transform.
   - On `PlayerController`: assign the **InputReader**.
5. Add an empty GameObject **"Systems"** → add **`GameBootstrapper`**.
6. Press **Play**.

### D. What you should observe
| Action | Expected result |
| --- | --- |
| Mouse move | Smooth, clamped look; body yaws, camera pitches (can't flip over) |
| WASD | Walk with slight acceleration/deceleration |
| Hold **Shift** while moving | Sprint; console logs `Stance -> Sprinting`; stamina drains |
| Hold **Ctrl** | Crouch (capsule shrinks); slower; won't stand under a low ceiling |
| **Space** | Jump; console logs `Player jumped …` then `Player landed …` |
| **Esc** | Pause: time freezes, cursor unlocks; press again to resume |
| Watch Console | Stance transitions and jump/land events print via the EventBus |

If all of the above hold, **Milestone 1 is verified.**

---

## Notes & decisions
- **No `.meta` files are committed by hand** — Unity generates them deterministically on first
  import. After opening once, commit the generated metas so GUID references stay stable.
- **Movement uses `CharacterController`, not Rigidbody** — deterministic, jitter-free feel that
  suits tense, precise horror traversal.
- **Sprint is deliberately costly** (stamina + it will be the loudest stance) to set up the
  Milestone 5 noise/stealth core, where *moving fast = being heard*.

## Next: Milestone 2 — Camera Feel & Footsteps
Head-bob, surface-aware footstep audio, breath/cold fog hooks, and wiring stance → footstep
cadence — the first taste of the acoustic layer.
