# NORDO — Pre-Milestone-8 Repository Audit

**Date:** 2026-07-12 · **Scope:** full repository · **Verdict after fixes:** stable, cleared for content work.

Method: exhaustive static resolution analysis (cross-assembly access, asmdef reference coverage,
namespace/using resolution, shader syntax & include paths, YAML structure/duplicates, GUID linkage,
2022.3 API surface, runtime-order reasoning). Unity is not installable in this environment, so
findings marked *(editor-verify)* need one confirmation pass in the editor.

---

## Findings

### CRITICAL — fixed in this commit

| # | Finding | Detail | Fix |
| --- | --- | --- | --- |
| C1 | **Cross-assembly `internal` access → CS0122 compile error** | `EventBusTests` (assembly `Nordo.Tests.EditMode`) called `EventBus<T>.Clear()`, which was `internal` to `Nordo.Core`. Editor-assembly compile errors block compilation → Safe Mode. | `Clear()` made `public` (documented as the test/hard-reset hook). |
| C2 | **Invalid HLSL in `Nordo_CRT.shader`** | `CurveUV(input.texcoord, out float mask)` — C#-style inline `out` declaration is not legal HLSL → shader compile error in the console. | Declared `float mask;` before the call. |
| C3 | **Wrong shader include path** | `#include ".../render-pipelines.universal/ShaderLibrary/Blit.hlsl"` — that file doesn't exist in URP 14; `Blit.hlsl` lives in `com.unity.render-pipelines.core/Runtime/Utilities/`. → include-not-found console error. | Corrected path. |

### HIGH — fixed in this commit

| # | Finding | Detail | Fix |
| --- | --- | --- | --- |
| H1 | **No render-pipeline asset assigned → locked PSX art direction never renders** | `GraphicsSettings` had no URP asset, so the project ran Built-in and `Nordo/PSX` was inactive (builders fell back to Standard). The art direction is LOCKED; it must actually be on screen. | New `Nordo.Editor` (editor-only) `UrpAutoSetup`: on first load, if no pipeline is assigned, creates `Assets/_Project/Settings/URP/` renderer + pipeline assets and assigns them. Idempotent; logs once. *(editor-verify)* |

### MEDIUM — accepted / deferred with rationale

| # | Finding | Detail | Disposition |
| --- | --- | --- | --- |
| M1 | Most `.meta` files not committed | Unity generates them on first import with fresh GUIDs per clone. Impact is low **because all wiring is runtime-code**, and the only two GUID-pinned files (bootstrap scene + `GameBootstrap.cs`) have committed metas (linkage verified byte-for-byte). | After the next editor session, commit the generated metas (documented in PROJECT_SETUP). |
| M2 | `Highlighter` emission has no effect on `Nordo/PSX` materials | The PSX shader has no `_EmissionColor` property, so focus-highlight is invisible on PSX-shaded props (prompt text still shows). | Fold into M8 polish (add emission term to PSX shader) — a player-experience item, done with content. |
| M3 | Station section has no throwable props | The distraction verb (grab/throw) exists and works, but the first section places no throwables — a gameplay gap, not a defect. | M8 content (encounter design). |
| M4 | `StationHUD` is IMGUI | Deliberate (renders over a fully runtime-built level, zero asset deps); event-driven so swappable. | Revisit for diegetic UI in polish. |

### LOW — fixed or noted

| # | Finding | Disposition |
| --- | --- | --- |
| L1 | Unused `com.unity.postprocessing` package (URP has its own post) | **Removed** from manifest. |
| L2 | NavMesh covers small unreachable floor regions outside rooms | Cosmetic; no pathing risk (verified all AI destinations are in-room). |
| L3 | `ProjectVersion.txt` pins `2022.3.62f1`; other 2022.3 patches show a version-switch prompt | Normal Unity behaviour; any 2022.3.x works. |
| L4 | TMP package present but unused at runtime (presenter only for hand-built scenes) | Harmless; kept for future UI. |

### Verified clean (no findings)

- **Assembly references:** every `using Nordo.*` in all 19 assemblies is covered by its asmdef; graph acyclic; `Unity.InputSystem` / `Unity.TextMeshPro` / `Unity.AI.Navigation` references present where used.
- **Namespaces/usings:** no unresolved `EventBus`/`ServiceLocator`/`UnityEngine.Rendering`/`System` usages (full sweep re-run post-fix).
- **Unity 2022.3 API surface:** no Unity-6 APIs (`linearVelocity`, `FindFirstObjectByType`, `PhysicsMaterial`), no obsolete APIs (`WWW`, `LoadLevel`, component shortcuts). `velocity`, `PhysicMaterial`, `innerSpotAngle`, `InputActionAsset.FromJson` all valid.
- **Serialization:** all inspector types are `[Serializable]`; SO defaults safe; no `[SerializeReference]` risks.
- **Scene & GUIDs:** `Bootstrap.unity` parses (7 docs); MonoBehaviour → `GameBootstrap.cs.meta` GUID **match**; EditorBuildSettings → scene meta GUID **match**; both GUIDs valid 32-hex.
- **ProjectSettings:** all 13 files parse; no duplicate YAML keys; no tabs; New Input System active (`activeInputHandler: 2` = Both — legacy-safe); Force-Text serialization; quality levels sane.
- **Build Settings:** exactly one scene (Bootstrap), enabled.
- **NavMesh:** runtime bake via `NavMeshSurface` (package present); doorways (3 m) ≫ agent radius (0.5); door leaves carve; enemy spawn/waypoints sampled onto the mesh; bake order (player excluded → bake → enemy) correct.
- **Input:** embedded controls JSON schema matches `.inputactions` format; reader initialization order safe.
- **Runtime order:** service creation precedes consumers (`DefaultExecutionOrder` −100/−60/−50); build-inactive→wire→activate pattern means every `Awake` sees its dependencies; `AddComponent` ordering (Lockable before Door) verified.
- **Tags/layers:** only built-in tags used (`Player`, `MainCamera`) — canonical TagManager is sufficient.
- **Console-error risks:** every builder/controller null-guards missing optional pieces (audio clips, animator, palette) with warnings, not exceptions.

---

## Residual risk (honest)

Static analysis can't execute UnityYAML importers or the shader compiler. The three places that most
warrant one live confirmation: (1) first-import of the hand-authored ProjectSettings, (2) `Nordo/PSX`
+ `Nordo/CRT` shader compile, (3) `UrpAutoSetup` first run. All are low-complexity and were checked
against URP 14 / 2022.3 documented APIs.

---

## Checklists

### Compile checklist
- [x] No cross-assembly `internal` access (C1 fixed; repo-wide sweep clean)
- [x] All `using` directives resolve; asmdef references cover every dependency
- [x] No Unity-6/obsolete APIs; 2022.3 surface only
- [x] Shaders: valid HLSL, valid include paths (C2/C3 fixed)
- [x] All C#/shader braces balanced; all asmdef/JSON/YAML parse

### Unity verification checklist (run in editor)
- [ ] Open project → **no Safe Mode**, Console shows **0 errors** after import
- [ ] `[Nordo] URP pipeline created & assigned` appears once; `Nordo/PSX` renders (no magenta)
- [ ] Test Runner → EditMode → **all suites pass** (EventBus, CameraEffectSample, NoiseStimulus, Battery, NoiseAttenuation)
- [ ] Build Settings shows `Bootstrap.unity`; Play from it enters Play Mode with 0 errors
- [ ] Commit Unity-generated `.meta` files + `GraphicsSettings.asset` after first session

### Gameplay checklist (Play Mode)
- [ ] Spawn in the entry; WASD/mouse/crouch/sprint/jump work; flashlight on **F**
- [ ] HUD shows objective, inventory, prompts; **J** opens the logbook
- [ ] Key opens control room; fuse → fuse box → generator → power restored → lights wake → exit unlocks
- [ ] Generator roar visibly draws The Listener; going silent loses it; being caught respawns you
- [ ] Reaching the transmitter shows SECTION COMPLETE

*Next (on approval): Milestone 8 — production-quality horror pass on the slice (level design, encounters, atmosphere, pacing) reusing existing systems only.*
