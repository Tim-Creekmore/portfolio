# Design: Consolidate timothycreekmore.com onto a single Astro source of truth

Date: 2026-08-21
Status: Awaiting review

## Problem

The repo contains two parallel implementations of the same website.

1. **Static HTML** (shipping). 15 hand-written `.html` files served from the repo
   root by GitHub Pages. Nav, `<head>` metadata, and footer are duplicated in
   every file.
2. **Astro** (unfinished, untracked). `src/` holds working layouts, components,
   a typed content schema, and 7 MDX entries. `npm run astro:build` produces a
   complete `dist/`.

Neither is authoritative. The Astro build works but is far thinner than what is
live:

| Page | Static | Astro |
| --- | --- | --- |
| `/portfolio` | 536 lines / 31 KB | 27 lines / 4 KB |
| `/` | 147 lines / 10 KB | 20 lines / 4 KB |
| `/game` | 8 KB | 3 KB |

The plumbing was built; the content was never ported.

Duplication has already produced drift. `shop/`, `content/`, and `resume.html`
exist on disk, are absent from the nav, and are absent from the README's
structure section. Adding one project currently requires editing four files: a
new HTML page, the portfolio grid, `sitemap.xml`, and the home page.

## Goals

- One source of truth for site content.
- Adding a project or note is a one-file change.
- Pixel parity with the current live site at cutover.
- The portfolio reads as a current, complete credibility anchor.

## Non-goals

- No redesign. The visual system is not being changed.
- No multi-site infrastructure. The owner is undecided about future sites; this
  design keeps that decision cheap rather than pre-building for it.
- No new frameworks, no client-side JS framework, no CMS.

## Decisions

### D1. Finish the Astro migration rather than continue with static HTML

The stated purpose of the site is a credibility anchor, whose failure mode is
staleness. The four orphaned pages are empirical evidence of how the
hand-maintained version ages under a four-files-per-change edit cost. Fleshing
out 536 lines of hand-written HTML increases that cost immediately before more
content is added.

This also resolves the future multi-site question without deciding it. Subfolder
to subdomain to separate domain are all reversible via redirects; what makes
them expensive is content trapped in duplicated HTML. With a content collection,
relocating a section moves a folder.

### D2. Port markup verbatim

The entire visual identity is CSS, not markup: roughly 250 lines in
`assets/css/tailwind.src.css` scoped to `body[data-site-theme="warm-dark"]`.
It works by overriding Tailwind utility classes (`.bg-white`, `.text-gray-900`,
`.rounded-2xl`, and others) with `!important` under that selector.
`BaseLayout.astro` already sets the attribute.

Consequence: identical class names produce identical rendering. Substituting a
"better" utility class silently drops the theme. Ports copy markup; they do not
rewrite it.

`glass-panel`, `metric-card`, `eyebrow`, `warm-rule`, and `glow-link` are all
defined in `tailwind.src.css` and already used by the Astro components. No
custom class is missing.

### D3. Parity is verified by screenshot diff, not by judgment

Baseline screenshots of every live page at desktop and mobile widths are
captured before any change, into `tests/screenshots/baseline/`. After the port,
screenshots are recaptured and diffed. A page ships only when it matches.

### D4. Lean page set

Keep and port: `/`, `/portfolio`, `/game`, `/notes`, `/notes/<slug>`,
`/projects/<slug>`, `/resume`.

Delete: `shop/`, `content/`, `uses/`. Fold `now/` into the portfolio's existing
`#current-focus` section. Git retains the history.

Rationale: a half-built store and an unlinked toolbox page on a credibility page
read as abandoned. Fewer pages that are all current beats more pages that are
not.

### D5. Resume becomes an HTML page with a PDF download

`scripts/build-resume.mjs` already prints `resume.html` to `resume.pdf` via
headless Chrome, so `resume.html` is already the source of truth. It becomes an
Astro page at `/resume` with a PDF download button. HTML is indexable by search
engines; PDF is not.

This inverts the PDF pipeline. Today the script reads the hand-written
`resume.html` at the repo root. After the port that file no longer exists, so
the script must print from the **built** page at `dist/resume/index.html`. PDF
generation therefore becomes a post-build step: `astro build`, then
`build-resume.mjs`, then commit the regenerated `resume.pdf` into `public/`.

That ordering has one consequence worth stating plainly: `public/resume.pdf` is
an input to the build that is produced by the build. It is regenerated manually
when the resume content changes, not on every build, and the committed PDF is
what ships in between. This is acceptable because resume content changes rarely;
the alternative (two-pass CI build) is not worth the complexity.

Two existing defects are fixed as part of this:

- The script hardcodes `file:///C:/Users/timcr/repos/TimsHouse/resume.html` - an
  absolute path with incorrect casing that works only because Windows paths are
  case-insensitive. It is replaced with a path resolved relative to the repo
  root, pointed at the built output.
- `scripts/check-links.mjs` hardcodes a file list including `now/index.html` and
  `uses/index.html`. It is repointed at the built `dist/` output so it cannot
  drift from the real page set again.

### D6. Deploy via GitHub Actions

GitHub Pages currently serves the repo root, `dist/` is gitignored, and a stale
`dist/` sits beside hand-written HTML. A workflow runs `npm ci`, `npm run
build:css`, `astro build`, then the link check, and publishes `dist/` to Pages.

Build output is never committed again. This is the mechanism that prevents the
two-sources-of-truth problem from recurring.

`package-lock.json` currently exists but is untracked. It must be committed for
`npm ci` to work in CI.

## Target structure

```
public/                  served at site root by Astro
├── CNAME                www.timothycreekmore.com
├── robots.txt
├── resume.pdf           generated by build-resume.mjs
├── coverletter.pdf
├── MAEresults.png
├── assets/
│   ├── css/tailwind.css     built, committed
│   └── js/hub-nav.js
└── game/play/           Unity/Godot web export
src/
├── layouts/
│   ├── BaseLayout.astro     head, theme body, shared scripts
│   └── CaseStudyLayout.astro
├── components/
│   ├── Nav.astro  Footer.astro  GlassPanel.astro  MetricCard.astro
│   └── Seo.astro            OG, Twitter, JSON-LD Person
├── content/
│   ├── projects/*.mdx       4 entries
│   └── notes/*.mdx          3 entries
├── content.config.ts
└── pages/
    ├── index.astro  portfolio.astro  game.astro  resume.astro
    ├── notes/index.astro  notes/[slug].astro
    └── projects/[slug].astro
scripts/                 build-resume.mjs, check-links.mjs
.github/workflows/deploy.yml
```

`assets/` moves to `public/assets/` because Astro serves `public/` at the site
root; the existing `/assets/...` URLs are unchanged. The game's Unity and Godot
*source* directories stay outside `public/` - only the web export at
`game/play/` is served.

`sitemap.xml` is deleted; `@astrojs/sitemap` generates it.

## Gaps in the current Astro layer

These are the specific things a careless port would drop. Each is a copy, not a
design decision.

| Gap | Effect if missed |
| --- | --- |
| `BaseLayout` head has no OG or Twitter meta, no JSON-LD `Person`, no favicon | No link preview card when the URL is shared. The most damaging possible regression for a credibility anchor |
| `Nav.astro` has 3 links; the static nav has 9 | About, Now, Projects, Skills, Contact anchors disappear, as does `data-analytics` on Resume |
| devicon CDN stylesheet absent from `BaseLayout` | All 8 skill icons disappear |
| Body class hardcoded `bg-gray-100 text-gray-800 antialiased`; home uses `min-h-screen bg-slate-50 text-gray-800 antialiased` | Layout shift on `/` |
| Contact email-reveal and `?sent=1` confirmation script is page-scoped | Contact form loses its confirmation state |

The head metadata is extracted into `Seo.astro` so it is defined once and cannot
be dropped per-page again.

## Sequence

Phase 0 - Baseline. Capture Playwright screenshots of all live pages at 1440px
and 390px into `tests/screenshots/baseline/`. Record the current page inventory.

Phase 1 - Assets. Move `assets/`, `CNAME`, `robots.txt`, PDFs, images, and
`game/play/` into `public/`. Verify the built site still resolves every
`/assets/...` URL.

Phase 2 - Shell. Complete `BaseLayout` and `Nav`: add `Seo.astro`, the devicon
stylesheet, a `bodyClass` prop, the full 9-link nav, and the contact script as a
page-scoped slot.

Phase 3 - Port. Move each page's markup verbatim into its Astro page. Portfolio
sections (`#about`, `#current-focus`, `#projects`, `#skills`, `#cover-letter`,
`#contact`) become components where they repeat, and stay inline where they do
not. `now/` content merges into `#current-focus`.

Phase 4 - Verify. Rebuild, recapture screenshots, diff against baseline. Run the
repointed link check against `dist/`. Fix until clean.

Phase 5 - Cutover. Add the Actions workflow, point Pages at it, delete the
static duplicates and the orphaned directories, remove the committed `dist/`,
narrow `tailwind.config.js` content paths to `src/` and `public/`, update the
README.

Phase 6 - Content. The work this migration exists to make cheap: each of the 4
projects gets a real case study structured problem, approach, outcome; the
skills section states what was built with each technology rather than listing
logos; every link resolves and nothing is stale.

Phases 0 through 5 are mechanical and reversible. Phase 6 is the actual goal and
is deliberately last, so it is written once in its permanent home. Phase 6 is
content work, not migration work, and gets its own plan once Phase 5 lands.

### Deliberately deferred to Phase 6

The README states that CDN icon usage is being removed as pages are touched. The
devicon stylesheet is nevertheless carried across unchanged, because D3 requires
pixel parity and removing it changes the skills section visually. Replacing
devicon with inline SVG is a content decision, made in Phase 6 against a working
baseline rather than mid-migration.

## Risks

**A verbatim port is tedious and invites improvisation.** Mitigated by D3 - the
screenshot diff fails loudly when markup drifts.

**Pages deployment misconfiguration takes the site down.** Mitigated by
sequencing: the static site keeps serving through Phase 4, and cutover is a
single settings change that is reversible.

**`build-resume.mjs` depends on a local Chrome install.** It stays a manual,
locally-run step rather than a CI step; `resume.pdf` remains committed. Only the
hardcoded path is fixed.

**Tailwind purging.** `tailwind.config.js` currently scans both static HTML and
`src/`. Content paths are narrowed only in Phase 5, after the static files are
gone, so no class is purged while both trees exist.

## Success criteria

- `astro build` produces every page in the D4 list, and no others.
- Screenshot diff against the Phase 0 baseline is clean at both widths.
- Link check passes against `dist/`.
- Sharing the site URL produces a link preview card with title, description, and
  image.
- Adding a new project requires creating exactly one `.mdx` file.
- No hand-written `.html` and no committed `dist/` remain in the repo.
