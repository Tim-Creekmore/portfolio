# VOXEL KINGDOM — MASTER PLAN (Granular Edition)

---

## LAYER 0: CURSOR TOOLING PREREQUISITES

Everything below assumes your Cursor environment is fully armed. Do this first — before touching any game code. Think of this as building the factory before building the product.

---

### 0A. Cursor Project Structure

Your Unity project root should look like this when you're done with Layer 0:

```
YourUnityProject/
├── .cursorrules                    ← already exists (update it)
├── .cursor/
│   ├── rules/
│   │   ├── visual-bible.md         ← extracted from .cursorrules for modularity
│   │   ├── scale-contract.md       ← hard measurements, always loaded
│   │   └── unity-conventions.md    ← C#/URP code style rules
│   ├── skills/
│   │   ├── visual-task/
│   │   │   └── SKILL.md            ← visual task template + workflow
│   │   ├── combat-system/
│   │   │   └── SKILL.md            ← combat implementation patterns
│   │   ├── voxel-building/
│   │   │   └── SKILL.md            ← voxel construction patterns
│   │   ├── squad-ai/
│   │   │   └── SKILL.md            ← unit AI + commander mode patterns
│   │   └── save-system/
│   │       └── SKILL.md            ← serialization patterns for voxel + state
│   ├── commands/
│   │   ├── new-task.md             ← /new-task: scaffolds a task card
│   │   ├── visual-check.md         ← /visual-check: screenshot + review loop
│   │   ├── playtest.md             ← /playtest: enters play mode, runs checks
│   │   └── phase-status.md         ← /phase-status: reads phase board, reports
│   ├── hooks/
│   │   └── hooks.json              ← stop hook for grind loop
│   ├── plans/
│   │   └── current-sprint.md       ← active sprint context
│   └── mcp.json                    ← MCP server config
├── Assets/
├── Packages/
└── references/
    └── going_medieval_*.png         ← visual reference screenshots
```

---

### 0B. MCP Servers to Install

These are the MCP servers that will actually help your workflow. Install them in order of impact.

**1. Unity MCP (AnkleBreaker Studio) — CRITICAL**
- Repo: `github.com/AnkleBreaker-Studio/unity-mcp-server`
- What it does: 288 tools across 30+ categories. Scene management, GameObject manipulation, component editing, material assignment, physics, profiling, terrain, NavMesh, animation, builds. Your agent can directly control the Unity Editor without you copy-pasting.
- Why it matters for you: The agent can create GameObjects, set transforms, assign materials, enter/exit play mode, read console logs, capture screenshots, run profiler checks — all from Cursor. This is the bridge between "agent writes code" and "agent sees the scene."
- Config in `.cursor/mcp.json`:
```json
{
  "mcpServers": {
    "unity": {
      "command": "node",
      "args": ["path/to/unity-mcp-server/build/index.js"]
    }
  }
}
```

**2. Git MCP — IMPORTANT**
- Already built into Cursor, but make sure it's enabled.
- Why it matters: The agent can commit, branch, diff, and manage version control. Critical for the "one phase at a time" discipline — the agent should commit at phase boundaries.

**3. Filesystem MCP — ALREADY AVAILABLE**
- Built into Cursor. Confirm it can read/write your Unity project folder.

**4. Optional but valuable later:**
- **Figma MCP** — if you ever get reference designs from a designer
- **Image generation** — Cursor now has built-in image gen (Google Nano Banana Pro) for creating UI mockups and placeholder textures directly in the editor

---

### 0C. Rules (Always-On Context)

Migrate from a single monolithic `.cursorrules` to modular rules in `.cursor/rules/`. The `.cursorrules` file can stay as the master overview, but break out frequently-referenced sections:

**`.cursor/rules/scale-contract.md`**
```markdown
---
description: Hard measurements for all game assets. Reference before creating or placing any asset.
globs: ["Assets/**/*.cs", "Assets/**/*.shader"]
---

# Scale Contract

All measurements in Unity world units. 1 unit = 1 meter.

- Player character height: 1.8 units
- Player character width: 0.5 units
- Standard door height: 2.2 units
- Standard door width: 1.0 units
- Building floor height: 3.0 units (floor to ceiling)
- Wall thickness: 0.3 units (wood), 0.5 units (stone)
- Tree trunk diameter: 0.3–0.8 units
- Tree canopy height: 4–12 units (varies by species)
- Voxel block size: 1.0 unit (cube)
- Standard road width: 4.0 units
- River width (small): 6–10 units
- Settlement wall height: 5.0 units minimum
- Camera eye height (first person): 1.6 units

All assets must be proportional to these values. If you are unsure whether something looks right, compare it to the player character height (1.8 units).
```

**`.cursor/rules/visual-bible.md`**
```markdown
---
description: Color palette, lighting spec, and visual identity for Going Medieval aesthetic.
globs: ["Assets/**/*.shader", "Assets/**/*.mat", "Assets/**/*.cs"]
---

# Visual Bible

## Color Palette (exact hex)
- Grass top: #5a7a3a
- Grass side: #4a6a2a
- Dirt: #7a5c3a
- Stone: #6a6560
- Stone dark: #4a4540
- Wood: #8a6040
- Wood dark: #6a4830
- Water surface: #2a4a6a
- Water deep: #1a3a5a
- Sand: #c4a870
- Snow: #d8d0c8
- Leaf: #3a5a2a
- Thatch: #b89860

## Lighting Specification
- Sun color: warm amber #ffcc88
- Sun intensity: 1.2
- Sun angle: 35 degrees (permanent late-afternoon feel)
- Ambient color: warm grey #886644 at 0.4 intensity
- SSAO: enabled, radius 0.4, intensity 1.0
- Fog: enabled, color #c8b898, start 40 units, end 120 units
- Shadows: soft, shadow distance 80 units
- Shadow cascades: 4

## Absolute Prohibitions
- No saturated or neon colors
- No Minecraft-style hard block edges on surface terrain
- No white or cool-toned lighting
- No bloom or lens flare
- No default URP materials — everything gets custom
```

**`.cursor/rules/unity-conventions.md`**
```markdown
---
description: C# and Unity code style conventions.
globs: ["Assets/**/*.cs"]
---

# Unity Code Conventions

- Namespace everything: `VoxelKingdom.Systems`, `VoxelKingdom.Combat`, etc.
- One MonoBehaviour per file
- Use SerializeField for inspector-exposed privates, never public fields
- Prefer ScriptableObjects for data definitions (weapon stats, unit types, biome configs)
- Use the new Input System, not legacy Input.GetKey
- Coroutines only for simple timing — use async/await or state machines for complex flows
- Comment non-obvious decisions with // DECISION: reasoning
- Performance-critical code gets // PERF: annotation
- Never use Find() or FindObjectOfType() in Update loops
- Chunk-based systems must implement object pooling
- All generated meshes must use Jobs + Burst where applicable
```

---

### 0D. Skills (Dynamic, Loaded When Relevant)

Skills are the big upgrade over a flat `.cursorrules` file. They load only when the agent decides they're relevant, keeping your context window clean.

**`.cursor/skills/visual-task/SKILL.md`**
```markdown
---
name: visual-task
description: Use when creating or modifying any visual element — terrain, buildings, vegetation, lighting, materials, VFX, UI. Provides the visual task template and review workflow.
---

# Visual Task Workflow

## When to Use
- Creating any new visual asset or material
- Modifying terrain, lighting, or atmosphere
- Placing or scaling scene objects
- Adjusting shaders or post-processing

## Task Template (fill before starting)

### What is the thing?
[Name and description]

### What does it look like?
[Reference image path in /references/ OR description against visual bible]

### Where does it go in the scene?
[Scene name, approximate position, what it's near]

### Scale check
[Expected dimensions in Unity units, compared to player height 1.8u]

### Color palette constraints
[Which hex values from the visual bible apply]

### Done when
[Specific acceptance criteria — e.g. "stone wall matches #6a6560, casts soft shadow, SSAO visible in crevices"]

## Review Loop
1. Make the change
2. Use Unity MCP to enter play mode and capture screenshot from player camera
3. Compare screenshot against visual bible palette
4. Check scale against scale contract
5. If wrong: iterate. If right: commit and update phase board.
```

**`.cursor/skills/combat-system/SKILL.md`**
```markdown
---
name: combat-system
description: Use when implementing or modifying combat mechanics — directional melee, blocking, damage, stamina, hit detection, weapon types.
---

# Combat System Patterns

## When to Use
- Implementing attack/block mechanics
- Adding weapon types or combat animations
- Tuning damage, stamina, or hit detection
- Combat-related UI (health bar, stamina bar, damage numbers)

## Architecture
- Combat uses a state machine: Idle → WindUp → Swing → Recovery → Idle
- Direction determined by mouse delta during WindUp
- Four attack directions: overhead, left, right, thrust
- Blocking mirrors attack directions — correct block negates damage
- Each state has a fixed duration (tunable via ScriptableObject)
- Hit detection via Physics.SphereCast during Swing state only

## Key Constraints
- Combat must feel weighty — no instant attacks
- WindUp minimum 0.2s, Swing 0.15s, Recovery 0.3s (starting values)
- Stamina cost per swing: 15. Stamina cost per block: 10. Regen rate: 5/sec.
- Player cannot attack during Recovery (punishment for whiffing)
- DECISION: This is Mount & Blade feel, not Dark Souls. Faster rhythm, less punishment.
```

**Create similar SKILL.md files for:**
- `squad-ai/` — unit behavior state machine, commander mode patterns, morale system
- `voxel-building/` — block placement, material types, structural integrity
- `save-system/` — what needs serializing, chunk format, migration strategy

---

### 0E. Commands (Slash-Triggered Workflows)

**`.cursor/commands/new-task.md`**
```markdown
---
name: new-task
description: Scaffold a new task card with the standard template.
---

Create a new task card in `.cursor/plans/current-sprint.md` using this format:

## Task: [TITLE]
- **Phase:** [A/B/C/D/E]
- **Type:** [code / visual / system / bugfix]
- **Depends on:** [list any prerequisite tasks]
- **Spec:**
  [What needs to be built — be specific]
- **Acceptance criteria:**
  [ ] Criterion 1
  [ ] Criterion 2
  [ ] Criterion 3
- **Visual task?** [Yes/No — if yes, load the visual-task skill]
- **Status:** [ ] Not started

Ask me for the task details before filling this in.
```

**`.cursor/commands/visual-check.md`**
```markdown
---
name: visual-check
description: Run a visual QA pass using Unity MCP.
---

1. Use Unity MCP to enter play mode
2. Position the camera at the current player spawn
3. Capture a screenshot via Unity MCP
4. Analyze the screenshot:
   - Does the color palette match the visual bible?
   - Are proportions consistent with the scale contract?
   - Is SSAO visible on surfaces?
   - Is fog present and warm-tinted?
   - Are there any default URP materials visible (grey/pink)?
5. Report findings and suggest fixes if needed
6. Exit play mode
```

**`.cursor/commands/phase-status.md`**
```markdown
---
name: phase-status
description: Report current phase completion status.
---

Read `.cursor/plans/current-sprint.md` and the phase board in `.cursorrules`.
Report:
- Current phase and sprint
- Tasks completed vs remaining
- Any blockers flagged
- Estimated remaining work
```

---

### 0F. Hooks (Automated Guardrails)

**`.cursor/hooks/hooks.json`**
```json
{
  "version": 1,
  "hooks": {
    "stop": [
      {
        "command": "bun run .cursor/hooks/grind.ts"
      }
    ]
  }
}
```

**`.cursor/hooks/grind.ts`** — keeps the agent iterating until a task is done:
```typescript
import { readFileSync, existsSync } from "fs";

interface StopHookInput {
  conversation_id: string;
  status: "completed" | "aborted" | "error";
  loop_count: number;
}

const input: StopHookInput = await Bun.stdin.json();
const MAX_ITERATIONS = 8;

if (input.status !== "completed" || input.loop_count >= MAX_ITERATIONS) {
  console.log(JSON.stringify({}));
  process.exit(0);
}

const scratchpad = existsSync(".cursor/scratchpad.md")
  ? readFileSync(".cursor/scratchpad.md", "utf-8")
  : "";

if (scratchpad.includes("TASK_COMPLETE")) {
  console.log(JSON.stringify({}));
} else {
  console.log(
    JSON.stringify({
      followup_message: `[Iteration ${input.loop_count + 1}/${MAX_ITERATIONS}] Task not marked complete. Check scratchpad.md for remaining work. Continue implementing. Write TASK_COMPLETE to scratchpad when done.`,
    })
  );
}
```

This means when you give the agent a task, it will keep working on it (up to 8 iterations) until it writes `TASK_COMPLETE` to the scratchpad. No more one-shot prompts that leave things half-done.

---

### 0G. Reference Images

Create a `/references/` folder in your project root and add:
- 3–5 Going Medieval screenshots showing the exact tone/lighting you want
- 1 screenshot of your current Unity scene (baseline)
- Any concept art or visual targets you find

The agent can read images directly. Your visual task cards should reference these paths.

---

### Layer 0 Checklist

```
[ ] .cursor/ folder structure created
[ ] Unity MCP (AnkleBreaker) installed and connecting to Unity Editor
[ ] .cursor/mcp.json configured
[ ] Rules split into modular files (scale-contract, visual-bible, unity-conventions)
[ ] visual-task SKILL.md created
[ ] combat-system SKILL.md created
[ ] /new-task command created
[ ] /visual-check command created
[ ] /phase-status command created
[ ] Grind hook installed (hooks.json + grind.ts)
[ ] Bun installed (required for hooks)
[ ] Reference images added to /references/
[ ] .cursorrules updated with decision log (4 locked decisions)
[ ] Unity MCP screenshot workflow tested (agent → play mode → screenshot → review)
[ ] Git repo initialized with .gitignore
```

---
---

## LAYER 1: THE PLAN (Granular Breakdown)

Your plan document is solid. Here's every phase broken into individual tasks small enough that each one is a single agent session (30–90 minutes). Each task has clear inputs, outputs, and a done-when.

---

## PHASE A — PROOF OF FANTASY (Weeks 1–2)

### Sprint A1: Commander Toggle + Camera (Days 3–4)

**A1.1 — Camera state machine scaffold**
- Input: Player controller already exists
- Build: CameraStateMachine.cs with two states: HeroCamera (first-person) and CommanderCamera (top-down tactical)
- HeroCamera: attached to player head, mouse-look, existing behavior
- CommanderCamera: elevated position (15–20 units above player), WASD pan, mouse-edge scroll, zoom with scroll wheel
- Done when: Both camera states exist as separate classes. No transition yet.

**A1.2 — Commander toggle input binding**
- Input: A1.1 complete
- Build: Bind a dedicated key (Tab) via new Input System action map "CommandMode"
- On press: CameraStateMachine transitions HeroCamera → CommanderCamera
- On press again: CommanderCamera → HeroCamera
- Done when: Pressing Tab reliably switches camera perspective. No visual glitches during transition.

**A1.3 — Camera transition smoothing**
- Input: A1.2 complete
- Build: Lerp between hero and commander positions over 0.5s. Lock player input during transition.
- Done when: Transition feels smooth, no snap-cut, no jitter. Player can't move/attack during the 0.5s blend.

**A1.4 — Commander cursor + selection**
- Input: A1.3 complete
- Build: In commander mode, show a cursor. Raycast from mouse position to ground plane. Draw a selection circle at hit point.
- Done when: Cursor visible in commander mode, hidden in hero mode. Raycast hits terrain.

### Sprint A2: Death System (Days 4–5)

**A2.1 — Player health component**
- Input: Player controller exists
- Build: PlayerHealth.cs — max HP (100), current HP, TakeDamage(float), OnDeath event
- Done when: Health decrements when TakeDamage is called. OnDeath fires at 0 HP.

**A2.2 — Death marker system**
- Input: A2.1 complete
- Build: On death, spawn a DeathMarker prefab at player position. Store the player's carried inventory reference. DeathMarker has a timer (300s default). DeathMarker destroyed when timer expires OR player retrieves items.
- Done when: Dying spawns a marker. Marker persists for 5 minutes. Marker is interactable.

**A2.3 — Respawn flow**
- Input: A2.2 complete
- Build: On death → fade to black (1s) → teleport to last settlement spawn point → fade in (1s) → restore HP to full → inventory is empty (items are at death marker)
- Done when: Full death → respawn loop works without errors.

**A2.4 — Death marker retrieval**
- Input: A2.3 complete
- Build: Player walks to death marker → press E to interact → items return to inventory → marker destroyed
- Done when: Items can be dropped and retrieved. If timer expires first, items are gone permanently.

### Sprint A3: Hero Combat Baseline (Days 6–8)

**A3.1 — Weapon data architecture**
- Input: None
- Build: WeaponData ScriptableObject — damage, swing speed, stamina cost, range, attack directions supported
- Create one instance: "Iron Sword" — damage 20, swing 0.4s, stamina 15, range 1.5u
- Done when: ScriptableObject created and accessible in inspector.

**A3.2 — Combat state machine**
- Input: A3.1 complete
- Build: CombatStateMachine.cs — states: Idle, WindUp, Swing, Recovery, Blocking
- State durations driven by WeaponData
- Transitions: Idle → (left click) → WindUp → Swing → Recovery → Idle
- Transitions: Idle → (right click hold) → Blocking → (release) → Idle
- Done when: State machine cycles through states correctly. Debug log shows transitions.

**A3.3 — Directional attack input**
- Input: A3.2 complete
- Build: During WindUp, read mouse delta to determine attack direction (up = overhead, left = left slash, right = right slash, down = thrust)
- Store direction in combat state
- Done when: Debug UI shows current attack direction based on mouse movement.

**A3.4 — Hit detection**
- Input: A3.3 complete
- Build: During Swing state only, fire a SphereCast from player position in attack direction
- Range from WeaponData. Radius 0.3u.
- On hit: call TakeDamage on target's health component
- Done when: Swinging at a test dummy reduces its HP. Missing does nothing.

**A3.5 — Blocking system**
- Input: A3.4 complete
- Build: While Blocking, read mouse delta to set block direction (same 4 directions)
- If incoming attack direction matches block direction → negate damage, play block feedback
- If mismatch → full damage
- Done when: Correct block negates damage. Wrong direction takes full hit.

**A3.6 — Stamina system**
- Input: A3.5 complete
- Build: PlayerStamina.cs — max 100, cost per attack (from WeaponData), cost per block (10), regen 5/sec while not attacking/blocking
- Can't attack if stamina < cost. Can't block if stamina < 10.
- Done when: Stamina depletes on actions, regenerates over time, prevents actions when empty.

**A3.7 — Combat HUD**
- Input: A3.6 complete
- Build: Minimal UI — health bar (red), stamina bar (green), positioned bottom-left
- Uses Unity UI Toolkit or Canvas (agent's choice)
- Done when: Bars update in real-time. Visually clean, not placeholder.

### Sprint A4: Squad Basics (Days 8–10)

**A4.1 — Unit data architecture**
- Input: None
- Build: UnitData ScriptableObject — unit type name, health, damage, speed, morale threshold
- Create one instance: "Militia" — HP 60, damage 10, speed 3.5, morale threshold 30
- Done when: ScriptableObject exists, values inspectable.

**A4.2 — Unit behavior state machine**
- Input: A4.1 complete
- Build: UnitAI.cs — states: Following, HoldPosition, Attacking, Retreating
- Following: move toward player/rally point, maintain formation offset
- HoldPosition: stay at current position, face nearest threat
- Attacking: move toward target, attack in range
- Retreating: flee away from nearest threat
- Done when: Unit can be manually set to each state via debug command and behaves correctly.

**A4.3 — Squad manager**
- Input: A4.2 complete
- Build: SquadManager.cs — maintains list of units (max 6 for Phase A). Provides methods: SetAllState(state), SetRallyPoint(position), GetAliveCount()
- Done when: 4 units can be spawned, added to squad, and respond to state changes collectively.

**A4.4 — Commander mode orders**
- Input: A4.3 + A1.4 complete
- Build: In commander mode, right-click ground → set rally point (units move there). Right-click enemy → set attack target. Press H → hold position. Press F → follow player.
- Done when: All four order types work from commander camera.

**A4.5 — Unit health + death**
- Input: A4.2 complete
- Build: Units have health. When HP reaches 0, play death animation (or ragdoll), remove from squad. SquadManager updates count.
- Done when: Units can die in combat. Squad count decreases. Dead units are cleaned up.

### Sprint A5: Test Scene + Playtest (Days 11–14)

**A5.1 — Micro encounter arena**
- Input: All A1–A4 tasks complete
- Build: A small enclosed area (50x50 units). Flat terrain with the visual bible palette applied. A few rock props and a wooden fence for cover. Player spawn at one end.
- Done when: Area exists, is lit according to visual bible, and has the Going Medieval feel.

**A5.2 — Enemy placement**
- Input: A5.1 complete
- Build: Place 6–8 enemy units (using Militia UnitData) in the arena. Give them simple aggro behavior: if player or player's units enter range (15u), switch to Attacking state.
- Done when: Enemies idle until approached, then attack.

**A5.3 — Ambient pass**
- Input: A5.1 complete
- Build: Add fog (visual bible spec), SSAO, ambient audio (wind loop, distant birds). Place 2 torch light sources with warm point lights.
- Done when: /visual-check passes. Scene feels atmospheric, not sterile.

**A5.4 — Save/load minimal state**
- Input: A5.2 complete
- Build: Save: player position, HP, stamina, squad unit positions + HP. Load: restore all. Use JSON serialization to a local file.
- Done when: Save → quit → load restores the exact game state.

**A5.5 — Playtest session 1**
- Input: All A5 tasks complete
- Build: Play for 15 minutes. Document in `.cursor/plans/playtest-notes-A1.md`: what felt fun, what felt broken, what was confusing.
- Done when: Notes written. Top 3 blockers identified.

**A5.6 — Blocker fixes (budget: 2 days)**
- Input: A5.5 complete
- Build: Fix only the top 3 blockers from playtest. Nothing else.
- Done when: 3 issues resolved. All other feedback deferred.

**A5.7 — Playtest sessions 2 and 3**
- Input: A5.6 complete
- Build: Two more playtests. Same documentation. Compare notes across sessions.
- Done when: 3 total playtest reports exist. Go/no-go decision made for Phase B.

### Phase A Exit Gate Review

```
[ ] Player can complete 10–15 minute loop without crash
[ ] Commander toggle works 100% (hero ↔ commander, reliable)
[ ] Death loop works (die → marker → respawn → retrieve)
[ ] Combat has directional attack + block
[ ] 4–6 squad units respond to orders
[ ] Save/load preserves basic state
[ ] 3 playtest sessions completed with notes
[ ] Baseline FPS measured in test scene
[ ] GO / NO-GO decision recorded
```

---

## PHASE B — VERTICAL SLICE (Weeks 3–8)

### Sprint B1: World Foundation (Week 3)

**B1.1 — Terrain material overhaul**
- Apply visual bible hex values to all terrain materials
- Run /visual-check after each material change

**B1.2 — Biome system scaffold**
- BiomeData ScriptableObject: name, terrain colors, tree density, grass density, ambient sounds
- Create 3 instances: Temperate, Mountain, Frontier

**B1.3 — Biome region painting**
- Define 3 regions on the terrain using a biome map texture
- Each region uses its BiomeData to drive material and vegetation

**B1.4 — Vegetation density pass**
- Trees and grass density varies by biome
- Mountain biome: sparse trees, no grass. Frontier: dead trees, scrub.

**B1.5 — Ambient audio per biome**
- AudioSource zones that crossfade as player moves between biomes
- Temperate: birds + wind. Mountain: wind + echo. Frontier: wolves + silence.

### Sprint B2: Hero Depth (Week 4)

**B2.1 — Inventory data model**
- InventorySystem.cs — slot-based (20 slots). ItemData ScriptableObject per item type.
- Item types: weapon, armor, consumable, material, quest item
- Weight per item. Total weight limit: 50 units.

**B2.2 — Inventory UI**
- Grid display. Drag-and-drop or click-to-equip.
- Show item name, icon (placeholder squares are fine), weight.

**B2.3 — Equipment system**
- Equip weapon → changes WeaponData used by combat system
- Equip armor → modifies damage reduction on PlayerHealth
- Two slots: main hand, armor

**B2.4 — Stamina tuning pass**
- Regen rates, costs, and feel adjusted based on playtest A notes
- Stamina bar visual feedback (flash when low, shake when empty)

**B2.5 — Health recovery**
- Consumable items (e.g., bread, potion) restore HP over time
- Use from inventory → trigger regen effect → item consumed

### Sprint B3: General Depth (Week 5)

**B3.1 — Squad cap increase to 10**
- SquadManager cap raised. Formation logic updated for larger groups.

**B3.2 — Morale system v1**
- UnitMorale.cs — starts at 100. Decreases when: allies die nearby (-15), taking damage (-5), outnumbered (-1/sec). Increases when: near player (+2/sec), winning fight (+1/sec).
- Below morale threshold → state changes to Retreating

**B3.3 — Routing cascade**
- When 3+ units in a squad are retreating, remaining units get -20 morale penalty
- This creates the Total War "collapse" feel

**B3.4 — Scripted field battle**
- Create a battlefield area (100x60 units). Player's 10 units vs 15 enemy units.
- Enemy uses simple advance-and-attack AI.
- No procedural spawning — all hand-placed.

**B3.5 — Battle outcome tracking**
- After all enemies dead OR all player units dead/retreated: display simple outcome screen
- "Victory" or "Defeat" + casualties count

### Sprint B4: Dungeon (Week 6–7)

**B4.1 — Dungeon entrance + loading**
- Cave opening in terrain. Trigger zone loads dungeon scene additively.
- Player walks in → seamless transition (no loading screen)

**B4.2 — Dungeon layout (4 rooms)**
- Room 1: Entry corridor, torches, atmosphere
- Room 2: Simple puzzle (pressure plate opens gate)
- Room 3: Combat encounter (3 skeleton enemies, new UnitData)
- Room 4: Boss room (1 large enemy, higher HP/damage)

**B4.3 — Dungeon enemy AI**
- Skeletons: patrol a path, aggro on sight (10u range), melee attack
- Boss: charge attack on timer, area swing, more HP

**B4.4 — Dungeon loot**
- Boss drops a weapon (better than starting sword)
- Chest in room 2 contains health potions

**B4.5 — Dungeon atmosphere**
- Dark lighting (point lights only, no sun). Fog closer. Echo audio.
- Dripping water ambient. Torch flicker.

### Sprint B5: Save + Polish (Week 8)

**B5.1 — Save system v1**
- Full state: player position, HP, stamina, inventory, equipped items, squad composition, dungeon cleared flag, death marker state
- JSON to local file. Load restores all.

**B5.2 — Vertical slice playthrough**
- Full start-to-finish play. 20–30 minutes.
- Spawn → explore biomes → find dungeon → clear dungeon → field battle → settlement interaction (placeholder)

**B5.3 — Performance baseline**
- Measure FPS in each area. Target: 60fps on your hardware.
- If any area drops below 45fps: flag for optimization.

**B5.4 — Bug sweep**
- Play through 5 times. Log all bugs. Fix criticals only.

**B5.5 — Phase B exit gate review**

```
[ ] 20–30 minute slice playable start-to-finish
[ ] All 3 biomes visually distinct
[ ] Dungeon completable (puzzle + combat + boss + loot)
[ ] Field battle plays out with morale/routing
[ ] Inventory works (pick up, equip, use consumables)
[ ] Save/load handles full slice state
[ ] 60fps target met (or optimization plan created)
[ ] 5 internal runs completed without critical blocker
[ ] Fresh tester understands core loop in under 5 minutes
```

---

## PHASE C — SYSTEMS EXPANSION (Weeks 9–16)

*(Each of these is a 2–4 day sprint)*

**C1 — Squad AI improvements**: formations (line, wedge, circle), target priority (low HP first, closest, player's target), pathfinding around obstacles

**C2 — Overworld expansion**: 2 more regions (5 total), landmark placement, points of interest, road network connecting settlements

**C3 — Settlement management v1**: one player settlement, 3 building types (house → +population cap, farm → +food, wall → +defense), simple build menu

**C4 — Faction system v1**: 3 NPC factions, reputation per faction (0–100), faction territory on overworld, basic AI (expand, defend, declare war)

**C5 — Diplomacy v1**: talk to faction leaders, negotiate alliance/trade/war, reputation affects dialogue options

**C6 — Economy v1**: 3 resources (wood, stone, food), settlements produce/consume, trade routes between settlements, player can buy/sell

**C7 — Integration playtest**: full loop with all Phase C systems active, identify conflicts and balance issues

---

## PHASE D — BUILDER / FORTIFICATION (Weeks 17–21)

**D1 — Voxel placement system**: place/destroy blocks in owned territory, material selection (wood/stone/metal)

**D2 — Material properties**: wood burns (takes fire damage), stone resists, metal strongest but heaviest

**D3 — Fortification gameplay**: walls/gates/towers affect battle defense modifiers, breach points on walls

**D4 — Structural integrity v1**: blocks need support from below, unsupported blocks collapse after delay

**D5 — Build UX**: ghost preview, snap grid, rotate, undo last placement

---

## PHASE E — INTEGRATION + POLISH (Weeks 22–28)

**E1 — Mode handoff polish**: hero→general→lord transitions feel seamless, no visible loading

**E2 — Magic v1**: 4 spells (fireball, heal, shield, telekinesis), mana resource, cooldowns

**E3 — Save system hardening**: full state across all systems, save file versioning, corruption recovery

**E4 — UX polish**: feedback sounds, hit VFX, UI transitions, onboarding hints

**E5 — Optional main quest scaffold**: 3-beat structure (inciting incident → midpoint → climax), can be ignored

**E6 — Demo candidate build**: 45+ minute stable session, package for sharing

---

## TRACK 2 — WEBSITE (2–3 hrs/week max)

**W1 — /game page**: description + 3 screenshots + devlog entry
**W2 — /portfolio update**: add game project card, AI workflow article
**W3 — /shop page**: product grid + PayPal buttons + sold overlay + inquiry form
**W4 — /youtube placeholder**: coming soon + channel link
**W5 — Demo embed**: when Unity WebGL build is stable, embed on /game

---

## WEEKLY METRICS

Track every Friday:
- Tasks completed vs planned
- Critical bugs opened / closed
- Average FPS in benchmark scene
- Playtest sessions run
- Fun score (1–5 gut feeling)
- Scope changes (added / cut / deferred)

If metrics worsen 2 consecutive weeks → freeze features, stabilize.
