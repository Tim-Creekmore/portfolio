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
