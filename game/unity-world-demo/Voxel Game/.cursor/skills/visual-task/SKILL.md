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
2. Enter play mode and capture screenshot from player camera
3. Compare screenshot against visual bible palette
4. Check scale against scale contract
5. If wrong: iterate. If right: commit and update phase board.
