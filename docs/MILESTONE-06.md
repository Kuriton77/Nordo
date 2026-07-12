# Milestone 6 — The Listener AI

**Status:** ✅ Complete · **Depends on:** M1, M2, M3, M5

The first production-ready implementation of **The Listener** — a blind hunter driven *entirely* by
the acoustic system. No vision, no line-of-sight checks: every decision comes from what it hears and
where. Plus a runtime-built, fully playable test area to hunt (and be hunted) in.

---

## New assemblies

| Assembly | Depends on | Purpose |
| --- | --- | --- |
| `Nordo.Enemy` | `Nordo.Core`, `Nordo.Noise` | The Listener: FSM, config, memory, animator/audio hooks |
| `Nordo.VerticalSlice` | `Core`, `Noise`, `Enemy`, `Interaction`, `Lighting`, `Unity.AI.Navigation` | Runtime test-area builder + director/HUD |

`Nordo.Core` gained `ListenerStateId`, `IAutoDoor`, and the `PlayerCaughtEvent` /
`ListenerStateChangedEvent` bus events. `OpenableBase` now implements `IAutoDoor`.

---

## Scripts — what each does

### Core
- **`ListenerStateId`** — the shared FSM state enum (so UI/events refer to states without depending on the enemy).
- **`IAutoDoor`** — a door the AI can push open; locked doors report `CanBeOpenedByAI == false` (safe rooms).
- **`PlayerCaughtEvent` / `ListenerStateChangedEvent`** — bus events for the game-flow and HUD/audio.

### Enemy
- **`ListenerConfig`** — all difficulty/behaviour params (speeds, suspicion, thresholds, attack, nav). Auto-defaulted if unassigned.
- **`ListenerBlackboard`** — short-term memory: last heard position/time/loudness, home anchor.
- **`IListenerState`** + the six states (**Patrol, Investigate, Search, Chase, Attack, Return**) — one focused class each.
- **`PatrolRoute`** — ordered waypoints (with child-transform fallback and runtime assignment).
- **`IListenerAnimator` / `ListenerAnimator`** — Animator contract (`Speed` float, `State` int, `Attack` trigger); null-safe for greybox.
- **`ListenerAudio`** — presence loop (scales with aggression) + transition stingers; the player's only read on an unseen creature.
- **`ListenerController`** — the brain: `NoiseListenerBase` + NavMeshAgent, owns suspicion/memory, ticks the FSM, opens doors, draws debug gizmos.

### Vertical Slice
- **`VerticalSliceBuilder`** — builds the whole test area at runtime, bakes a NavMesh, wires everything.
- **`ExitZone`** — trigger that fires the escape.
- **`VerticalSliceDirector`** — objective HUD, live Listener state + suspicion meter, caught→respawn, escaped→win.

---

## How The Listener thinks (all hearing, no sight)

1. Every noise it can perceive (via `NoiseSystem`) calls `OnHeardNoise`, which **records the position**
   in the blackboard and raises **suspicion** (scaled by perceived loudness × priority).
2. Suspicion **decays** every second, so old sounds fade from attention.
3. **Patrol** → suspicion ≥ *InvestigateThreshold* → **Investigate** (walk to the exact sound).
4. Suspicion ≥ *ChaseThreshold* → **Chase** (run to the continuously-updated last-heard spot).
5. In range → **Attack** (wind-up → strike → `PlayerCaughtEvent`).
6. Player silent past *LoseTargetTime* → **Search** (sweep the area) → timeout → **Return** → **Patrol**.

The core fantasy: **noise summons it, silence loses it.** Crouch-walk to stay near-silent; sprinting,
throwing, slamming doors, and running generators are all beacons on the same channel.

---

## How to test — the playable area

### Setup
1. Open an (otherwise empty) scene. Ensure your **player rig** (from Milestones 1–4) is present,
   **tagged `Player`**, with a **CharacterController** and its footstep/breath components (so it emits
   noise). It also needs a URP camera — your existing rig has this.
2. Create an empty GameObject and add **`VerticalSliceBuilder`**. Leave *Build On Start* ticked.
3. Press **Play**. The builder generates two rooms, a door, a table with throwables, a hiding nook, the
   patrolling Listener, an exit, and the HUD; it teleports your player to the spawn and bakes a NavMesh.

> The HUD (top-left) shows the objective, the Listener's current **state**, and a live **suspicion**
> meter. Keep the **Scene view** open too — the Listener draws its hearing range, memory, and path.

### The seven required player verbs
| Verb | How | What The Listener does |
| --- | --- | --- |
| **Walk quietly** | Crouch-walk (Ctrl) | Minimal noise; it mostly stays on patrol |
| **Sprint** | Shift | Loud footfalls; suspicion spikes → it investigates/chases |
| **Throw objects** | Grab a throwable (E), then E to throw | The impact noise pulls it to that spot (distraction) |
| **Open doors** | Look at the door, E | The door-open noise is audible; the enemy also opens doors itself in pursuit |
| **Hide** | Duck into the nook and **stay silent** | Chase → Search → Return; it loses you when you go quiet |
| **Distract** | Throw a prop away from your route | It investigates the noise while you slip past |
| **Escape** | Reach the green **Exit** in the far room | HUD shows the win; the Listener stands down |

### Suggested run
Start in the south room. Watch the HUD state read **Patrol**. Sprint once — see suspicion jump and the
state flip to **Investigate**/**Chase**, and the Scene-view line snap to where you were. Duck into the
nook and stay still — watch it go **Search** then **Return** to **Patrol**. Now throw a bottle toward
the west wall to bait it, cross through the door, and walk (don't sprint) to the **Exit**.

### Difficulty
Create a **ListenerConfig** asset (*Create → Nordo → Enemy → Listener Config*) and assign it on the
`TheListener` object (or edit the runtime default's values) to make Easy/Normal/Hard variants —
hearing range, speeds, thresholds, and the all-important *LoseTargetTime* window.

### Automated tests
The prior EditMode suites (EventBus, CameraEffectSample, NoiseStimulus, Battery, **NoiseAttenuation**)
still pass; the acoustic model the AI relies on is unit-tested. The AI itself is validated through the
playable slice above.

If all seven verbs behave as described and you can be caught **and** escape, **Milestone 6 is verified.**

---

## Design notes & decisions
- **Genuinely blind.** There is no camera, raycast-to-player, or angle check anywhere in the AI —
  only `OnHeardNoise` and the last-heard position. The stealth is honest.
- **State objects, not a switch.** Each behaviour is an `IListenerState` class sharing the controller
  as context — easy to read, extend (add a "flee" or "ambush" state), and reason about.
- **Composition for presentation.** Animator and audio are behind interfaces/components, so the
  creature runs headless as a capsule now and gains its skin/voice later with zero AI changes.
- **Runtime test area over a hand-authored scene.** Building the slice in code keeps it in version
  control as readable C# (no fragile binary `.unity` diffs) and guarantees it matches the current APIs.
- **Difficulty as data.** One `ListenerConfig` asset retunes the entire hunt.

## Next: the rest of the vertical slice
Inventory & puzzles (keys/fuses/generators — the `MachineNoiseEmitter` is ready), objectives, menus,
and save/load, assembled into one finished, finishable level.
*(Awaiting approval before the next milestone.)*
