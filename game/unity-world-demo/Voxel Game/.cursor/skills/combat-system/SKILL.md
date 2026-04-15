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
