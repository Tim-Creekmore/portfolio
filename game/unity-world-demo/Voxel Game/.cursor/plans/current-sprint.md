# Current Sprint: Phase A — Proof of Fantasy

**Phase:** A (Weeks 1–2)
**Goal:** Prove the core gameplay loop — commander toggle, death, combat, squads — in a simple grass arena.

**Terrain note:** World is simplified to a grass field for Phase A. Biome polish deferred to Phase B.

---

## Sprint A1: Commander Toggle + Camera

### Task A1.1 — Camera state machine scaffold
- **Type:** code
- **Spec:** CameraStateMachine.cs with HeroCamera (first-person) and CommanderCamera (top-down tactical, 15-20u above player, WASD pan, scroll zoom)
- **Acceptance criteria:**
  - [ ] Both camera states exist as separate classes
  - [ ] No transition yet, just the two modes
- **Status:** [ ] Not started

### Task A1.2 — Commander toggle input binding
- **Type:** code
- **Depends on:** A1.1
- **Spec:** Tab key toggles HeroCamera ↔ CommanderCamera via new Input System
- **Acceptance criteria:**
  - [ ] Tab reliably switches camera perspective
  - [ ] No visual glitches during transition
- **Status:** [ ] Not started

### Task A1.3 — Camera transition smoothing
- **Type:** code
- **Depends on:** A1.2
- **Spec:** Lerp between positions over 0.5s, lock player input during transition
- **Acceptance criteria:**
  - [ ] Smooth transition, no snap-cut or jitter
  - [ ] Player can't move/attack during blend
- **Status:** [ ] Not started

### Task A1.4 — Commander cursor + selection
- **Type:** code
- **Depends on:** A1.3
- **Spec:** In commander mode, show cursor, raycast to ground, draw selection circle at hit point
- **Acceptance criteria:**
  - [ ] Cursor visible in commander mode, hidden in hero mode
  - [ ] Raycast hits terrain correctly
- **Status:** [ ] Not started

---

## Sprint A2: Death System

### Task A2.1 — Player health component
- **Type:** code
- **Spec:** PlayerHealth.cs — max HP 100, TakeDamage(float), OnDeath event
- **Status:** [ ] Not started

### Task A2.2 — Death marker system
- **Type:** code
- **Depends on:** A2.1
- **Spec:** On death, spawn DeathMarker at player position. Store inventory ref. 300s timer.
- **Status:** [ ] Not started

### Task A2.3 — Respawn flow
- **Type:** code
- **Depends on:** A2.2
- **Spec:** Death → fade black → teleport to spawn → fade in → restore HP → inventory empty
- **Status:** [ ] Not started

### Task A2.4 — Death marker retrieval
- **Type:** code
- **Depends on:** A2.3
- **Spec:** Walk to marker → press E → items return → marker destroyed
- **Status:** [ ] Not started

---

## Sprint A3: Hero Combat Baseline
*(Tasks A3.1–A3.7 — weapon data, combat state machine, directional attacks, hit detection, blocking, stamina, HUD)*
- **Status:** [ ] Not started — full breakdown in master plan

---

## Sprint A4: Squad Basics
*(Tasks A4.1–A4.5 — unit data, unit AI state machine, squad manager, commander orders, unit death)*
- **Status:** [ ] Not started — full breakdown in master plan

---

## Sprint A5: Test Scene + Playtest
*(Tasks A5.1–A5.7 — arena, enemy placement, ambient, save/load, 3 playtest sessions)*
- **Status:** [ ] Not started — full breakdown in master plan
