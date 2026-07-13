# Milestone 8 — Vardø-9 Becomes a Horror Game

**Status:** ✅ Complete · **Philosophy:** player experience over framework. Everything here reuses
the existing systems; the new code is *content* — textures, sound, rooms, story.

The greybox is gone. Vardø-9 is now twelve hand-designed rooms with baked PSX textures, a full
procedural soundscape, environmental storytelling, and encounter design built around one idea:
**you play this game with your ears.**

---

## The station (floor plan)

```
            N
   ┌─────────────┬──────────────┐
   │  GENERATOR  │  RADIO ROOM  │        D3 = generator door (key-locked)
   │   [D3]      │[D4]  signal★ │        D4 = radio door (releases with power)
   ├──────┬──────┴──┬───────────┤
   │BATH- │ MAINTENANCE │ELEC-  │        corridor: red emergency light, pipes,
   │ROOM  │ CORRIDOR(red)│TRICAL│        the blood trail ends at D3
   ├──────┴─┬────arch───┬─┴─────┤
   │SLEEPING│           │FROZEN │        utility: broken window, snow inside,
   │QUARTERS│   MAIN    │UTILITY│        the fuse, ice cracking
   ├─(panic)│   HALL    ├───────┤
   │KITCHEN │(barricade)│STORAGE│        quarters: barricaded from inside;
   ├────────┴──arch─────┴───────┤        the key in Halvorsen's locker
   │        ENTRANCE            │
   └─────────[airlock]──────────┘        spawn: the airlock, facing a dark station
            S
```

**Progression:** duty log hints → key (quarters locker) → generator room [D3] → note: fuse first →
fuse (frozen utility) → electrical room panel → start the generator (**the loudest thing you will
ever do**) → power wakes the station, D4 releases → send the signal at the radio console — while
The Listener sweeps toward the roar you just made.

## Every room tells its part

| Room | The story it tells |
| --- | --- |
| Airlock | Frost creeping through the seals; the way out is frozen shut |
| Entrance | Coats still on their hooks — nobody left by the front door |
| Main Hall | Overturned table, scattered papers, a warning scratched into paint, and a blood trail leading **where you must go** |
| Kitchen | Meals abandoned mid-eat; the kettle that stood cold three weeks because no one dared the whistle |
| Quarters | The panic room: hall door boarded from inside, planks intact, blood beneath them — **the barricade held; it didn't matter** |
| Bathroom | Pitch black. A drip. A mirror your flashlight finds before you do. A battery, if you'll go in |
| Storage | Picked-over shelves; tin cans worth throwing |
| Frozen Utility | The outside coming in: broken window, drift, shards, a pipe burst frozen mid-spray — and the fuse |
| Corridor | The red artery: dying emergency lamps, pipes, tools, the trail's end smeared at the generator door |
| Electrical | Dead breaker panels and one amber pilot light waiting for a fuse |
| Generator | The machine, its hazard apron, and instructions that read like a dare |
| Radio Room | The operator's final log: *they answered the pattern, and it came up the path the signal went out* |

## The soundscape (all synthesized, all diegetic)

| Sound | Source | Heard by The Listener? |
| --- | --- | --- |
| Arctic wind bed + window howl | ambience | no — weather isn't a stimulus |
| Metal creaks, ice cracks, drips, distant thumps | `PeriodicNoiseEmitter`s | **yes** — the station itself tugs its patrols around |
| Your footsteps (concrete scuffs / metal ring on treadplate) | surface-aware `FootstepController` | **yes** — stance and floor decide how far |
| Doors groaning, locked rattles, bolt clunks | interaction | **yes** |
| Thrown cans clanging | `ImpactNoiseEmitter` | **yes** — your distraction tool |
| Fuse clunk, generator catch + running rumble, mains hum | progression | **yes** — the climax is deliberately deafening |
| Radio static | the goal, hissing quietly | no |
| **The rasp** — slow breathing, sub-growl, swells when hunting | The Listener | it **is** the Listener — your only read on where it is |
| State stingers (alerted / chase / strike / lost) | The Listener | your feedback loop |

## Encounter design (no scripted scares)

- **It keeps to the red corridor when calm** — the corridor you must cross four times.
- Its rasp is a 16 m 3D loop: you always know *roughly* where it is, never exactly. Silence is
  information; so is its absence.
- **When it breaks into a chase the station's electrics gutter** — your flashlight strobes
  (emergency-flicker modulator) exactly when you want it most. First chase shows one line of help.
- Ambient noises are real stimuli, so patrols drift unpredictably — the same run never plays twice.
- The generator start is the designed set-piece: an **Alarming** 30 m spike pulls it in for a sweep,
  but the steady hum is quiet/short-ranged, so it *leaves* again — you hide through the sweep, then
  slip to the radio door it just walked past. Getting caught costs position, not progress.

## What was added (all content-layer)

- **`ProceduralAudio`** (Nordo.Audio) — the entire soundscape synthesized at load, 22 kHz lo-fi.
- **`PSXTextureLib`** (Nordo.Rendering) — 13 baked 128×128 point-filtered textures in the palette.
- **`StationPropKit`** (Nordo.Level) — ~35 prop builders: furniture, machines, pipes, blood, snow,
  barricades, the Listener's silhouette figure.
- **`FlickeringLight`** (Nordo.Lighting) — dying lamps.
- **`StationEntranceBuilder`** — rebuilt: the twelve-room station above.
- **PSX shader** — emission term added, so interactables glow softly on focus.
- Small runtime audio setters on existing components (the hooks existed since M2–M6; now they sing).
- Footsteps are audible and **surface-aware** (metal floors ring differently — and carry further).

## How to verify (first 15 minutes)

1. Press Play. You wake in the **airlock**: wind, a frosted door behind you, one weak lamp ahead.
2. Read the duty log in the entrance. Cross the dark hall — flashlight on (**F**), slow.
3. Hear it before you see it: a rasp somewhere north, red light spilling from the corridor arch.
4. Kitchen → quarters: the barricade, the letter, the key in the open locker.
5. The bathroom dares you: pitch black, a drip, a battery.
6. Storage → frozen utility: wind screaming through the window; take the fuse; ice cracks behind you.
7. Corridor (metal underfoot now — louder): past the trail, into electrical; seat the fuse; hear the hum.
8. D3 with the key. Read the starter note. **Start the generator.** Lights wake everywhere; the roar
   summons it; the flashlight gutters when it charges.
9. Hold still until the sweep passes; through D4; read the operator's log; step to the console.
   **SIGNAL SENT.**

If that quarter-hour plays as written, Milestone 8 is verified.
