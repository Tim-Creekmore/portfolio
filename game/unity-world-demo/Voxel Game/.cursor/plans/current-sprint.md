# Current Sprint: Phase A — Proof of Fantasy

**Phase:** A (Weeks 1–2)
**Goal:** Prove the core gameplay loop — commander toggle, death, combat, squads — in a simple grass arena.

**Terrain note:** World is simplified to a grass field for Phase A. Biome polish deferred to Phase B.

---

## Sprint A1: Commander Toggle + Camera ✅ COMPLETE

### Task A1.1–A1.3 — Camera state machine + toggle + transitions
- **Type:** code
- **Delivered:** `CameraStateMachine.cs` — three modes (Hero, ThirdPerson, Commander) cycling via V key
- **Acceptance criteria:**
  - [x] Three camera states in single state machine
  - [x] V key cycles Hero → ThirdPerson → Commander → Hero
  - [x] Smooth 0.4s smoothstep transition between modes
  - [x] Player input frozen during transitions and commander mode
  - [x] Commander: top-down view, WASD pan, scroll zoom (10–40u height)
  - [x] Cursor visible in commander mode, locked in hero/TP
- **Status:** [x] Complete

### Task A1.4 — Commander cursor + selection
- **Type:** code
- **Spec:** In commander mode, raycast to ground, draw selection circle at hit point
- **Acceptance criteria:**
  - [x] Cursor visible in commander mode, hidden in hero mode
  - [x] Raycast hits terrain + enemies — right-click ground = rally, right-click enemy = attack target
- **Status:** [x] Complete — delivered in A4

---

## Sprint A2: Death System ✅ COMPLETE

### Task A2.1 — Player health component
- **Delivered:** `PlayerHealth.cs` — HP 100, TakeDamage, Heal, ResetHealth, events
- **Status:** [x] Complete — tested via debug P key

### Task A2.2 — Death marker system
- **Delivered:** `DeathMarker.cs` — red cross, bobs + rotates, 300s timer. `DeathSystem.cs` — spawns marker while screen is black
- **Status:** [x] Complete — marker spawns correctly, visible after respawn

### Task A2.3 — Respawn flow
- **Delivered:** `DeathSystem.cs` + `ScreenFade.cs` — fade out → marker spawn → teleport → restore HP → fade in. Safety snap on spawn.
- **Status:** [x] Complete — tested, no falling through terrain

### Task A2.4 — Death marker retrieval
- **Delivered:** `Interactor.cs` — R key within 2.5u destroys marker, shows "Items retrieved" text
- **Status:** [x] Complete — tested

### Also delivered (A3 partial):
- `CombatHUD.cs` — health bar (anchor-scaled, visually shrinks), stamina bar (placeholder), damage text feedback
- Debug P key for testing damage (will be removed when real enemies exist)

---

## Sprint A3: Hero Combat Baseline ✅ COMPLETE

### Task A3.1 — Weapon data architecture
- **Delivered:** `WeaponData.cs` ScriptableObject + "Iron Sword" instance (20 dmg, 0.25s windup, 1.8u range)
- **Status:** [x] Complete

### Task A3.2 — Combat state machine
- **Delivered:** `CombatSystem.cs` — Idle → WindUp → Swing → Recovery → Idle, Blocking on right-click
- **Status:** [x] Complete

### Task A3.3 — Directional attacks (Option E — crosshair zone)
- **Delivered:** Crosshair on zone box within 2.5u = aimed attack. Outside range = random direction. Zone-name-based detection.
- **Status:** [x] Complete — tested, responsive

### Task A3.4 — Hit detection
- **Delivered:** SphereCast during Swing state, per-direction damage multipliers, jump attack 1.5x (always overhead)
- **Status:** [x] Complete — tested with test dummy

### Task A3.5 — Blocking system
- **Delivered:** Right-click sword block (50% damage, horizontal guard pose), shield toggle (1 key, 0% damage, shield push-forward animation), HUD shows damage + [BLOCKED]/[SHIELD] tag, shield full-block shows "BLOCKED 15 [SHIELD]"
- **Status:** [x] Complete — tested, both block types verified

### Task A3.6 — Stamina system + Sprint
- **Delivered:** `PlayerStamina.cs` — max 100, regen 5/sec with 1s delay after use, `TryConsume()` gates actions, `Drain()` for continuous use
- **Stamina costs:** Attack = 15, Block hold = 10/sec, Sprint = 12/sec
- **Sprint:** Hold Shift on land to sprint (7.5u/s vs 4.2u/s walk), drains stamina, can't sprint when empty, faster head bob for feedback
- **Integration:** CombatSystem gates attacks/blocks on stamina, auto-drops block when empty, DeathSystem resets stamina on respawn, CombatHUD yellow bar updates in real-time
- **Status:** [x] Complete — tested

### Task A3.7 — Combat HUD
- **Delivered early in A2:** Health bar + stamina bar + damage text. Stamina bar now live (yellow, anchor-scaled, color shifts with ratio).
- **Status:** [x] Complete

### Also delivered:
- `TestDummy.cs` — 4-zone block dummy (head/left/right/legs), color-coded, 999k HP, damage popups
- `AttackDummy.cs` — red dummy with 3-phase sword animation (wind-up → swing → recovery), attacks every 2.5s within range
- `TargetHealth.cs` — enemy health component with directional hit events
- `Billboard.cs` — camera-facing utility
- Placeholder sword visual (blade/handle/guard), curve-based swing animations
- Placeholder shield visual (wooden face + rim), animated block pose
- Sword/shield hide in ThirdPerson/Commander mode
- `CombatHUD.cs` — HP bar updates on all damage sources, shows block type indicators

---

## Sprint A4: Squad Basics ✅ COMPLETE

### Task A4.1 — Unit data architecture
- **Delivered:** `UnitData.cs` ScriptableObject — unit name, HP, damage, attack interval/range, speed, follow distance, threat detection range, morale threshold
- **Instance:** "Militia" — HP 60, dmg 10, speed 3.5, attack range 1.8u, detect 12u, morale threshold 30
- **Status:** [x] Complete

### Task A4.2 — Unit behavior state machine
- **Delivered:** `UnitAI.cs` — 4 states: Following (formation around player/rally), HoldPosition (face threats, attack in range), Attacking (move to + attack target, auto-scan), Retreating (placeholder for morale system in B3)
- **Features:** Formation offsets (circle, 60° spacing), threat scanning via `EnemyTag`, CharacterController movement, gravity, closest-target re-evaluation (1.5s), `_commanderOrdered` flag distinguishes player orders from AI autonomy
- **Status:** [x] Complete — tested all states

### Task A4.3 — Squad manager
- **Delivered:** `SquadManager.cs` — max 6 units, `SetAllState()`, `SetRallyPoint()`, `SetAttackTarget()`, `AliveCount`/`TotalCount` (tracks starting count), `ResetSquad()` for respawn
- **Status:** [x] Complete — tested, squad count HUD shows alive/total correctly

### Task A4.4 — Commander mode orders
- **Delivered:** `CommanderInput.cs` — in commander mode: right-click ground = rally point (blue marker, units move there), right-click enemy = attack target (units charge), H = hold position (melee-only defense), F = follow player (restores autonomous behavior)
- **Status:** [x] Complete — tested all 4 order types

### Task A4.5 — Unit health + death
- **Delivered:** `UnitHealth.cs` — Init(maxHP), TakeDamage, OnDeath event, transparent white death visual, 3s self-cleanup. Player sword hits both `TargetHealth` and `UnitHealth`. Full respawn on player death via `UnitSpawner.cs`.
- **Status:** [x] Complete — tested, enemies and friendlies die correctly

### Also delivered:
- `EnemyTag.cs` — marker component for enemy identification
- `UnitSpawner.cs` — runtime unit creation for respawn (friendly + enemy), reflection-based field injection
- `CommanderInput.cs` — debug logging for all orders, camera fallback for raycasts
- 4 friendly blue militia units (follow player, auto-engage, respond to orders)
- 4 red enemy units (hold position, detect at 12u, attack closest target including player)
- Rally point visual marker (blue transparent cylinder, 8s lifetime)
- Squad count HUD — top-left, shows "Squad: 3/4" format, updates on death, resets on respawn
- Outgoing damage toast — center screen, gentle fade
- Death visual — transparent white for all units (friendly + enemy), 3s cleanup
- Friendly fire enabled (intentional)
- AI targets closest enemy (re-evaluates every 1.5s)

---

## Sprint A5: Test Scene + Playtest [~] IN PROGRESS

### Task A5.1 — Micro encounter arena
- **Delivered:** 50×50 fenced arena (wood posts + double rails), 12 large boulders for cover (1.8–4.2u wide, up to 3u tall), 2 torch lights with warm point lights, visual bible lighting (sun #ffcc88 35°, fog #c8b898, ambient #886644), spawn at south end
- **Status:** [x] Complete

### Task A5.2 — Enemy placement
- **Delivered:** 8 enemy militia (red) spread across north half of arena — strategic positions among boulders, aggro at 15u range, hold-position until approached, attack closest target
- **Status:** [x] Complete

### Task A5.3 — Ambient pass
- **Delivered:** SSAO renderer feature (radius 0.4, intensity 1.0), shadow settings (soft, 80u dist, 4 cascades), procedural ambient audio (wind gusting loop + distant bird chirps), fog + torches carried from A5.1
- **Status:** [x] Complete

### Task A5.4 — Save/load minimal state
- **Delivered:** `SaveSystem.cs` — F5 to save, F9 to load. Persists player position/HP/stamina + squad unit positions/HP to `save.json` in `Application.persistentDataPath`. Added `SetHP`/`SetStamina` setters to health/stamina components.
- **Status:** [x] Complete

### Task A5.5 — Playtest session 1
- **Delivered:** Notes in `playtest-notes-A1.md`. Top 3 blockers identified: combat feedback overhaul (hit flash + enemy wind-up + shield size), stamina rebalance (bigger pool + no block cost), movement fixes (lower jump, fence collision).
- **Status:** [x] Complete

### Task A5.6 — Blocker fixes (budget: 2 days)
- **Delivered:**
  1. **Combat feedback** — full-screen red hit flash (alpha scales with damage), soft cyan block flash on shield-blocks, enemy units now have proper sword wind-up/swing/recovery animation (0.55s telegraph), idle shield scaled to 55% and pushed off-screen-edge
  2. **Stamina rebalance** — max stamina 100→250, regen 5→14/s, regen delay 1.0s→0.6s, blocking is fully free (no cost in data or code)
  3. **Movement & collision** — jump velocity 5.5→4.6, gravity 12→18 (snappier landings, less moon), fence rails now have solid colliders + wider hitbox
- **Status:** [x] Complete

### Task A5.7 — Playtest sessions 2 & 3
- **Spec:** Two more rounds, same documentation, compare notes, go/no-go decision for Phase B
- **Status:** [ ] Not started ← **CURRENT**
