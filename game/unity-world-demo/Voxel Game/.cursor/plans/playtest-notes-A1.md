# Playtest Notes — Session A1

**Build:** Phase A complete (A1–A5.4)
**Duration:** ~15 minutes
**Tester:** Tim

---

## What felt fun
- Jumping around on top of the terrain is enjoyable
- Directional attacks land reliably (with some difficulty, but acceptable)
- Blocking mechanic itself feels fine

---

## What felt broken / annoying

### Movement
- **Jumping feels like the moon** — too floaty/high. Doesn't need real-Earth gravity but should be toned down
- **Fence collision is half-broken** — vertical posts block movement, but horizontal rails let you walk straight through

### Combat — Damage feedback (biggest pain point)
- **Receiving hits is unclear** — both damaging and non-damaging hits give weak feedback
- Often don't realize HP is critical until looking down at the bar
- **Enemy has no swing animation** — impossible to predict/dodge incoming attacks
- **Shield visual takes up too much screen** when held but not actively blocking

### Stamina
- **Drains way too fast** — running + jumping + attacking + blocking depletes the bar before fight starts
- **Blocking should not cost stamina** at all
- Considered removing stamina entirely → **deferred decision**, will increase pool size for now

### Everything else
- Felt fine

---

## Top 3 Blockers (for A5.6)

1. **Combat feedback overhaul**
   - Add screen-edge red vignette / hit flash when player takes damage
   - Add enemy attack wind-up animation (telegraphed sword raise) so blocks are reactive
   - Shrink shield visual when held but not actively blocking

2. **Stamina rebalance**
   - Increase max stamina pool significantly (200% or more)
   - Remove stamina cost from blocking entirely
   - Keep sprint, attack, jump costs as-is

3. **Movement & collision fixes**
   - Reduce jump height (~30% lower) and increase gravity slightly for snappier landings
   - Fix fence collision so horizontal rails are solid

---

## Out of scope (intentionally not addressed)
- Remove stamina system entirely — deferred to Phase B decision
- Real character animations / models — Phase B+
- Combat sound effects — later

---

# Playtest Notes — Session A2 (post-A5.6 fixes)

**Build:** A5.6 blocker fixes applied
**Tester:** Tim

## Outcome
- All three blocker fixes confirmed working and well-received
- "Loved the fixes" — combat feedback, stamina, jump/fence all feel right
- Session A3 scheduled for tomorrow

---

# Playtest Notes — Session A3 (final, Phase A exit gate)

**Build:** A5.6 fixes + jump polish
**Tester:** Tim

## Verdict: **GO for Phase B**

> "Everything we have built is solid and interesting to play. Especially taking into account this is beginning stage development. The core loop gets old and boring after the initial loop — but thankfully this won't be the entire focus of the game. What we do have here works."

## Minor tweak applied (in-scope polish)
- Jump felt slightly too snappy. Gravity 18 → 14.5, jump velocity 4.6 → 4.8. A touch more hang time without being moon-y.

## Out of scope — deferred to Phase B
- **Throwing weapons + bows** — natural fit for Sprint B2 (weapon equip system) and B3 (archer squads)

## Phase B priorities surfaced from this playtest
- Core loop is fun but thin — needs more content variety (biomes, enemy types, weapons, objectives) to stay engaging beyond 15 minutes
- Phase B plan addresses this via: B1 biomes, B2 inventory/weapon variety, B3 larger field battles, B4 dungeon

## Late A5.7 fixes (in-scope save/load polish)
- **Save/load enemy state** — F9 now restores all enemies (alive + dead at save time), wipes aggro, and snaps everyone to saved positions/HP. Achieved by routing Load through `UnitSpawner.RespawnAll()` then applying snapshots by `FormationIndex`.
