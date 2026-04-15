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
