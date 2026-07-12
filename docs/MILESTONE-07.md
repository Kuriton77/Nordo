# Milestone 7 — Inventory, Objectives & Puzzle Progression (the first section)

**Status:** ✅ Complete · **Depends on:** M1–M6

A complete, finishable gameplay progression and the **first designed section of Vardø-9**. The player
enters, explores, collects a key and a fuse, restores power, avoids The Listener, and escapes to the
transmitter — a full start-to-finish loop, built to the locked PSX art direction.

---

## New assemblies

| Assembly | Depends on | Purpose |
| --- | --- | --- |
| `Nordo.Progression` | Core, Items, Interaction, Noise | Inventory, objectives, power grid, fuse/generator, notes |
| `Nordo.UI` | Core, Items, Progression, InputSystem | Station HUD, inventory, prompt, toasts, logbook |
| `Nordo.Level` | Core, Noise, Enemy, Interaction, Lighting, Items, Progression, UI, Rendering, AI.Navigation | The Vardø-9 section builder + director |

Core gained `IInventory`, `IObjectiveService`, and gameplay events (inventory / objective / power /
note / message). Existing components gained runtime-config APIs (`Lockable.Configure`/`TryAutoUnlock`,
`ItemPickup.SetItem`, `ItemDefinition.CreateRuntime`).

---

## Scripts — what each does

### Core
- **`IInventory` / `IObjectiveService`** — the query/consume and progression surfaces puzzle logic and
  UI depend on (never the concrete classes).
- **Gameplay events** — `InventoryChangedEvent`, `ObjectiveChangedEvent`, `PowerStateChangedEvent`,
  `NoteReadEvent`, `GameMessageEvent`.

### Progression
- **`InventoryService`** — stores item stacks (fed by `ItemPickedUpEvent`), implements `IInventory`,
  raises change/message events. Registered service.
- **`ObjectiveTracker` (+ `ObjectiveStep`)** — the multi-stage chain; `CompleteObjective(id)` advances.
- **`PowerController`** — a named grid; `RestorePower()` broadcasts `PowerStateChangedEvent`.
- **`PoweredDevice`** → **`PoweredLight`** (lights wake with power), **`PoweredLock`** (door releases
  with power).
- **`FuseBox`** — consumes a fuse from the inventory to power itself (a generator prerequisite).
- **`Generator`** — needs the fuse box powered; starting it kicks the `MachineNoiseEmitter` (loud!) and
  raises `Started` (wired to restore power + advance the objective).
- **`ReadableNote`** — environmental storytelling; raises `NoteReadEvent` (logged + shown).
- **`Lockable`** (updated) — now `TryAutoUnlock` from the inventory, so the right key opens a door.

### UI
- **`StationHUD`** — cold-styled IMGUI: current objective, inventory, interaction prompt, feedback
  toasts, a modal note reader, and a **[J] field logbook** (objectives + recovered notes). Event-driven
  and swappable.

### Level
- **`StationEntranceBuilder`** — builds the section, bakes a carving-door NavMesh, places items/notes/
  puzzles, wires the dependency chain, spawns The Listener, and sets up the HUD/director.
- **`SectionExit`** — the finish trigger. **`StationDirector`** — respawn-on-caught + the ending.

---

## How to play the first section

### Setup
1. Open an empty scene. Ensure your **player rig** (Milestones 1–4) is present, **tagged `Player`**,
   with a **CharacterController**, its footstep/breath components, and a **flashlight** (you'll need it
   — the station is dark and fogged).
2. Add an empty GameObject → **`StationEntranceBuilder`** (leave *Build On Start* on).
3. Press **Play**. The section assembles, fog rolls in, the HUD appears, and you spawn in the entry
   airlock.

### The intended run (start → finish)
1. **Explore.** The first objective reads *"Find a way into the control room."* Use your flashlight;
   move slow — footsteps are quiet when you walk/crouch, loud when you sprint.
2. **Read notes** (press E on the pale pages). They tell you: there's a **key** in an office, a **fuse**
   in storage, and that *"it hears, it doesn't see."*
3. **Collect the key** (west office desk) and the **fuse** (east storage shelf) — watch them appear in
   the bottom-left **inventory**. Open the **[J] logbook** any time to review objectives + notes.
4. **Unlock the control room** (north door): with the key held, press E on it — it consumes the key and
   opens. Objective advances to *"Restore power…"*.
5. **Insert the fuse** into the **fuse box**, then **start the generator**. The generator ROARS — a
   huge noise that will pull The Listener toward you. Power is restored: **lights wake across the
   station** and the **transmitter door releases**.
6. **Escape.** Reach the transmitter room (far north) past the now-active hunter. The HUD shows
   **SECTION COMPLETE** and the ending line.

### Avoiding The Listener (integration)
- It's **blind** — it hunts your noise. After the generator, **walk/crouch**, use doors and distance,
  and let it investigate the generator's din while you slip to the exit.
- **Locked rooms keep it out**: the NavMesh doors carve dynamically, so a closed door genuinely blocks
  it until opened.
- If it catches you, you **respawn at the entry** and it resets — try a quieter route.

### What to verify
- [ ] Objective text advances through all three stages; logbook lists them (✓/▶/•) and recovered notes.
- [ ] Key and fuse appear in the inventory; the key opens the control door (and is consumed).
- [ ] Fuse box rejects you with "you need a fuse" until you have one; generator rejects until the fuse
      box is powered.
- [ ] Starting the generator restores power → lights turn on → exit door unlocks.
- [ ] The generator's noise visibly draws The Listener (watch its gizmos / the tension).
- [ ] Reaching the transmitter shows SECTION COMPLETE; being caught respawns you at the entry.
- [ ] Everything looks PSX: fogged, cold, low-poly, few hard-shadowed lights (art direction holds).

### Automated tests
All prior EditMode suites (EventBus, CameraEffectSample, NoiseStimulus, Battery, NoiseAttenuation)
still pass. The progression/level are validated by playing the section above.

If the full loop completes, **Milestone 7 is verified** — Nordo has its first finishable section.

---

## Design notes
- **Interconnected by dependency, not scripting.** Key→door, fuse→box, box→generator, generator→power,
  power→lights+exit: each link is a small component reacting to an event, so the chain is readable and
  re-composable, and the builder just wires the ends together.
- **The UI is decoupled.** IMGUI now (guaranteed to render over a runtime-built level), but every panel
  reads from events/services — a diegetic world-space pass can replace it with zero gameplay changes.
- **Doors that matter to the AI.** Carving NavMesh obstacles make a locked door a real barrier to a
  blind hunter, not just a visual — the stealth and the puzzle share the same door.
- **Loudest at the climax.** Restoring power is deliberately the noisiest act, tying the puzzle payoff
  directly to the stealth threat.

## Next: Milestone 8 — deepen the puzzles & polish the section
Combination locks, a hidden passage, more of Vardø-9, and a full audio/lighting pass — turning this
first section into a vertical slice with real production polish.
*(Awaiting approval.)*
