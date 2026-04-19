---
description: When the user says "@update", update sprint plan and design doc to reflect current progress.
globs: ["**/*"]
---

# @update Trigger

When the user types `@update` in chat:

1. **Read** `.cursor/plans/current-sprint.md`
2. **Mark completed tasks** — check off acceptance criteria and update status for anything finished in this session
3. **Mark in-progress tasks** — flag whatever we're currently working on
4. **Update `game/WORLD_DEMO_DESIGN.md`** — sync the Development Phases section with actual progress
5. **Report back** — brief summary of what changed:
   - Tasks completed since last update
   - Current task in progress
   - Next task up
   - Any blockers or open decisions
