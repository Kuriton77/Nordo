# NORDO — Architecture, Roadmap & Build Order

This is the single source of truth for how **Nordo** is engineered. It is written to be read
top-to-bottom by any engineer joining the project.

---

## 1. Game Identity (so the tech serves the design)

| Aspect | Decision |
| --- | --- |
| **Genre** | First-person survival horror / stealth |
| **Setting** | *Vardø-9*, an abandoned Arctic radio-relay station |
| **Antagonist** | *The Listener* — a blind entity that hunts by sound |
| **Core loop** | Explore → gather → solve station machinery → transmit distress signal → escape, while managing **noise**, **light/battery**, and **cold** |
| **Signature mechanic** | **Acoustic stealth.** The enemy cannot see. All detection is sound-driven. Light is safe to use; noise is not. |
| **Secondary pressure** | **The Cold** — warmth zones, breath fog, cold-based stamina drain |
| **Tone** | Isolation, industrial decay, dread over gore |
| **Endings** | Multiple (Escape / Sacrifice / Silence / Trapped) driven by player choices and collected knowledge |

Everything below exists to make that design shippable, performant, and maintainable.

---

## 2. Architectural Principles

1. **SOLID + composition over inheritance.** Behaviours are small components wired in the
   inspector; systems depend on interfaces, not concrete classes.
2. **Event-driven core.** Systems communicate through a typed, allocation-free `EventBus<T>`
   and `ScriptableObject` event channels — never by singleton-reaching into each other.
3. **Data lives in `ScriptableObject`s.** Items, enemies, difficulty curves, audio profiles,
   puzzle definitions — all authored as assets, not hard-coded.
4. **Assembly-definition boundaries.** Each subsystem is its own assembly (`.asmdef`) so
   compile times stay low and dependencies stay one-directional (see the dependency graph).
5. **Performance by default.** Object pooling for anything spawned, cached component lookups,
   zero per-frame allocations in hot paths, no `Update()` where an event will do.
6. **Diegetic / mobile-ready UI.** UI is decoupled from gameplay via events, so it can be
   swapped (world-space, screen-space, or touch) without touching game logic.
7. **Determinism where it matters.** Randomized item placement uses a seeded RNG so runs are
   reproducible for QA and speedrun fairness.

---

## 3. Folder Structure

All content lives under `Assets/_Project/` so third-party/imported assets never mix with ours.

```
Assets/
└─ _Project/
   ├─ Art/
   │  ├─ Materials/
   │  ├─ Models/
   │  ├─ Textures/
   │  └─ VFX/
   ├─ Audio/
   │  ├─ Music/
   │  ├─ SFX/
   │  └─ Mixers/
   ├─ Input/                 # .inputactions assets
   ├─ Prefabs/
   │  ├─ Player/
   │  ├─ Enemies/
   │  ├─ Interactables/
   │  └─ Systems/
   ├─ Scenes/
   │  ├─ Bootstrap.unity      # loads persistent systems, then the main menu
   │  ├─ MainMenu.unity
   │  └─ Station.unity        # the playable level
   ├─ ScriptableObjects/
   │  ├─ Items/
   │  ├─ Enemies/
   │  ├─ Difficulty/
   │  ├─ Audio/
   │  └─ Events/              # ScriptableObject event channels
   ├─ Settings/              # URP assets, quality settings, input presets
   └─ Scripts/
      ├─ Core/               # asmdef: Nordo.Core     (no game deps)
      ├─ Input/              # asmdef: Nordo.Input
      ├─ Player/             # asmdef: Nordo.Player
      ├─ CameraFeel/         # asmdef: Nordo.CameraFeel (modular camera-effect stack)
      ├─ Interaction/        # asmdef: Nordo.Interaction
      ├─ Enemy/              # asmdef: Nordo.Enemy
      ├─ Items/              # asmdef: Nordo.Items         (item definitions & pickup events)
      ├─ Noise/              # asmdef: Nordo.Noise         (central hearing/propagation service)
      ├─ Puzzles/            # asmdef: Nordo.Puzzles
      ├─ Audio/              # asmdef: Nordo.Audio
      ├─ Save/               # asmdef: Nordo.Save
      ├─ UI/                 # asmdef: Nordo.UI
      └─ Game/               # asmdef: Nordo.Game (composition root / bootstrap)
```

**Dependency rule:** arrows only point *down* the list. `Core` depends on nothing in the
project; `Game` may depend on everything. No cycles, ever.

---

## 4. Feature Dependency Graph

```
                         ┌────────────┐
                         │    Core    │  EventBus, ServiceLocator, pooling,
                         │ (Nordo.Core)│  math utils, interfaces, base types
                         └─────┬──────┘
        ┌──────────────┬───────┼────────────┬───────────────┐
        ▼              ▼       ▼            ▼               ▼
   ┌─────────┐   ┌──────────┐ ┌──────┐ ┌──────────┐  ┌──────────┐
   │  Input  │   │  Audio   │ │ Save │ │  Items   │  │    UI    │
   └────┬────┘   └────┬─────┘ └───┬──┘ └────┬─────┘  └────┬─────┘
        │             │           │         │             │
        ▼             │           │         ▼             │
   ┌─────────┐        │           │   ┌────────────┐      │
   │ Player  │◄───────┘           │   │Interaction │      │
   └────┬────┘                    │   └─────┬──────┘      │
        │                         │         │             │
        ▼                         │         ▼             │
   ┌─────────┐                    │   ┌────────────┐      │
   │  Enemy  │  (needs Player as  │   │  Puzzles   │      │
   │         │   a sound source)  │   └─────┬──────┘      │
   └────┬────┘                    │         │             │
        └───────────────┬────────┴─────────┴─────────────┘
                         ▼
                  ┌────────────┐
                  │    Game    │  Composition root: scene flow, difficulty,
                  │(Nordo.Game)│  ending logic, glue. Depends on everything.
                  └────────────┘
```

Key read: **the acoustic-stealth Enemy depends on the Player** (the player is the primary
sound emitter). The **Noise System** is a Core-level service that both Player/Items *write* to
and Enemy *reads* from — that decoupling is what keeps the graph acyclic.

---

## 5. Development Roadmap & Milestones

Each milestone **must compile and be testable** before the next begins.

| # | Milestone | Delivers | Depends on |
| --- | --- | --- | --- |
| **M1** | **Core Foundation & First-Person Movement** | Folder/asmdef skeleton, EventBus, bootstrap, Input System asset, walk/look/sprint/crouch/jump, gravity | — |
| **M2** ✅ | **Camera Feel & Footsteps** | Modular camera-effect stack (head-bob, sway, landing impact, move-tilt), smooth look, surface-aware footsteps with speed-driven cadence, breath + cold-breath hooks | M1 |
| **M3** ✅ | **Interaction & Physics** | `IInteractable` + raycast interactor, prompts, highlight, doors/drawers/cabinets, lockables, item pickups, grab/throw, inspection, physics impact noise, **central NoiseSystem** with propagation + occlusion + priority | M1 |
| **M4** | **Flashlight & Lighting** | Battery-driven flashlight, dynamic light setup, battery pickups, diegetic charge indicator | M1, M3 |
| **M5** | **Noise System** | Central `NoiseSystem` service, noise emitters on movement/items/machinery, debug visualizer | M1, M3 |
| **M6** | **Inventory & Items** | `ItemDefinition` SOs, inventory model, keys/batteries/tools, randomized seeded placement | M1, M3 |
| **M7** | **Enemy AI** | NavMesh agent, patrol → hearing → investigate → search → chase → attack → lose → return FSM, memory | M1, M5, M2 |
| **M8** | **Puzzle Systems** | Fuse boxes, generators, combination locks, keyed doors, hidden passages, environmental story | M3, M6 |
| **M9** | **Save / Load** | JSON save service, scene state serialization, checkpoint & manual saves | M1, M6, M8 |
| **M10** | **Difficulty & Endings** | Difficulty SO curves, ending resolver, station-wide state machine | M7, M8, M9 |
| **M11** | **UI / UX & Menus** | Main menu, pause, settings, diegetic prompts, options persistence | M1, M9 |
| **M12** | **Audio Polish & Mix** | Audio mixer, spatialization, dynamic music/tension layers, occlusion | M2, M5, M7 |
| **M13** | **Optimization & Steam-ready** | Pooling audit, LODs, addressables, GC audit, build pipeline, Steam wrapper seam | all |

### Build Order (what to construct, in order)

1. Repo skeleton + packages + asmdefs *(M1)*
2. Core services: EventBus → ServiceLocator → ObjectPool *(M1/M5)*
3. Input layer *(M1)*
4. Player locomotion *(M1)* → camera/audio feel *(M2)*
5. Interaction + physics *(M3)* → flashlight *(M4)*
6. Noise service *(M5)* → items/inventory *(M6)*
7. Enemy AI *(M7)* → puzzles *(M8)*
8. Save/load *(M9)* → difficulty/endings *(M10)*
9. UI *(M11)* → audio mix *(M12)* → optimization/ship *(M13)*

---

## 6. Core Systems Reference (as they come online)

- **`EventBus<T>`** — static, typed, allocation-free publish/subscribe for `struct` events.
- **`ServiceLocator`** — lightweight registry for cross-cutting services (Noise, Save, Audio)
  to avoid singleton sprawl; registered at bootstrap, resolved by interface.
- **`ObjectPool<T>`** — generic pool for pooled spawns (audio one-shots, VFX, projectiles).
- **`NoiseSystem`** — the beating heart of stealth; emitters report `NoiseEvent`s, the enemy's
  hearing sensor subscribes and reasons about them.
- **`SaveService`** — versioned JSON persistence with per-object `ISaveable` participation.

---

## 7. Coding Standards

- One class per file; file name == type name.
- XML `<summary>` on every public type and member; inline comments explain *why*, not *what*.
- `[SerializeField] private` over `public` fields; expose via properties when needed.
- `[Header]`, `[Tooltip]`, `[Range]` for inspector ergonomics.
- No allocations in `Update`/`FixedUpdate` hot paths; cache components in `Awake`.
- Namespaces mirror assemblies: `Nordo.Core`, `Nordo.Player`, etc.
- Prefer events over polling; prefer `ScriptableObject` config over magic numbers.

---

### New in Milestone 2 — the camera-effect stack pattern

The camera feel is built as an **additive effect stack** (Open/Closed principle): `CameraRig`
discovers every `ICameraEffect` component on its GameObject, sums each one's
`CameraEffectSample` (a local position + Euler offset), smooths the total, and applies it to the
camera-effects transform. Adding a new camera behaviour is just attaching another component — no
rig changes. Effects read player state through the Core-level `ILocomotionState` interface, never
the concrete motor, keeping `Nordo.CameraFeel` dependent only on `Nordo.Core`.

Footsteps and breathing are **event emitters, not islands**: `FootstepController` raises a
`FootstepEvent` (position + surface + loudness) and `BreathController` raises a `BreathEvent` on
every step/exhale. Audio and cold-breath VFX consume these today; the Milestone-5 noise/AI layer
will consume the very same events, which is why they carry a stealth `Loudness` already.

### New in Milestone 3 — interaction framework & the noise system

**Interaction** is a strict interface play. The `PlayerInteractor` depends only on `IInteractable`
and `IInteractionOverride`; every concrete interactable (Door, Drawer, Cabinet, ItemPickup,
Grabbable, Inspectable) is a small subclass of `InteractableBase`, and cross-cutting concerns are
**composed, not inherited** — a `Lockable` component makes anything lockable, a `Highlighter`
makes anything highlightable. Adding a new interactable or a new hold/inspect-style override never
touches the interactor. UI is fully decoupled: prompts travel as `InteractionPromptEvent`s.

**Noise** is now a first-class Core service. Emitters (footsteps, thrown props via
`ImpactNoiseEmitter`, doors, pickups, locked-rattles) raise a `NoiseEvent` carrying a
`NoiseStimulus` (position, loudness, range, **priority**, kind) — they have **zero** knowledge of
who hears it. The `NoiseSystem` (registered as `INoiseService`) attenuates each stimulus by
distance and optional **occlusion** raycast and delivers only perceivable sounds to registered
`INoiseListener`s. The Milestone-7 enemy will simply subclass `NoiseListenerBase`; the entire
acoustic-stealth pillar is now standing and testable with the `DebugNoiseListener`.

*Last updated: Milestone 3.*
