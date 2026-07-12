# NORDO — Art Direction (LOCKED)

> **This document is binding.** Every future environment, prop, shader, material, lighting setup,
> particle, and post-effect in Nordo must comply. If a change would violate a rule here, the rule
> wins — change the asset, not the rule. Realistic / photorealistic assets are **prohibited**.

**Aesthetic:** *Modern PSX horror* — the chunky, affine-warped, fog-drowned look of late-90s
survival horror, rebuilt with today's lighting, shadows and post-processing discipline.

---

## 1. The Ten Locked Rules

| # | Rule | Concrete spec |
| --- | --- | --- |
| 1 | **Low-poly environments** | Rooms/structures built from hard, faceted geometry. Target **≤ ~300 tris** per modular wall/floor piece; no smoothing groups faking curvature. |
| 2 | **Low-poly props** | **50–800 tris** per prop. Silhouette carries the read, not surface detail. |
| 3 | **Low-resolution textures** | **128–256 px** albedo. **Point (nearest) filtering**, no mipmaps, no anisotropic. No 512+ textures. |
| 4 | **Heavy atmospheric fog** | Fog **always on**. Cold colour, dense — visibility roughly **8–22 m** depending on space. Fog is the primary depth cue and the reason light is scarce. |
| 5 | **Cold blue-grey palette** | Desaturated blues/greys/teals. Warmth only as a rare, deliberate accent (a flashlight, a warning light). See §3. |
| 6 | **Harsh shadows** | Hard-edged shadows, high contrast, crushed blacks. Quantised/banded diffuse — no soft gradient shading. |
| 7 | **Limited light sources** | **A scene is mostly dark.** Prefer 1 weak directional/ambient + a handful of local lights. The flashlight is the player's main light. Never flood a room. |
| 8 | **Slight vertex jitter** | Vertex-snapping ("wobble") where it reads well — environments and static props. Subtle, not seasick. Skinned characters may reduce or disable it. |
| 9 | **Optional CRT / VHS** | A toggleable full-screen pass: scanlines, mild chromatic aberration, grain, slight curvature/vignette. Off by default; player-selectable. |
| 10 | **Strong silhouette readability** | Every prop/enemy must read as a black shape on the fog. Design silhouettes first; interactables must be distinguishable in near-dark. |

Plus the overarching constraint: **Performance-first rendering.** The retro look is also a performance
budget — unlit/vertex-lit where possible, few real-time shadow casters, no post-stack bloat, mobile-
friendly. If a technique is pretty but expensive, it loses.

---

## 2. How to comply (the provided tooling)

The look is not a suggestion baked into memory — it ships as reusable tools under
`Assets/_Project/Scripts/Rendering/` and `Assets/_Project/Art/Shaders/`. **Use these; do not invent
parallel ones.**

- **`Nordo/PSX` shader** (`Art/Shaders/Nordo_PSX.shader`) — the default material shader for **all**
  environments and props. Provides: vertex snapping (rule 8), affine texture warp, banded/harsh
  lighting (rule 6) from the main light + local lights (so the flashlight works), ambient tint,
  alpha-cutout, and fog. Set textures to **Point filter, no mipmaps** in their import settings.
- **`Nordo/CRT` shader** (`Art/Shaders/Nordo_CRT.shader`) — the optional full-screen CRT/VHS pass
  (rule 9). Add via a URP **Full Screen Pass Renderer Feature** with a material using this shader;
  expose the toggle in the options menu.
- **`PSXPalette`** (ScriptableObject) — the single source of truth for the **locked palette**, fog
  colour/density, and ambient (rules 4–5). One authoritative asset; reference it everywhere.
- **`SceneAtmosphere`** (component) — applies a `PSXPalette`'s fog + ambient to a scene on load, so
  every level is fogged and colour-graded consistently (rules 4, 5, 7). **Every playable scene must
  have one.**

### Material rules
- Environments/props → material using **`Nordo/PSX`**. Never URP/Lit "realistic" materials for world
  content.
- Albedo textures: **128–256 px**, **Point** filter, **mipmaps off**, compression low/none for the
  crunchy look.
- No normal maps, no metallic/smoothness workflow, no parallax, no SSAO-driven realism. Detail comes
  from vertex colour, cheap albedo, and lighting contrast.

### Lighting rules
- One dim directional or pure ambient as base; keep rooms **dark**.
- Local lights are few, small-range, and motivated (a flickering bulb, a monitor glow, the flashlight).
- Real-time shadows: **hard**, from a limited number of casters. Crush the blacks; do not lift shadows.
- Colour temperature cold by default; warm lights are rare narrative accents.

### VFX rules
- Particles: low count, additive or cutout, cold-tinted (breath fog is the sanctioned exception and is
  near-white). No lush volumetrics beyond the fog and simple light shafts.
- Screen effects live in the CRT pass only; no per-object glossy post.

---

## 3. The Locked Palette

Cold blue-grey core; warmth is an *event*, not a default. (Author these into the `PSXPalette` asset;
values are sRGB hex starting points.)

| Role | Hex | Use |
| --- | --- | --- |
| Void / crushed black | `#0A0C10` | Shadows, backgrounds, silhouettes |
| Deep steel | `#141A22` | Primary structure / walls |
| Slate | `#232B34` | Floors, large surfaces |
| Cold grey | `#3A444D` | Props, trim |
| Ice highlight | `#6E7C86` | Edges catching light |
| Fog blue-grey | `#39434E` | **Fog colour** (also ambient) |
| Sickly teal | `#2C4A47` | Accents, corrosion, screens |
| **Warning amber** (accent) | `#C8791E` | Rare: hazard lights, warm bulbs |
| **Signal red** (accent) | `#8E2B2B` | Rare: alarms, danger, blood-dark |

Rule of thumb: **≥ 90%** of any frame sits in the cold column; accents are single, small, and meaningful.

---

## 4. Compliance checklist (use on every asset/scene)

- [ ] Geometry is low-poly and faceted; within the tri budgets (rules 1–2).
- [ ] Textures are 128–256 px, Point filter, no mipmaps (rule 3).
- [ ] Material uses `Nordo/PSX` (not a realistic shader).
- [ ] Scene has a `SceneAtmosphere` with the project `PSXPalette`; fog is on and cold (rules 4–5).
- [ ] Lighting is dark and limited; shadows are hard; blacks are crushed (rules 6–7).
- [ ] Vertex jitter present where appropriate (rule 8).
- [ ] Silhouette reads clearly in near-dark fog (rule 10).
- [ ] Palette sits in the cold column; ≤ one deliberate warm/red accent (rule 5, §3).
- [ ] No photorealism, no normal/metallic realism, no heavy post beyond the optional CRT.
- [ ] Runs cheap: few shadow casters, low particle counts, no per-object post (perf-first).

If any box is unchecked, the asset is **not** shippable.

---

*Status: LOCKED at the start of the vertical-slice content phase. Supersedes any conflicting visual
choice in code, prefabs, or scenes.*
