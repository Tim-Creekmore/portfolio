# Current Sprint: Phase B — Vertical Slice

**Phase:** B (Weeks 3–8)
**Goal:** Ship a 20–30 minute playable slice with biomes, inventory, field battles, a dungeon, and full save/load.

**Architecture decision (locked in):**
- Biomes are the default experience (`ARENA_MODE = false`)
- Arena stays accessible as a combat sandbox via a dev toggle
- Phase B redesigns biomes from scratch: **3 biomes** (Temperate, Mountain, Frontier) replacing the old 13-biome experiment

---

## Sprint B1: World Foundation (Week 3) — IN PROGRESS

### Task B1.1 — Terrain material overhaul
- **Delivered:**
  - `TerrainChunk.BiomeColor()` — replaced all hardcoded RGB with visual bible palette constants (`VB_GRASS_TOP`, `VB_GRASS_SIDE`, `VB_DIRT`, `VB_STONE`, `VB_STONE_DARK`, `VB_SAND`, `VB_SNOW`, `VB_LEAF`)
  - Voxel chunk cliff sides now use `VB_STONE_DARK` (#4a4540)
  - Tree trunk base color updated to `#6a4830` (visual bible wood dark)
  - Tree leaf palette (`TREE_COLORS`) reduced from 10 to 8 values, all clustered tightly around visual bible `#3a5a2a` for consistent medieval forest tone
- **Pending:** Visual verification awaiting next `ARENA_MODE = false` build; currently not visible in arena mode
- **Status:** [x] Complete (applied)

### Task B1.2 — Biome system scaffold
- **Delivered:**
  - `BiomeData.cs` ScriptableObject — ground + vegetation palette, atmospheric tint, density, tree style hint, ambient profile id
  - `BiomeAssetBuilder.cs` editor utility with menu `Voxel Kingdom / Build Starter Biomes` — creates/refreshes the 3 starter assets in `Assets/Data/Biomes/`
  - **Temperate** — VB palette, warm gold ambient, high density (0.65 tree / 0.90 grass), Leafy
  - **Mountain** — amber-tinted stone, cream palette, gentle cool ambient, sparse (0.25/0.15), Pine
  - **Frontier** — burnt autumn palette, warm amber ambient, medium density (0.50/0.35), AutumnBurnt
  - Updated `visual-bible.md` with "Design Philosophy" section locking in mystery-not-dread framing
- **Pending:** Player must run menu `Voxel Kingdom / Build Starter Biomes` once to materialize the 3 assets
- **Status:** [x] Complete

### Task B1.3 — Biome region painting
- **Delivered:**
  - `BiomeRegistry.cs` — scene singleton with 3 serialized biome assets, 3-seed Voronoi with Perlin-warped borders, `GetBiomeAt()` + `GetBlendedBiomes()` (returns primary + secondary + blend weight for soft transitions at borders)
  - Region seeds: Temperate (40,40) SW, Frontier (95,45) E, Mountain (60,95) N — creates a natural "walk forward into mystery" layout
  - `TerrainChunk.BiomeColor` now branches: arena mode → legacy palette; biome world → BiomeRegistry with blended palette
  - `WorldSceneSetup` auto-wires the registry with loaded biome assets (runs early in scene build)
  - `TerrainChunk.BuildTerrain` moved to `Start()` so `BiomeRegistry.Awake()` completes first
  - Added `LEGACY_OVERLAYS = false` gate — when ARENA_MODE=false, old lakes/rivers/roads stay dormant
  - Spawn position branches: arena → (60,38); biome world → (40,40) deep inside Temperate
  - Gizmos visualize biome seeds when BiomeRegistry is selected in scene
- **How to see it:** Set `WorldData.ARENA_MODE = false`, run Setup Scene, enter Play
- **Status:** [x] Complete

### Task B1.4 — Vegetation density pass
- **Spec:** Tree/grass density varies by biome. Mountain: sparse pines, no grass. Frontier: dead trees, scrub. Temperate: full vegetation.
- **Status:** [ ] Not started

### Task B1.5 — Ambient audio per biome
- **Spec:** `AudioSource` zones that crossfade as player moves between biomes. Temperate: birds + wind. Mountain: wind + echo. Frontier: wolves + silence.
- **Status:** [ ] Not started

---

## Sprint B2: Hero Depth (Week 4)
- **Key outcome:** Inventory system, weapon/armor equip, consumables
- **Tim's requested additions:** Bows + throwing weapons land here (weapon variety via equip system)

## Sprint B3: General Depth (Week 5)
- **Key outcome:** Squad cap 10, morale + routing, scripted 10v15 field battle

## Sprint B4: Dungeon (Week 6–7)
- **Key outcome:** 4-room cave dungeon with puzzle, combat, boss, loot

## Sprint B5: Save + Polish (Week 8)
- **Key outcome:** Full save/load of slice state, performance baseline, bug sweep, Phase B exit gate

---

## Phase B Exit Gate (target)
- [ ] 20–30 minute slice playable start-to-finish
- [ ] All 3 biomes visually distinct
- [ ] Dungeon completable (puzzle + combat + boss + loot)
- [ ] Field battle plays out with morale/routing
- [ ] Inventory works (pick up, equip, use consumables)
- [ ] Save/load handles full slice state
- [ ] 60fps target met (or optimization plan documented)
- [ ] 5 internal runs completed without critical blockers
