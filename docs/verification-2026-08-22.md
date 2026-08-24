# Task 12 — Full Parity Verification

**Date:** 2026-08-22
**Branch:** astro-consolidation
**Commit verified:** `7f29e348251323cf6e648d4af62cd22cef2e9fb1` ("fix: sandbox link checker to dist/, check og/twitter images and fragment ids")

This is the gate before cutover. It does not fix anything found — it reports.
Requirements for this run are defined by
`.superpowers/sdd/2026-08-21-astro-consolidation/task-12-CORRECTION.md`, which
supersedes `task-12-brief.md`.

**Result: PASS. All 9 steps came out as expected. No defects found.**

---

## Step 1 — Clean build from scratch

```
rm -rf dist
npm run build
```

Exit code: `0`

```
> timothycreekmore-com@1.0.0 build
> npm run check:css && npm run build:css && npx astro build && npm run check:links

> timothycreekmore-com@1.0.0 check:css
> node scripts/check-css.mjs

public/assets/css/tailwind.css matches a fresh build:css output.

> timothycreekmore-com@1.0.0 build:css
> tailwindcss -i ./src/styles/tailwind.src.css -o ./public/assets/css/tailwind.css --minify

Rebuilding...

Done in 208ms.
[content] Syncing content
[content] Synced content
[types] Generated 222ms
[build] output: "static"
[build] mode: "static"
[build] directory: C:\Users\timcr\repos\TimsHouse\dist\
[build] Collecting build info...
[build] ✓ Completed in 238ms.
[build] Building static entrypoints...
[vite] ✓ built in 628ms
[build] ✓ Completed in 647ms.

 generating static routes 
▶ src/pages/game.astro
  └─ /game/index.html (+4ms)
▶ src/pages/notes/index.astro
  └─ /notes/index.html (+1ms)
▶ src/pages/portfolio.astro
  └─ /portfolio/index.html (+2ms)
▶ src/pages/projects/ai-engineering-sandbox.astro
  └─ /projects/ai-engineering-sandbox/index.html (+1ms)
▶ src/pages/projects/prophetcma.astro
  └─ /projects/prophetcma/index.html (+1ms)
▶ src/pages/projects/signature-extraction.astro
  └─ /projects/signature-extraction/index.html (+1ms)
▶ src/pages/resume.astro
  └─ /resume/index.html (+1ms)
▶ src/pages/index.astro
  └─ /index.html (+1ms)
▶ src/pages/notes/[slug].astro
  ├─ /notes/biomes-data-assets/index.html (+5ms)
  ├─ /notes/ocr-human-review/index.html (+3ms)
  └─ /notes/repair-debugging/index.html (+3ms)
✓ Completed in 50ms.

[@astrojs/sitemap] `sitemap-index.xml` created at `dist`
[build] 11 page(s) built in 953ms
[build] Complete!

> timothycreekmore-com@1.0.0 check:links
> node scripts/check-links.mjs

Checked 12 pages, 167 local links.
All local links resolve.
```

**Outcome: PASS.** Exit 0. `check:css`, `build:css`, `astro build`, and
`check:links` all ran and succeeded as a single chain.

---

## Step 2 — Exact page set

```
find dist -name "*.html" | sort
```

```
dist/game/index.html
dist/game/play/index.html
dist/index.html
dist/notes/biomes-data-assets/index.html
dist/notes/index.html
dist/notes/ocr-human-review/index.html
dist/notes/repair-debugging/index.html
dist/portfolio/index.html
dist/projects/ai-engineering-sandbox/index.html
dist/projects/prophetcma/index.html
dist/projects/signature-extraction/index.html
dist/resume/index.html
```

**Outcome: PASS.** Exactly the 12 files specified in the correction — including
`dist/game/play/index.html` (verbatim redirect stub copied from `public/`) and
excluding `dist/projects/unity-game-lab/index.html` (deliberately removed —
`unity-game-lab.mdx` has `url: /game/`). No `now/`, `uses/`, `shop/`,
`content/`, or `unity-game-lab/` output.

---

## Step 3 — Full visual suite against `dist` (astro mode), exact-zero tolerance, x3

Server: `node scripts/static-server.mjs dist 3000`, backgrounded; polled with
curl until `200`; single listener on port 3000 confirmed via `netstat` before
each run.

Command: `VISUAL_MODE=astro npx playwright test` (full suite: `pages.spec.js` +
`static-server.spec.js`, both projects — 28 tests total). Exit code captured
directly (not through a pipe), per the known `tail`/pipe exit-code trap.

**Run 1** — exit `0`:
```
Running 28 tests using 8 workers
  ✓  24 tests (pages.spec.js x22 across 11 pages x2 viewports, static-server.spec.js x2)
  -  4 skipped (now, uses — both viewports)

  4 skipped
  24 passed (4.7s)
```

**Run 2** — exit `0`:
```
  4 skipped
  24 passed (4.7s)
```

**Run 3** — exit `0`:
```
  4 skipped
  24 passed (4.7s)
```

**Outcome: PASS.** All three runs identical: **24 passed, 4 skipped, 0 failed**,
exit 0 every time. The 4 skips are `now` and `uses` across both viewports
(pages with `astro: null` — deliberately deleted). No instability across
repeated runs at `threshold: 0, maxDiffPixelRatio: 0`.

Server killed after this step; port 3000 confirmed free before Step 4.

---

## Step 4 — Definitive parity proof: original static site vs. committed baselines

This is the strongest evidence in this task. `public/assets` and
`public/MAEresults.png` were copied to the repo root
(`cp -r public/assets ./assets`, `cp public/MAEresults.png ./MAEresults.png`)
to make the original static HTML at the repo root servable again, exactly as
it was pre-migration.

Server: `node scripts/static-server.mjs . 3000` (repo root, not `dist`),
backgrounded, polled with curl until `200` (`/` and `/resume.html` both
verified 200), single listener confirmed via `netstat`.

Command: `VISUAL_MODE=static npx playwright test tests/visual/pages.spec.js`
against the **committed baselines**, no `--update-snapshots` flag.

Exit code: `1` (expected — see below). Full result:

```
Running 26 tests using 8 workers
  ✓ home (desktop, mobile)
  ✓ game (desktop, mobile)
  ✓ note-repair (desktop, mobile)
  ✓ note-ocr (desktop, mobile)
  ✓ note-biomes (desktop, mobile)
  ✓ proj-prophetcma (desktop, mobile)
  ✓ proj-signature (desktop, mobile)
  ✓ proj-sandbox (desktop, mobile)
  ✓ now (desktop, mobile)
  ✓ uses (desktop, mobile)
  ✘ portfolio (desktop, mobile)
  ✘ notes (desktop, mobile)
  ✘ resume (desktop, mobile)

  6 failed
    [desktop] › portfolio
    [desktop] › notes
    [desktop] › resume
    [mobile] › portfolio
    [mobile] › notes
    [mobile] › resume
  20 passed (8.6s)
```

Failure detail (pixel diffs, all against committed baselines):
- `portfolio` desktop: 1440x6565 expected vs 1440x6313 received, 60% pixels differ
- `portfolio` mobile: 390x10583 expected vs 390x10371 received, 63% pixels differ
- `notes` desktop: 16% pixels differ (same dimensions)
- `notes` mobile: 12% pixels differ (same dimensions)
- `resume` desktop: 1440x1104 expected vs 1440x1056 received, 13% pixels differ
- `resume` mobile: 816x1104 expected vs 816x1056 received, 19% pixels differ

**Outcome: PASS — parity confirmed.** Exactly the three declared intentional
changes differ, across both viewports, and nothing else does:

| page | why it differs |
| --- | --- |
| `portfolio` | folded "Open to / Good technical conversations" card |
| `notes` | navigation added to the notes index |
| `resume` | `.no-print` back-link / Download PDF bar |

All 20 other tests (10 pages x 2 viewports, including `now` and `uses`, which
run for real in static mode since they still exist as static pages) matched
their committed baselines pixel-for-pixel. This is the strongest available
evidence that the Astro build reproduces the original static site exactly,
except where a change was declared.

Cleanup: server killed, `rm -rf ./assets ./MAEresults.png`, `git status`
confirmed clean (`nothing to commit, working tree clean`, exit 0) — full
tracked state unaffected.

---

## Step 5 — Link check (detail)

```
node scripts/check-links.mjs
```
```
Checked 12 pages, 167 local links.
All local links resolve.
```
Exit: `0`.

**Outcome: PASS.** 12 pages, 167 local links, all resolve — matches the
expected counts exactly.

---

## Step 6 — Sitemap

```
test -f dist/sitemap-index.xml && echo SITEMAP_INDEX_EXISTS
```
→ `SITEMAP_INDEX_EXISTS`

`dist/sitemap-index.xml`:
```
<?xml version="1.0" encoding="UTF-8"?><sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"><sitemap><loc>https://www.timothycreekmore.com/sitemap-0.xml</loc></sitemap></sitemapindex>
```

`grep -c 'timothycreekmore.com' dist/sitemap-index.xml` → `1`

URLs actually listed in `dist/sitemap-0.xml`:
```
https://www.timothycreekmore.com/
https://www.timothycreekmore.com/game/
https://www.timothycreekmore.com/notes/
https://www.timothycreekmore.com/notes/biomes-data-assets/
https://www.timothycreekmore.com/notes/ocr-human-review/
https://www.timothycreekmore.com/notes/repair-debugging/
https://www.timothycreekmore.com/portfolio/
https://www.timothycreekmore.com/projects/ai-engineering-sandbox/
https://www.timothycreekmore.com/projects/prophetcma/
https://www.timothycreekmore.com/projects/signature-extraction/
https://www.timothycreekmore.com/resume/
```

**Outcome: PASS.** `dist/sitemap-index.xml` exists and references
`timothycreekmore.com`. The generated sitemap lists exactly the 11
Astro-rendered pages (the `game/play` redirect stub is correctly excluded, as
it's not an Astro route). No `now`, `uses`, or `unity-game-lab` entries.

---

## Step 7 — Social preview audit

`og:title` / `og:description` / `og:image` / `twitter:card` presence, all 12
built HTML files:

```
dist/game/index.html                                    og:title=1 og:description=1 og:image=1 twitter:card=1
dist/game/play/index.html                                og:title=0 og:description=0 og:image=0 twitter:card=0
dist/index.html                                          og:title=1 og:description=1 og:image=1 twitter:card=1
dist/notes/biomes-data-assets/index.html                 og:title=1 og:description=1 og:image=1 twitter:card=1
dist/notes/index.html                                    og:title=1 og:description=1 og:image=1 twitter:card=1
dist/notes/ocr-human-review/index.html                   og:title=1 og:description=1 og:image=1 twitter:card=1
dist/notes/repair-debugging/index.html                   og:title=1 og:description=1 og:image=1 twitter:card=1
dist/portfolio/index.html                                og:title=1 og:description=1 og:image=1 twitter:card=1
dist/projects/ai-engineering-sandbox/index.html          og:title=1 og:description=1 og:image=1 twitter:card=1
dist/projects/prophetcma/index.html                      og:title=1 og:description=1 og:image=1 twitter:card=1
dist/projects/signature-extraction/index.html            og:title=1 og:description=1 og:image=1 twitter:card=1
dist/resume/index.html                                   og:title=1 og:description=1 og:image=1 twitter:card=1
```

`og:image` value per page:
```
game                       -> https://www.timothycreekmore.com/assets/images/og-card.svg
index                      -> https://www.timothycreekmore.com/assets/images/og-card.svg
notes/biomes-data-assets   -> https://www.timothycreekmore.com/assets/images/og-card.svg
notes                      -> https://www.timothycreekmore.com/assets/images/og-card.svg
notes/ocr-human-review     -> https://www.timothycreekmore.com/assets/images/og-card.svg
notes/repair-debugging     -> https://www.timothycreekmore.com/assets/images/og-card.svg
portfolio                  -> https://www.timothycreekmore.com/assets/images/og-card.svg
projects/ai-engineering-sandbox -> https://www.timothycreekmore.com/assets/images/og-card.svg
projects/prophetcma        -> https://www.timothycreekmore.com/MAEresults.png
projects/signature-extraction   -> https://www.timothycreekmore.com/assets/images/og-card.svg
resume                     -> https://www.timothycreekmore.com/assets/images/og-card.svg
```

**Outcome: PASS.** All 11 Astro-generated pages carry all four tags.
`dist/game/play/index.html` (the raw redirect stub copied verbatim from
`public/`) correctly has none — it is not an Astro/`BaseLayout` page.
`dist/projects/prophetcma/index.html` is the one page with a page-specific
`og:image` (`MAEresults.png`); every other page carries the generic
`og-card.svg`, confirming the expected split.

---

## Step 8 — CSS freshness

```
npm run check:css
```
```
> timothycreekmore-com@1.0.0 check:css
> node scripts/check-css.mjs

public/assets/css/tailwind.css matches a fresh build:css output.
```
Exit: `0`.

**Outcome: PASS.** The committed stylesheet matches a fresh Tailwind build —
production (which rebuilds CSS before deploying) will render identically to
everything verified in this run.

---

## Step 9 — Accessibility spot-check

`id="main-content"` count and `href="#main-content"` skip-link count per page:

```
dist/game/index.html                                    main-content=1 skip-link=1
dist/game/play/index.html                                main-content=0 skip-link=0
dist/index.html                                          main-content=1 skip-link=1
dist/notes/biomes-data-assets/index.html                 main-content=1 skip-link=1
dist/notes/index.html                                    main-content=1 skip-link=1
dist/notes/ocr-human-review/index.html                   main-content=1 skip-link=1
dist/notes/repair-debugging/index.html                   main-content=1 skip-link=1
dist/portfolio/index.html                                main-content=1 skip-link=1
dist/projects/ai-engineering-sandbox/index.html          main-content=1 skip-link=1
dist/projects/prophetcma/index.html                      main-content=1 skip-link=1
dist/projects/signature-extraction/index.html            main-content=1 skip-link=1
dist/resume/index.html                                   main-content=0 skip-link=0
```

**Outcome: PASS.** All 10 `BaseLayout` pages have exactly one
`id="main-content"` and a matching skip-link. `resume` legitimately has
neither — it uses a standalone `ResumeLayout`, not a defect. `game/play` is
the raw meta-refresh redirect stub from `public/`, not a content page, so it
legitimately has neither either.

---

## Summary

| Step | Check | Result |
| --- | --- | --- |
| 1 | Clean build (`npm run build`) | PASS — exit 0 |
| 2 | Exact 12-file page set | PASS — exact match |
| 3 | Visual suite vs. `dist`, x3 | PASS — 24 passed / 4 skipped / 0 failed, all 3 runs identical |
| 4 | Visual suite vs. original static site | PASS — exactly the 3 declared pages differ (portfolio, notes, resume), both viewports; 20/20 other tests pixel-identical |
| 5 | Link check | PASS — 12 pages, 167 links, all resolve |
| 6 | Sitemap | PASS — exists, correct domain, correct 11 URLs, no dropped pages |
| 7 | Social preview audit | PASS — all 11 Astro pages carry all 4 tags; correct og:image split (prophetcma unique, rest generic) |
| 8 | CSS freshness | PASS — exit 0 |
| 9 | Accessibility spot-check | PASS — exactly one main-content + skip-link per content page; resume and game/play legitimately exempt |

**No defects found. The migration is at parity with the original static
site, modulo the three declared intentional changes. This build is ready for
Task 13.**

Working tree confirmed clean (`git status` → `nothing to commit, working tree
clean`) after this verification's temporary `assets/`/`MAEresults.png` restore
was cleaned up. `dist/` remains present (untracked, gitignored) as build
output from Step 1.
