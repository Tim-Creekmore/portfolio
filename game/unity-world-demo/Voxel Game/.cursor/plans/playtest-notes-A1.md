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
