---
name: visual-check
description: Run a visual QA pass using diagnostic tools.
---

1. Enter play mode in Unity
2. Position the camera at the current player spawn
3. Capture a screenshot via `python tests/diagnose.py --screenshot`
4. Analyze the screenshot:
   - Does the color palette match the visual bible?
   - Are proportions consistent with the scale contract?
   - Is fog present and warm-tinted?
   - Are there any default URP materials visible (grey/pink)?
5. Report findings and suggest fixes if needed
