# Opening NORDO in Unity

This repository is a **complete Unity 2022.3 LTS project**. Unity Hub detects it directly and it
opens to a playable startup scene with no manual reconstruction.

## Open it

1. **Clone** the repo (or add it in Unity Hub → *Add project from disk* pointing at the repo root).
2. Unity Hub reads `ProjectSettings/ProjectVersion.txt` and offers to open with **Unity 2022.3 LTS**
   (install `2022.3.x` if prompted — any 2022.3 patch works).
3. On first open Unity resolves the packages in `Packages/manifest.json` (URP, Input System, TMP,
   Addressables, AI Navigation, Cinemachine, Test Framework) and generates its `Library/` cache and
   any missing `.meta` files. This can take a few minutes the first time.
4. Open **`Assets/_Project/Scenes/Bootstrap.unity`** (it is also the only scene in Build Settings) and
   press **Play**.

## What happens on Play

The scene contains a single **`GameBootstrap`** object. On Play it:

1. Builds a complete first-person **player rig in code** (`PlayerRigFactory`) — CharacterController,
   movement/look/crouch/sprint, camera-feel stack, flashlight, footsteps, breath, interaction, and
   held-object/inspection — wired to controls built at runtime from the embedded NordoControls map.
   The rig is tagged **`Player`**.
2. Builds the first **Vardø-9 section** (`StationEntranceBuilder`) around the player: rooms, a locked
   control room, a fuse box, a generator, a powered exit, keys, notes, lights, and The Listener, then
   bakes a NavMesh and wires the objective/puzzle chain.

So: clone → open → press Play → you're in the station. See `docs/MILESTONE-07.md` for the full
play-through (collect the key, find the fuse, restore power, escape — quietly).

## Project layout

```
Nordo/
├─ Assets/_Project/     # all game content (scripts, scenes, shaders, settings)
├─ Packages/manifest.json   # package dependencies (URP, Input System, …)
├─ ProjectSettings/     # full Unity project configuration (tracked in git)
└─ docs/                # design & engineering docs (incl. ART_DIRECTION.md)
```

## Notes

- **Render pipeline:** the project opens on the **Built-in** pipeline by default; the runtime level
  builders detect the active pipeline and use Built-in-compatible materials, so nothing renders
  magenta. To get the full **PSX look** (`Nordo/PSX` shader), create a URP asset
  (*Assets → Create → Rendering → URP Asset*) and assign it in *Project Settings → Graphics* and
  *Quality*. Fog and the cold palette work under both pipelines (they're driven by `SceneAtmosphere`).
- **Input:** the New Input System is enabled (`activeInputHandler: 2`), so no "enable backends" prompt.
- **Providing your own player:** turn off *Build Player Rig* on `GameBootstrap` and place your own
  `Player`-tagged rig with a `CharacterController` in the scene instead.
- **Serialization** is Force Text (git-friendly).
