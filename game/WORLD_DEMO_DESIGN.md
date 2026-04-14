# World Demo — Technical Design Document

**Author:** Tim  
**Engine:** Unity 2022.3 LTS (Universal Render Pipeline)  
**Original Engine:** Godot 4.6.2 (GL Compatibility / WebGL)  
**Target:** Browser (WebGL), Desktop  
**Last updated:** April 2026

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Terrain System](#terrain-system)
3. [River & Water](#river--water)
4. [Foliage System](#foliage-system)
5. [Tree Generation](#tree-generation)
6. [Grass Rendering](#grass-rendering)
7. [Shader Architecture](#shader-architecture)
8. [Day/Night Cycle](#daynight-cycle)
9. [Player Controller](#player-controller)
10. [Scene Graph & Wiring](#scene-graph--wiring)
11. [Performance Considerations](#performance-considerations)
12. [Porting Notes (Unity)](#porting-notes-unity)

---

## Architecture Overview

The world is a **single 40×40 unit outdoor environment** divided into four biome quadrants (Meadow NW, Forest NE, Rocky Hills SW, Pond SE), built entirely from procedural generation at load time. There are zero imported 3D models and zero texture files for geometry — everything is constructed from code: terrain meshes, tree meshes, grass blades, flowers, water surfaces, and collision shapes.

### Core Design Principles

- **Single source of truth for terrain:** A static `WorldData` class holds all height, river, and spawn logic. Every system (terrain mesh, water, foliage placement, player physics) queries this same API.
- **Deterministic seeding:** Each foliage type has a fixed hex seed (`0x54524545` = "TREE", `0x47524153` = "GRAS", etc.) so the world is identical every run.
- **GPU-heavy rendering:** Detail comes from shaders, not geometry. Terrain uses vertex colors + shader noise for surface breakup. Grass uses per-instance wind animation in the vertex shader.
- **MultiMesh for mass instancing:** 50,000 grass blades, 3,000 wildflowers, and 30 rocks all use `MultiMesh` — one draw call per type.

### System Dependency Graph

```
WorldData (static height/river API)
├── VoxelChunk (terrain mesh + collision + water mesh)
├── FoliagePlacer
│   ├── Grass Impostor (low-res ground plane)
│   ├── Grass Blades (50k MultiMesh)
│   ├── Trees (TreeGenerator → MeshInstance3D per tree)
│   ├── Rocks (MultiMesh)
│   └── Wildflowers (MultiMesh, per-instance color)
├── PlayerController (ground/swim detection)
└── PerimeterWalls (invisible collision box)

DayNight → DirectionalLight3D + ProceduralSkyMaterial + Environment
```

---

## Terrain System

### Height Function

The terrain is a continuous analytical height function — no heightmap texture. This keeps it resolution-independent and lets any system sample height at arbitrary precision.

```
height_smooth(fx, fz) =
    base_meadow(fx, fz)     // gentle rolling hills from summed sinusoids
  + ridge_mask(fx, fz)      // east-side ridge elevation boost
  - river_carve(fx, fz)     // river bed depression
```

**Base meadow:** Sum of `sin`/`cos` at different frequencies and amplitudes to create gentle undulation. Approximately 4-5 overlapping waves.

**Ridge mask:** Uses a custom `_smoothstep` on normalized `fx` and `fz` to raise the eastern terrain edge, creating a natural ridge/hillside boundary.

**River carve:** Computed from a 1D signed distance field (SDF). The river center follows `x = 5 + sin(z * 0.35) * 0.6` — a gentle S-curve down the Z axis. Within `RIVER_HALF` (1.6 units) of center, terrain smoothly blends down toward `RIVER_BED_Y` (2.2) using smoothstep on the bank edges.

### Key Constants

| Constant | Value | Purpose |
|----------|-------|---------|
| `SIZE` | 16 | World width/depth in units |
| `GRID` | 64 | Mesh resolution (64×64 vertices) |
| `STEP` | 0.25 | Distance between grid vertices (SIZE/GRID) |
| `WATER_Y` | 3.2 | Water surface height |
| `RIVER_HALF` | 1.6 | River half-width |
| `RIVER_BED_Y` | 2.2 | Deepest point of river floor |

### Mesh Generation (VoxelChunk)

Despite the name "voxel," this is a **heightmap mesh**. The chunk generates a single `ArrayMesh` by:

1. Sampling `WorldData.height_smooth` at each grid intersection → `(GRID+1)²` height values
2. Computing per-vertex normals via **finite differences** (sample height ±epsilon in X and Z, cross product)
3. Assigning **vertex colors** based on slope: flat = grass green, moderate = dirt brown, steep = rock gray. These blend via thresholds — the terrain shader reads vertex color to mix materials.
4. Building triangle indices (two tris per quad)
5. Adding **skirt geometry**: four edge strips dropping to Y=0 to hide the void underneath the terrain

**Collision:** A `ConcavePolygonShape3D` is generated from the same triangle data, with `backface_collision = true` so the player can't fall through from any direction.

### Trade-offs

- **Analytical height** is cheap to evaluate but limits terrain complexity (no overhangs, caves, true voxels). Sufficient for a meadow/river scene.
- **64×64 grid** is a balance between mesh density and vertex count. Higher resolution would show smoother river banks but costs more triangles.
- **Vertex colors for material blending** avoids texture splatmaps but limits to 3-4 material types (RGBA channels).

---

## River & Water

### River Shape

The river is defined by a 1D SDF in world space:

```
river_center_x(z) = 5.0 + sin(z * 0.35) * 0.6
river_sdf(x, z) = abs(x - river_center_x(z))
is_river(x, z) = river_sdf < RIVER_HALF * 0.7 AND z in (1.5, 14.5)
```

The `0.7` multiplier on `is_river` creates a narrower "definitely river" zone vs the full carved bank width, used for placement exclusion (foliage, grass).

### Water Mesh

Water is **not** a single full-size plane. It's generated as sparse quads: a 48×48 grid where each cell is only emitted if:
- Terrain height at that cell < `WATER_Y + 0.05` (terrain is below water)
- Z coordinate is between 1.0 and 15.0 (within the river corridor)

This saves fill rate by not rendering water under dry terrain.

### Water Shader

The water shader combines several techniques for a convincing surface:

- **Vertex displacement:** Sum-of-sines + noise for wave height animation
- **Ripple normals:** Finite-difference gradient of the wave function, blended with geometric normal at 70% strength
- **Fresnel:** `pow(1 - abs(dot(VIEW, NORMAL)), 3)` drives shallow/deep color mix
- **Foam:** High-frequency noise creates white foam patches at wave peaks (`smoothstep(0.72, 0.82, noise)`)
- **Transparency:** Base alpha 0.72, boosted by fresnel up to 0.92 max
- **Properties:** Low roughness (0.05), high specular (0.7), slight metallic (0.15), subtle emission for underwater glow

---

## Foliage System

All foliage is placed in `_ready` via `call_deferred("_place_all")` to avoid blocking the scene load. Placement order matters: impostor → trees → rocks → grass → flowers.

### Placement Rules

Every foliage type checks:
1. `WorldData.is_river(x, z)` → skip if in river
2. `WorldData.river_sdf(x, z)` → skip if too close to river bank (threshold varies: 0.4 for grass, 0.8 for flowers, 1.2 for trees)
3. Height is sampled from `WorldData.height_smooth` for Y positioning

### Instance Counts

| Type | Count | Seed | Mesh Type |
|------|-------|------|-----------|
| Trees | 22 attempts | `0x54524545` | Individual `MeshInstance3D` per tree |
| Rocks | 30 attempts | `0x524f434b` | `MultiMesh` (shared sphere mesh) |
| Grass blades | 50,000 | `0x47524153` | `MultiMesh` (5-segment blade) |
| Wildflowers | 3,000 | `0x464C5752` | `MultiMesh` with per-instance color |

### Color Palettes

**Tree canopy colors** (8 variations, all natural greens):
```
(0.22, 0.38, 0.18), (0.28, 0.42, 0.20), (0.32, 0.46, 0.22),
(0.25, 0.40, 0.25), (0.35, 0.50, 0.24), (0.20, 0.35, 0.22),
(0.38, 0.48, 0.18), (0.30, 0.44, 0.16)
```
Each tree randomly picks one and applies ±0.06 lightness variation.

**Wildflower colors** (6 types): yellow, cream, purple, red, orange, blue — each with ±0.08 lightness jitter. Color is passed via `MultiMesh.set_instance_color` and read as `COLOR.rgb` in the shader.

---

## Tree Generation

Trees are fully procedural low-poly meshes. The `TreeGenerator` class outputs two separate meshes: **wood** (trunk + branches) and **leaves** (canopy shapes).

### Three Styles

**Pine (style 0):**
- Cylinder trunk (3.5–5.0 units tall, radius 0.10–0.15)
- 4–6 stacked **cones** layered from 25% trunk height upward
- Each cone layer decreases in radius by ~55% from bottom to top
- Bottom layers wider (radius 1.4–2.0), taller; top layers smaller

**Round (style 1):**
- Cylinder trunk (3.0–4.5 units tall, thinner radius 0.08–0.12)
- Single **icosphere** canopy (radius 1.5–2.2, subdivision level 2)
- 70% chance of a secondary smaller icosphere blob offset to one side

**Oak (style 2):**
- Short thick trunk (2.8–3.8 units, radius 0.18–0.28) going 55% of total height
- **Forked:** 2–4 branch cylinders diverging from the fork point at 0.3–0.55 lean angle
- Each branch tip gets an icosphere blob (radius 1.2–2.0)
- Additional top icosphere blob (radius 1.4–2.2) for canopy crown

### Primitive Builders

**Cylinder:** Parameterized by base position, height, bottom/top radius, side count, and direction vector. 4 rings of vertices, quad-strip tessellation. Used for trunks and branches.

**Cone:** Generates `n`-sided cone with tip and base cap. Used for pine layers.

**Icosphere:** Starts from the 12-vertex icosahedron, applies **loop subdivision** `n` times using a midpoint cache to avoid duplicate vertices. Each final vertex is displaced by ±6% random jitter for an organic feel. Subdivision 2 gives 320 triangles — enough facets for a chunky-but-smooth low-poly look.

### Winding Order

Godot uses **counter-clockwise** front faces. The icosphere and cylinder needed their winding reversed from the naive vertex order. The cone's original winding was already correct. `generate_normals()` is called after all geometry is committed to compute face normals from winding.

### Material Assignment

Each tree gets its own `ShaderMaterial` instances:
- **Canopy:** Uses `canopy.gdshader` with `leaf_color` set to a random palette color. Wind strength and speed are randomized per tree.
- **Trunk:** Uses `trunk.gdshader` with `sway_speed` linked to the canopy's wind speed for coherent motion.

### Overall Tree Scale

Trees are placed with scale 1.0–1.5x (random per tree), with slight Y-axis stretch (0.9–1.15x) for height variety. Rotation is randomized around Y.

---

## Grass Rendering

Grass uses a **two-tier system**: an impostor ground plane for base coverage, and 50,000 individual blade instances for close-up detail.

### Tier 1: Grass Impostor

A low-resolution (48×48) mesh that covers all non-river terrain. The shader procedurally generates a grass-like pattern using world-space noise:
- Patches of grass density via `value_noise` at `patch_scale`
- View-dependent mix: faces pointing away from camera fade to dirt color
- Wind affects AO (ambient occlusion darkening) rather than geometry
- Perturbed normals for specular sparkle in sunlight

### Tier 2: Grass Blades

50,000 individual blades via `MultiMesh`. Each blade mesh has **5 segments** forming a tapered quad strip (width decreases 85% from base to tip). Blade properties:

**Vertex shader (per-blade):**
- `NODE_POSITION_WORLD` drives consistent world-space wind across all instances
- Patch noise scales blade size between `size_small` (0.4) and `size_large` (0.75)
- Blade bends forward along Z based on patch noise and `blade_bend` uniform
- Wind displacement: **ridged FBM** noise at two octaves, applied to XZ with slight Y sag
- Wind only affects upper portions of blade (UV.y multiplier)

**Fragment shader:**
- `cull_disabled` — both sides rendered
- Backface normals flipped for correct lighting on reverse side
- Normal mixed toward UP for softer, less harsh lighting across the meadow
- `BACKLIGHT` set for subsurface scattering effect (light passing through blades)
- Color interpolated from `color_young` (bright green) to `color_old` (darker) based on UV.y

### Wind Model

Wind is implemented as a **flow field** using fractal Brownian motion (FBM):

```glsl
wind_pos = world_xz / wind_scale - TIME * wind_direction * wind_speed
wind_value = ridged_noise(wind_pos)  // |2 * noise - 1|, summed at two octaves
displacement = wind_value * wind_strength * height_factor
```

The `ridged_noise` variant creates visible "gusts" that sweep across the grass field. Wind direction, speed, and strength are all shader uniforms.

---

## Shader Architecture

All shaders are `shader_type spatial` targeting GL Compatibility. Common patterns:

### Shared Noise Functions

Every shader contains inline copies of:
- `hash21(vec2)` or `hash31(vec3)` — pseudo-random hash via `fract(sin(dot(...)))`
- `value_noise(vec2)` or `noise3(vec3)` — trilinear interpolation of hash values with smoothstep
- `fbm(vec3)` — 3-4 octave fractal Brownian motion (terrain shader only)
- `ridged_noise(vec2)` — `abs(2*noise - 1)` for sharp ridge patterns (grass wind)

In Unity, these would be centralized in a shared `.hlsl` include file or a noise texture.

### Render Mode Choices

| Shader | Culling | Blend | Specular | Notes |
|--------|---------|-------|----------|-------|
| Terrain | `cull_back` | opaque | Schlick GGX | Standard opaque |
| Grass | `cull_disabled` | opaque | Schlick GGX | Two-sided, backface normal flip |
| Grass Impostor | `cull_back` | opaque | Schlick GGX | Ground plane |
| Canopy | `cull_back` | opaque | Schlick GGX | Solid low-poly geometry |
| Trunk | `cull_back` | opaque | `specular_disabled` | Matte bark |
| Water | `cull_disabled` | `blend_mix` | Schlick GGX | Transparent, both sides |
| Wildflower | `cull_disabled` | opaque | Schlick GGX | Two-sided petals |

---

## Day/Night Cycle

A single script on the `DirectionalLight3D` drives the full lighting state.

### Time Progression

```
_phase += delta / day_length_sec    // default 720 seconds = 12 minutes
_phase = fmod(_phase, 1.0)         // wraps 0→1
```

Phase 0.22 is starting time (early morning). Sun rotation maps phase to a 0–2π arc:

```
sun.rotation.x = lerp(-32°, -172°, sin(phase * 2π))
sun.rotation.y = cos(phase * 2π * 0.85) * 22°     // slight wobble
```

### Derived Values

`day_amt` = normalized sun altitude (1.0 = noon, 0.0 = midnight)  
`sunset_amt` = peak when `day_amt ≈ 0.35`, used for warm orange tinting

### What Changes

| Property | Day | Night/Sunset |
|----------|-----|-------------|
| Light energy | 1.2 | 0.05 |
| Light color | White | Warm orange (sunset) → cool blue (night) |
| Sky top color | Blue | Deep navy |
| Sky horizon | Light blue | Orange (sunset) → dark blue |
| Ground horizon | Warm tan | Dark brown |
| Ambient light | Soft white | Dim blue |
| Fog color | Light gray | Warm (sunset) → dark |

All transitions use `lerp` with `day_amt` and `sunset_amt` as interpolation factors.

---

## Player Controller

First-person / third-person `CharacterBody3D` with swim support.

### Movement

| State | Speed | Gravity | Jump |
|-------|-------|---------|------|
| Ground | 4.2 | 12.0 (project setting) | 5.5 velocity |
| Swimming | 2.4 | 0 (buoyancy system) | N/A |

**Swim detection:** Player "feet" are at `position.y - 0.8`. Swimming activates when feet are below `WATER_Y` AND `WorldData.is_river(x, z)` is true.

**Swim physics:**
- Buoyancy force proportional to submersion depth: `BUOYANCY * submerge_depth`
- Water drag on vertical velocity: `velocity.y *= 1.0 - WATER_DRAG * delta`
- Space = swim up (`SWIM_UP_FORCE`), Shift = sink (`SINK_SPEED`)
- Horizontal movement at `SWIM_SPEED` (slower than walk)

**Head bob:** Sinusoidal Y/X offset on the camera, different frequencies for walking vs swimming vs idle. Interpolated smoothly via `lerp`.

**Camera:** V key toggles first-person / third-person. Mouse sensitivity 0.0022 rad/pixel. Escape releases mouse; click recaptures.

### Collision Layers

- Layer 1: World (terrain, perimeter walls)
- Layer 2: Player

---

## Scene Graph & Wiring

```
World (Node3D, world_controller.gd)
├── WorldEnvironment (ProceduralSkyMaterial, ACES tonemap, fog, glow)
├── DirectionalLight3D (day_night.gd, shadow mode orthogonal)
├── VoxelChunk (voxel_chunk.gd, generates terrain + water at _ready)
├── Foliage (foliage_placer.gd, populates all vegetation at _ready)
├── PerimeterWalls (perimeter_walls.gd, invisible collision)
├── Player (player_controller.gd, CharacterBody3D)
└── Crosshair (CanvasLayer, layer 32, centered ColorRect)
```

### Environment Settings

- **Renderer:** GL Compatibility (WebGL-safe)
- **Viewport:** 1280×720, stretch mode `canvas_items`
- **Tonemap:** ACES
- **Fog:** Enabled, color driven by day/night
- **Glow:** Multiple levels, reduced 72% intensity on web builds
- **Texture filter:** Nearest (project default)

---

## Performance Considerations

### What's Cheap

- **MultiMesh instancing:** 50k grass + 3k flowers + 30 rocks = 3 draw calls total
- **Analytical terrain:** No texture lookups for height, just math
- **No imported textures:** Zero texture memory for geometry (only the icon.svg import)
- **Shadow casting disabled** on grass, flowers, canopy — only terrain and trunks cast shadows

### What's Expensive

- **50,000 grass blades** with per-instance vertex shader animation — the biggest GPU cost
- **Transparent water** with `blend_mix` — overdraw on the river surface
- **22 trees × separate MeshInstance3D** — not instanced via MultiMesh (each tree is unique geometry), so 44 draw calls (wood + leaves per tree)
- **Icosphere subdivision 2** = 320 triangles per sphere, multiple spheres per oak/round tree

### Optimization Opportunities

- Trees could be batched into a few `MultiMesh` groups by style if variety is reduced
- Grass blade count could scale with device capability (query GPU tier)
- LOD system: swap grass blades for impostor-only beyond a distance threshold
- Water could use a screen-space depth texture for proper depth-based transparency (not available in GL Compatibility)

---

## Porting Notes (Unity)

### Direct Mappings

| Godot | Unity |
|-------|-------|
| `CharacterBody3D` | `CharacterController` or Rigidbody + custom controller |
| `MultiMeshInstance3D` | `Graphics.DrawMeshInstanced` or `DrawMeshInstancedIndirect` |
| `SurfaceTool` | `Mesh` API (`SetVertices`, `SetTriangles`, etc.) |
| `ShaderMaterial` | `Material` with custom shader |
| `ProceduralSkyMaterial` | Custom skybox shader or Unity's built-in procedural sky |
| `WorldEnvironment` | Volume Profile (URP/HDRP) |
| `DirectionalLight3D` | `Light` component, type Directional |
| `ConcavePolygonShape3D` | `MeshCollider` |
| `StaticBody3D` + `BoxShape3D` | `BoxCollider` on a static GameObject |
| `RandomNumberGenerator` | `System.Random` with seed |
| `call_deferred` | `StartCoroutine` or `Invoke` |
| `Input.get_action_strength` | `Input.GetAxis` or new Input System |
| Shader `NODE_POSITION_WORLD` | `unity_ObjectToWorld._m03_m13_m23` or instance data |
| `BACKLIGHT` | Custom subsurface scattering in shader |
| `CanvasLayer` | Unity Canvas (Screen Space - Overlay) |

### Key Differences

1. **Shader language:** Godot shaders are GLSL-like with built-in `VERTEX`, `NORMAL`, `ALBEDO`, etc. Unity uses HLSL with `SV_Position`, custom output structs. The noise functions translate 1:1 but the I/O structure changes completely.

2. **MultiMesh vs DrawMeshInstanced:** Unity's instancing requires a `MaterialPropertyBlock` or compute buffer for per-instance data (color, transform). Godot's MultiMesh handles this automatically.

3. **Terrain generation:** The `SurfaceTool` workflow maps to Unity's `Mesh` class. Create `Vector3[]` for vertices/normals, `int[]` for triangles, `Color[]` for vertex colors, call `mesh.SetVertices()`, etc.

4. **Tree generator:** The icosphere subdivision algorithm is engine-agnostic. Port the `_add_icosphere`, `_add_cone`, `_add_cylinder` functions to C# operating on `List<Vector3>` and `List<int>`.

5. **Water transparency:** In URP, use a transparent shader with `_Surface` set to Transparent. Fresnel, wave displacement, and ripple normals all port directly.

6. **Day/night:** Drive `RenderSettings.ambientLight`, `RenderSettings.fogColor`, light transform/color/intensity, and skybox material properties from a single MonoBehaviour.

7. **Grass:** Consider Unity's `Graphics.DrawMeshInstancedIndirect` with a compute shader for wind animation, or use the Terrain system's detail/grass painting with a custom shader.

### What to Keep

- The terrain height function — it's pure math, works anywhere
- The river SDF approach — simple and effective
- The low-poly tree generation algorithm — engine-agnostic geometry
- The wind model (ridged FBM noise field) — proven to look good
- The day/night color tables — already tuned for pleasing results
- Deterministic seeds for reproducible worlds

### What to Reconsider

- Grass rendering strategy may benefit from Unity's GPU instancing + compute
- Water could use depth-based effects in URP/HDRP (screen-space depth available)
- Tree variety could use Unity's prefab system + random selection vs full procedural generation
- Audio (currently disabled) could use Unity's native audio system vs procedural generation
