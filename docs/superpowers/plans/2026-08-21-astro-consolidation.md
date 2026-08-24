# Astro Consolidation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace 15 hand-written HTML files with a single Astro source of truth, with byte-for-byte visual parity verified by screenshot diff.

**Architecture:** Work happens on a branch so `main` keeps serving the live site throughout. A Playwright visual-regression harness captures the current static site as a baseline first; every subsequent task is validated against that baseline. Pages are ported markup-verbatim into existing Astro layouts and components. Cutover is a merge plus a GitHub Pages source change, both reversible.

**Tech Stack:** Astro 5.8 (static output), `@astrojs/mdx`, `@astrojs/sitemap`, Tailwind 3.4 (pre-built CSS, committed), `@playwright/test` (visual regression), GitHub Actions + GitHub Pages, Node 24 local / Node 22 CI.

**Spec:** `docs/superpowers/specs/2026-08-21-astro-consolidation-design.md`

## Global Constraints

- **Port markup verbatim.** The theme in `assets/css/tailwind.src.css` works by overriding Tailwind utility classes under `body[data-site-theme="warm-dark"]`. Substituting a different utility class silently drops the theme. Copy classes exactly; never "improve" them.
- **Every page's `<body>` must keep `data-site-theme="warm-dark"` and its correct `data-hub` value.** `data-hub` drives active-link highlighting in `assets/js/hub-nav.js`.
- **Body classes differ per page** and are not interchangeable:
  - `/` → `min-h-screen bg-slate-50 text-gray-800 antialiased`
  - `/portfolio` → `bg-gray-100 text-gray-800`
  - `/game` → `min-h-screen bg-slate-950 text-gray-100 antialiased`
  - `/notes` → `bg-gray-100 text-gray-800`
- **Footer classes differ per page:**
  - `/` and `/notes` → `text-center text-gray-500 text-sm py-8 border-t border-gray-200 bg-white`
  - `/portfolio` → same, plus ` relative z-10`
  - `/game` → `text-center text-slate-500 text-sm py-8 border-t border-violet-500/20 bg-slate-900/40`
- **Screenshots must run with `reducedMotion: 'reduce'`.** The Vanta.NET hero animation is live WebGL and non-deterministic. `tailwind.src.css` hides `#vanta-bg canvas` under `prefers-reduced-motion: reduce`, which makes captures stable.
- **Do not commit `dist/`.** It is gitignored and stays that way.
- **Node version:** local is v24.14.0, CI pins 22. Do not use APIs newer than Node 22.
- **Site URL is `https://www.timothycreekmore.com`** — already set in `astro.config.mjs`. `public/CNAME` must contain `www.timothycreekmore.com` or the custom domain breaks.

---

## File Structure

**Created:**
- `playwright.config.js` — visual-regression config, two viewport projects, reduced motion
- `tests/visual/pages.spec.js` — one test per page, static/astro URL map
- `tests/visual/__snapshots__/` — committed baseline PNGs
- `src/components/Seo.astro` — all `<head>` metadata in one place (OG, Twitter, JSON-LD, favicon)
- `src/layouts/ResumeLayout.astro` — minimal print-clean layout, no site chrome
- `src/pages/resume.astro` — resume page
- `.github/workflows/deploy.yml` — build and deploy to Pages

**Modified:**
- `src/layouts/BaseLayout.astro` — add `Seo`, `bodyClass` prop, devicon stylesheet, script slot
- `src/components/Nav.astro` — 3 links → 9 links
- `src/components/Footer.astro` — add `class` prop
- `src/pages/index.astro` — stub → real home page
- `src/pages/portfolio.astro` — stub → real portfolio
- `src/pages/game.astro` — stub → real game page
- `src/pages/notes/index.astro`, `src/pages/notes/[slug].astro`, `src/pages/projects/[slug].astro`
- `scripts/build-resume.mjs` — fix hardcoded path, read from `dist/`
- `scripts/check-links.mjs` — walk `dist/` instead of a hardcoded list
- `package.json` — add Playwright, test scripts
- `tailwind.config.js` — narrow content paths (Task 13 only)
- `README.md` — update structure section

**Moved (Task 2):** `assets/`, `CNAME`, `robots.txt`, `resume.pdf`, `coverletter.pdf`, `MAEresults.png`, `game/play/` → under `public/`

**Deleted (Task 13):** all root `.html` files, `portfolio/`, `game/index.html`, `notes/*.html`, `now/`, `uses/`, `shop/`, `content/`, `sitemap.xml`, committed `dist/`

---

### Task 1: Visual-regression harness and baseline capture

Nothing else can be verified until the baseline exists. This task must complete before any file moves.

**Files:**
- Create: `playwright.config.js`
- Create: `tests/visual/pages.spec.js`
- Modify: `package.json`

**Interfaces:**
- Consumes: nothing (first task)
- Produces: `npm run visual:baseline` and `npm run visual:check` scripts; committed PNGs under `tests/visual/__snapshots__/` that every later task diffs against.

- [ ] **Step 1: Create the working branch**

```bash
git checkout -b astro-consolidation
```

`main` keeps serving the live site until Task 13.

- [ ] **Step 2: Install Playwright**

```bash
npm install --save-dev @playwright/test
npx playwright install chromium
```

- [ ] **Step 3: Create `playwright.config.js`**

```js
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/visual',
  snapshotDir: './tests/visual/__snapshots__',
  fullyParallel: true,
  reporter: 'list',
  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:3000',
    // Vanta.NET is live WebGL. tailwind.src.css hides #vanta-bg canvas under
    // prefers-reduced-motion, which is what makes these captures stable.
    reducedMotion: 'reduce',
  },
  expect: {
    toHaveScreenshot: {
      maxDiffPixelRatio: 0.01,
      animations: 'disabled',
    },
  },
  projects: [
    {
      name: 'desktop',
      use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 900 } },
    },
    {
      name: 'mobile',
      use: { ...devices['Desktop Chrome'], viewport: { width: 390, height: 844 }, isMobile: false },
    },
  ],
});
```

- [ ] **Step 4: Create `tests/visual/pages.spec.js`**

`VISUAL_MODE` selects which URL set to hit. The snapshot filename is the same in both modes, so the static run's PNGs become the baseline the Astro run is compared against.

```js
import { test, expect } from '@playwright/test';

const MODE = process.env.VISUAL_MODE || 'static';

// Static and Astro URLs differ for project and note detail pages.
// `astro: null` means the page is intentionally deleted in the migration
// (spec D4) and is baselined but never compared.
const PAGES = [
  { name: 'home',              static: '/',                              astro: '/' },
  { name: 'portfolio',         static: '/portfolio/',                    astro: '/portfolio/' },
  { name: 'game',              static: '/game/',                         astro: '/game/' },
  { name: 'notes',             static: '/notes/',                        astro: '/notes/' },
  { name: 'note-repair',       static: '/notes/repair-debugging.html',   astro: '/notes/repair-debugging/' },
  { name: 'note-ocr',          static: '/notes/ocr-human-review.html',   astro: '/notes/ocr-human-review/' },
  { name: 'note-biomes',       static: '/notes/biomes-data-assets.html', astro: '/notes/biomes-data-assets/' },
  { name: 'proj-prophetcma',   static: '/prophetcma.html',               astro: '/projects/prophetcma/' },
  { name: 'proj-signature',    static: '/signature-extraction.html',     astro: '/projects/signature-extraction/' },
  { name: 'proj-sandbox',      static: '/ai-engineering-sandbox.html',   astro: '/projects/ai-engineering-sandbox/' },
  { name: 'resume',            static: '/resume.html',                   astro: '/resume/' },
  { name: 'now',               static: '/now/',                          astro: null },
  { name: 'uses',              static: '/uses/',                         astro: null },
];

for (const p of PAGES) {
  test(`${p.name}`, async ({ page }) => {
    const path = MODE === 'static' ? p.static : p.astro;
    test.skip(path === null, `${p.name} is deleted in the Astro build`);

    await page.goto(path, { waitUntil: 'networkidle' });
    // The footer year is written by hub-nav.js; wait for it so captures are stable.
    await page.waitForFunction(() => {
      const el = document.getElementById('footer-year');
      return el === null || el.textContent.trim().length === 4;
    });
    await expect(page).toHaveScreenshot(`${p.name}.png`, { fullPage: true });
  });
}
```

- [ ] **Step 5: Add scripts to `package.json`**

Add these three entries to the `scripts` object:

```json
"serve:static": "npx serve . -l 3000 --no-clean-urls",
"serve:dist": "npx serve dist -l 3000 --no-clean-urls",
"visual": "playwright test"
```

`--no-clean-urls` is required: without it `serve` rewrites `/resume.html` to `/resume`, and the static baseline URLs stop resolving.

- [ ] **Step 6: Capture the baseline against the live static site**

In terminal 1:

```bash
npm run serve:static
```

In terminal 2:

```bash
VISUAL_MODE=static npx playwright test --update-snapshots
```

Expected: 26 snapshots written — 13 pages times 2 viewports. PNGs land in `tests/visual/__snapshots__/pages.spec.js-snapshots/`. Of those 13, eleven have an Astro equivalent and are compared later; `now` and `uses` are baselined for reference but never compared, because the spec deletes them.

- [ ] **Step 7: Verify the baseline is stable by re-running it**

```bash
VISUAL_MODE=static npx playwright test
```

Expected: all tests PASS. If any page fails on a second identical run, it has a non-deterministic element — find it and neutralize it (mask it via `toHaveScreenshot`'s `mask` option) before continuing. A flaky baseline makes every later task unverifiable.

- [ ] **Step 8: Commit**

```bash
git add playwright.config.js tests/visual package.json package-lock.json
git commit -m "test: add visual regression harness and capture static baseline"
```

---

### Task 2: Move static assets into public/

**Files:**
- Move: `assets/` → `public/assets/`
- Move: `CNAME`, `robots.txt`, `resume.pdf`, `coverletter.pdf`, `MAEresults.png` → `public/`
- Move: `game/play/` → `public/game/play/`
- Modify: `tailwind.config.js`

**Interfaces:**
- Consumes: Task 1's baseline
- Produces: every `/assets/...`, `/resume.pdf`, `/coverletter.pdf`, `/MAEresults.png` URL resolves from the Astro build. URLs are unchanged because Astro serves `public/` at the site root.

- [ ] **Step 1: Move the files with git mv so history is preserved**

```bash
mkdir -p public/game
git mv assets public/assets
git mv CNAME public/CNAME
git mv robots.txt public/robots.txt
git mv resume.pdf public/resume.pdf
git mv coverletter.pdf public/coverletter.pdf
git mv MAEresults.png public/MAEresults.png
git mv game/play public/game/play
```

The Unity and Godot source directories (`game/unity-world-demo/`, `game/world-demo/`) are build sources, not web assets. They stay where they are.

- [ ] **Step 2: Point the Tailwind build at the new CSS location**

In `package.json`, change the `build:css` and `watch:css` scripts:

```json
"build:css": "tailwindcss -i ./public/assets/css/tailwind.src.css -o ./public/assets/css/tailwind.css --minify",
"watch:css": "tailwindcss -i ./public/assets/css/tailwind.src.css -o ./public/assets/css/tailwind.css --watch"
```

- [ ] **Step 3: Add the new asset path to `tailwind.config.js` content**

Add `"./public/assets/js/**/*.js"` to the `content` array. Do **not** remove the existing static HTML paths yet — the static files still exist and their classes must not be purged until Task 13.

- [ ] **Step 4: Rebuild CSS and the site**

```bash
npm run build:css
npx astro build
```

Expected: build succeeds, and `dist/assets/css/tailwind.css` exists.

- [ ] **Step 5: Verify assets resolve in the built output**

```bash
test -f dist/assets/css/tailwind.css && echo "CSS OK"
test -f dist/assets/js/hub-nav.js && echo "JS OK"
test -f dist/CNAME && echo "CNAME OK"
test -f dist/resume.pdf && echo "PDF OK"
```

Expected: four `OK` lines. If `CNAME` is missing from `dist/`, the custom domain will break at cutover — stop and fix.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor: move static assets into public/ for Astro"
```

---

### Task 3: Complete BaseLayout with full head metadata

The current `BaseLayout` drops every OG tag, the Twitter card, the JSON-LD `Person` block, and the favicon. Shipping it as-is means pasting your URL anywhere produces no preview card — the single worst regression for a page whose job is credibility.

**Files:**
- Create: `src/components/Seo.astro`
- Modify: `src/layouts/BaseLayout.astro`
- Reference: `portfolio/index.html:3-53` (the complete head to copy from)

**Interfaces:**
- Consumes: Task 2's `public/assets/` layout
- Produces:
  - `Seo.astro` props: `{ title: string, description: string, ogType?: string, ogTitle?: string, ogDescription?: string }`
  - `BaseLayout.astro` props: `{ title: string, description: string, active?: 'home'|'portfolio'|'game', bodyClass?: string, ogType?: string }` and two named slots: `head` and `scripts`.

- [ ] **Step 1: Create `src/components/Seo.astro`**

Copy the meta tags and JSON-LD from `portfolio/index.html:8-52` verbatim, parameterising only title/description/URL.

```astro
---
interface Props {
  title: string;
  description: string;
  ogType?: string;
  ogTitle?: string;
  ogDescription?: string;
}

const {
  title,
  description,
  ogType = 'website',
  ogTitle = title,
  ogDescription = description,
} = Astro.props;

const canonical = new URL(Astro.url.pathname, Astro.site).toString();
const ogImage = new URL('/assets/images/og-card.svg', Astro.site).toString();
const favicon = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64'%3E%3Crect width='64' height='64' rx='12' fill='%23007BA7'/%3E%3Ctext x='32' y='44' font-family='system-ui,sans-serif' font-size='36' font-weight='700' text-anchor='middle' fill='%23fff'%3ET%3C/text%3E%3C/svg%3E";

const personSchema = {
  '@context': 'https://schema.org',
  '@type': 'Person',
  name: 'Timothy Creekmore',
  url: 'https://www.timothycreekmore.com/',
  image: 'https://www.timothycreekmore.com/assets/images/og-card.svg',
  jobTitle: 'Junior Software Developer and AI/Data Builder',
  email: 'mailto:timcreekmore2002@gmail.com',
  alumniOf: { '@type': 'CollegeOrUniversity', name: 'Lyon College' },
  knowsAbout: ['Data Science', 'Machine Learning', 'AI Engineering'],
  sameAs: [
    'https://github.com/Tim-Creekmore',
    'https://www.linkedin.com/in/timcreekmore',
  ],
};
---
<title>{title}</title>
<meta name="description" content={description} />
<link rel="canonical" href={canonical} />
<link rel="icon" href={favicon} />

<meta property="og:type" content={ogType} />
<meta property="og:site_name" content="Timothy Creekmore" />
<meta property="og:title" content={ogTitle} />
<meta property="og:description" content={ogDescription} />
<meta property="og:url" content={canonical} />
<meta property="og:image" content={ogImage} />
<meta property="profile:first_name" content="Timothy" />
<meta property="profile:last_name" content="Creekmore" />

<meta name="twitter:card" content="summary_large_image" />
<meta name="twitter:title" content={ogTitle} />
<meta name="twitter:description" content={ogDescription} />
<meta name="twitter:image" content={ogImage} />

<script type="application/ld+json" set:html={JSON.stringify(personSchema)} />
```

Before committing, open `portfolio/index.html:24-52` and confirm the `knowsAbout` and `sameAs` arrays above match the real JSON-LD block. The excerpt read during planning was truncated — copy the full arrays across.

- [ ] **Step 2: Rewrite `src/layouts/BaseLayout.astro`**

```astro
---
import Seo from '../components/Seo.astro';

interface Props {
  title: string;
  description: string;
  active?: 'home' | 'portfolio' | 'game';
  bodyClass?: string;
  ogType?: string;
}

const {
  title,
  description,
  active = 'home',
  bodyClass = 'bg-gray-100 text-gray-800',
  ogType = 'website',
} = Astro.props;
---
<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <Seo title={title} description={description} ogType={ogType} />
    <link rel="stylesheet" href="/assets/css/tailwind.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/gh/devicons/devicon@v2.16.0/devicon.min.css" />
    <slot name="head" />
  </head>
  <body class={bodyClass} data-hub={active} data-site-theme="warm-dark">
    <a href="#main-content" class="skip-link">Skip to main content</a>
    <div id="vanta-bg" class="fixed inset-0 -z-10" aria-hidden="true"></div>
    <slot />
    <script src="/assets/js/hub-nav.js" defer></script>
    <slot name="scripts" />
  </body>
</html>
```

The devicon stylesheet is required — without it all 8 skill icons on the portfolio disappear. The spec marks removing it as a Phase 6 decision, not a migration one.

- [ ] **Step 3: Build and assert the metadata is present**

```bash
npx astro build
grep -c 'og:image' dist/index.html
grep -c 'application/ld+json' dist/index.html
grep -c 'devicon' dist/index.html
grep -c 'rel="icon"' dist/index.html
```

Expected: each command prints `1` or more. A `0` on any line means that tag is still missing.

- [ ] **Step 4: Commit**

```bash
git add src/components/Seo.astro src/layouts/BaseLayout.astro
git commit -m "feat: restore full head metadata in BaseLayout via Seo component"
```

---

### Task 4: Bring Nav and Footer to parity

`Nav.astro` has 3 links; the real nav has 9. `Footer.astro` has no way to express the per-page class variants.

**Files:**
- Modify: `src/components/Nav.astro`
- Modify: `src/components/Footer.astro`
- Reference: `portfolio/index.html:58-86` (nav), `portfolio/index.html:493-502` (footer)

**Interfaces:**
- Consumes: Task 3's `BaseLayout`
- Produces:
  - `Nav.astro` props: `{ active?: 'home'|'portfolio'|'game', sectionPrefix?: string }` — `sectionPrefix` is `''` on the portfolio page (so anchors are `#about`) and `/portfolio/` everywhere else (so anchors are `/portfolio/#about`).
  - `Footer.astro` props: `{ class?: string }`

- [ ] **Step 1: Rewrite `src/components/Nav.astro`**

```astro
---
interface Props {
  active?: 'home' | 'portfolio' | 'game';
  sectionPrefix?: string;
}

const { active = 'home', sectionPrefix = '/portfolio/' } = Astro.props;

const sections = [
  { href: `${sectionPrefix}#about`, label: 'About' },
  { href: `${sectionPrefix}#current-focus`, label: 'Now' },
  { href: `${sectionPrefix}#projects`, label: 'Projects' },
  { href: `${sectionPrefix}#skills`, label: 'Skills' },
  { href: `${sectionPrefix}#contact`, label: 'Contact' },
];
---
<nav id="hub-navbar" class="fixed top-0 left-0 w-full z-50 bg-white/90 backdrop-blur-md border-b border-gray-200 shadow-sm">
  <div class="max-w-6xl mx-auto px-4 h-16 flex items-center justify-between gap-4">
    <a href="/" class="font-semibold text-gray-900 shrink-0 hover:opacity-80 transition" data-hub-link="home">Timothy Creekmore</a>
    <div class="hidden md:flex items-center gap-6 text-sm font-medium text-gray-600">
      <a href="/portfolio/" class="hover:text-teal-600 transition" data-hub-link="portfolio">Portfolio</a>
      <a href="/game/" class="hover:text-teal-600 transition" data-hub-link="game">Game</a>
      {sections.map((s) => (
        <a href={s.href} class="hover:text-teal-600 transition">{s.label}</a>
      ))}
      <a href="/resume.pdf" data-analytics="Resume Download Nav" class="hover:text-teal-600 transition">Resume</a>
    </div>
    <button type="button" id="hub-nav-toggle" class="md:hidden p-2 rounded-lg text-gray-700 hover:bg-gray-100" aria-label="Open menu" aria-expanded="false" aria-controls="hub-mobile-nav">
      <span aria-hidden="true">Menu</span>
    </button>
  </div>
  <div id="hub-mobile-nav" class="hidden md:hidden border-t border-gray-100 px-4 py-3 flex flex-col gap-1 text-sm font-medium text-gray-700 bg-white/95">
    <a href="/portfolio/" class="py-2 hover:text-teal-600" data-hub-link="portfolio">Portfolio</a>
    <a href="/game/" class="py-2 hover:text-teal-600" data-hub-link="game">Game</a>
    {sections.map((s) => (
      <a href={s.href} class="py-2 hover:text-teal-600">{s.label}</a>
    ))}
    <a href="/resume.pdf" class="py-2 hover:text-teal-600">Resume</a>
  </div>
</nav>
```

Open `portfolio/index.html:58-86` and confirm the mobile menu labels match. Planning observed the desktop menu using `About` and the mobile menu using `About (this page)` and `Current Focus`. If those differ in the source, reproduce the difference exactly rather than unifying it — parity first; tidying is Phase 6.

- [ ] **Step 2: Rewrite `src/components/Footer.astro`**

```astro
---
interface Props {
  class?: string;
}

const {
  class: className = 'text-center text-gray-500 text-sm py-8 border-t border-gray-200 bg-white',
} = Astro.props;
---
<footer class={className}>
  <p>&copy; <span id="footer-year">2026</span> Timothy Creekmore</p>
  <p class="mt-1 text-gray-400">
    <a href="/" class="hover:text-gray-700">Home</a>
    <span aria-hidden="true">&middot;</span>
    <a href="/portfolio/#contact" class="hover:text-gray-700">Contact</a>
    <span aria-hidden="true">&middot;</span>
    <a href="https://github.com/Tim-Creekmore" target="_blank" rel="noopener noreferrer" class="hover:text-gray-700">GitHub</a>
  </p>
</footer>
```

- [ ] **Step 3: Build and count the nav links**

```bash
npx astro build
grep -o 'hover:text-teal-600 transition' dist/index.html | wc -l
```

Expected: `8` (Portfolio, Game, 5 sections, Resume) in the desktop menu.

- [ ] **Step 4: Commit**

```bash
git add src/components/Nav.astro src/components/Footer.astro
git commit -m "feat: bring Nav and Footer to parity with static site"
```

---

### Task 5: Port the home page

**Files:**
- Modify: `src/pages/index.astro`
- Reference: `index.html:22-143`

**Interfaces:**
- Consumes: `BaseLayout`, `Nav`, `Footer` from Tasks 3–4
- Produces: `/` in the Astro build

- [ ] **Step 1: Replace the stub body of `src/pages/index.astro`**

Delete the placeholder copy — it currently reads "Astro source version of the homepage. The current GitHub Pages output still lives at the repository root while this migration scaffold matures." That text must not ship.

Copy `index.html` lines 50–95 (hero) and 96–132 (`<main>`) verbatim into the page body. The frontmatter and wrapper:

```astro
---
import BaseLayout from '../layouts/BaseLayout.astro';
import Nav from '../components/Nav.astro';
import Footer from '../components/Footer.astro';
---
<BaseLayout
  title="Timothy Creekmore — Software Developer, AI/Data Builder, Systems Thinker"
  description="Practical technologist building models, tools, and interactive systems."
  active="home"
  bodyClass="min-h-screen bg-slate-50 text-gray-800 antialiased"
  ogType="profile"
>
  <Nav active="home" />
  <!-- paste index.html:50-132 here, verbatim -->
  <Footer />
</BaseLayout>
```

Take `title` and `description` from `index.html:3-21` rather than the values above if they differ — the static head is the source of truth.

- [ ] **Step 2: Build and serve the Astro output**

```bash
npx astro build
npm run serve:dist
```

- [ ] **Step 3: Diff the home page against the baseline**

In a second terminal:

```bash
VISUAL_MODE=astro npx playwright test -g "^home$"
```

Expected: PASS on both `desktop` and `mobile`. On failure, open the diff PNG that Playwright writes to `test-results/` — it shows exactly which region drifted. Fix the markup, do not update the snapshot.

- [ ] **Step 4: Commit**

```bash
git add src/pages/index.astro
git commit -m "feat: port home page to Astro"
```

---

### Task 6: Port the portfolio page

The largest port: 536 lines, six sections, plus a page-scoped script.

**Files:**
- Modify: `src/pages/portfolio.astro`
- Reference: `portfolio/index.html` — `#main-content` hero `96-113`, `#about` `115-157`, `#current-focus` `159-189`, `#projects` `193-294`, `#skills` `296-399`, `#cover-letter` `401-411`, `#contact` `414-479`, page script `505-534`
- Reference: `now/index.html` (content folds into `#current-focus`)

**Interfaces:**
- Consumes: `BaseLayout`, `Nav` (with `sectionPrefix=""`), `Footer`
- Produces: `/portfolio/` in the Astro build

- [ ] **Step 1: Port the six sections verbatim**

```astro
---
import BaseLayout from '../layouts/BaseLayout.astro';
import Nav from '../components/Nav.astro';
import Footer from '../components/Footer.astro';
---
<BaseLayout
  title="Portfolio | Timothy Creekmore — Software Developer &amp; AI/Data Builder"
  description="Portfolio of Timothy Creekmore — junior software developer, AI/data builder, and systems thinker. Projects in software, ML, OCR, AI workflows, and Unity systems."
  active="portfolio"
  bodyClass="bg-gray-100 text-gray-800"
  ogType="profile"
>
  <Nav active="portfolio" sectionPrefix="" />
  <!-- paste portfolio/index.html:96-479 here, verbatim -->
  <Footer class="text-center text-gray-500 text-sm py-8 border-t border-gray-200 bg-white relative z-10" />
  <script slot="scripts" is:inline>
    // paste portfolio/index.html:506-533 here, verbatim
  </script>
</BaseLayout>
```

`is:inline` is required. Without it Astro will process and bundle the script, which changes its execution timing and can break the `?sent=1` form confirmation.

- [ ] **Step 2: Do not port the projects grid to the content collection yet**

`portfolio/index.html:193-294` is a hand-written grid. Leave it hand-written in this task. Converting it to read from `getCollection('projects')` changes rendered output and would fail the parity gate. That conversion happens in Task 9, where the case-study pages it links to also change.

- [ ] **Step 3: Prove parity BEFORE making any intentional change**

The `/now` fold is the one deliberate visual change in this migration. Verify the straight port first, so a parity failure can never be confused with an intended edit.

```bash
npx astro build
npm run serve:dist   # terminal 1
VISUAL_MODE=astro npx playwright test -g "^portfolio$"
```

Expected: PASS on both viewports. Do not proceed to Step 4 until this is green.

- [ ] **Step 4: Fold the `/now` content into `#current-focus`**

Open `now/index.html`. Any item there not already present in `portfolio/index.html:159-189` gets added as an additional card matching the existing card markup exactly. If `/now` contains nothing new, add nothing and say so in the commit message.

The portfolio snapshot will now fail — that is the expected, intended change. Confirm the diff PNG shows only the new card, then re-baseline this one page:

```bash
npx astro build
VISUAL_MODE=astro npx playwright test -g "^portfolio$" --update-snapshots
```

Review the new PNGs by eye before committing them.

- [ ] **Step 5: Verify the contact script survived**

```bash
grep -c 'contact-email' dist/portfolio/index.html
grep -c "sent" dist/portfolio/index.html
```

Expected: both `1` or more. Then load `http://localhost:3000/portfolio/?sent=1` in a browser and confirm the thank-you message appears, and that hovering the contact email reveals the address.

- [ ] **Step 6: Commit**

```bash
git add src/pages/portfolio.astro tests/visual/__snapshots__
git commit -m "feat: port portfolio page to Astro and fold in /now content"
```

---

### Task 7: Port the game page

**Files:**
- Modify: `src/pages/game.astro`
- Reference: `game/index.html:22-132`

**Interfaces:**
- Consumes: `BaseLayout`, `Nav`, `Footer`
- Produces: `/game/` in the Astro build

- [ ] **Step 1: Port the page**

```astro
---
import BaseLayout from '../layouts/BaseLayout.astro';
import Nav from '../components/Nav.astro';
import Footer from '../components/Footer.astro';
---
<BaseLayout
  title="Game Lab | Timothy Creekmore"
  description="Voxel game demo and Unity systems work."
  active="game"
  bodyClass="min-h-screen bg-slate-950 text-gray-100 antialiased"
>
  <Nav active="game" />
  <!-- paste game/index.html:47-120 here, verbatim -->
  <Footer class="text-center text-slate-500 text-sm py-8 border-t border-violet-500/20 bg-slate-900/40" />
</BaseLayout>
```

Take the real `title` and `description` from `game/index.html`'s head rather than the placeholders above.

- [ ] **Step 2: Verify the playable build still links correctly**

```bash
npx astro build
grep -o 'href="/game/play[^"]*"' dist/game/index.html
test -f public/game/play/index.html && echo "PLAY BUILD PRESENT"
```

Expected: the link is printed and `PLAY BUILD PRESENT` appears. If the game page links to a path under `/game/play/` that does not exist in `public/`, the play button is broken — fix before continuing.

- [ ] **Step 3: Diff against baseline**

```bash
npm run serve:dist   # terminal 1
VISUAL_MODE=astro npx playwright test -g "^game$"
```

Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add src/pages/game.astro
git commit -m "feat: port game page to Astro"
```

---

### Task 8: Port the notes index and note detail pages

**Files:**
- Modify: `src/pages/notes/index.astro`, `src/pages/notes/[slug].astro`
- Reference: `notes/index.html:11-40`, `notes/repair-debugging.html`, `notes/ocr-human-review.html`, `notes/biomes-data-assets.html`
- Verify: `src/content/notes/*.mdx` (3 files already exist)

**Interfaces:**
- Consumes: `BaseLayout`, `Nav`, `Footer`; the `notes` collection defined in `src/content.config.ts` with schema `{ title, summary, pubDate: Date, tags: string[] }`
- Produces: `/notes/` and `/notes/<slug>/`

- [ ] **Step 1: Confirm the three MDX files carry the real note content**

```bash
wc -l src/content/notes/*.mdx
wc -l notes/repair-debugging.html notes/ocr-human-review.html notes/biomes-data-assets.html
```

If an MDX file is materially shorter than its HTML counterpart, its body is a stub. Copy the real prose across from the HTML before continuing — a passing build with empty notes is worse than a failing one.

- [ ] **Step 2: Port the notes index**

```astro
---
import BaseLayout from '../../layouts/BaseLayout.astro';
import Nav from '../../components/Nav.astro';
import Footer from '../../components/Footer.astro';
import { getCollection } from 'astro:content';

const notes = (await getCollection('notes'))
  .sort((a, b) => b.data.pubDate.valueOf() - a.data.pubDate.valueOf());
---
<BaseLayout
  title="Field Notes | Timothy Creekmore"
  description="Short write-ups on debugging, data extraction, and systems work."
  active="portfolio"
  bodyClass="bg-gray-100 text-gray-800"
>
  <Nav active="portfolio" />
  <!-- paste notes/index.html:15-38 here, replacing the hand-written
       list items with a {notes.map(...)} loop that emits the SAME markup -->
  <Footer />
</BaseLayout>
```

The loop must emit byte-identical markup to the hand-written list — same classes, same element nesting. Only the text content comes from the collection.

- [ ] **Step 3: Build and diff all four notes pages**

```bash
npx astro build
npm run serve:dist   # terminal 1
VISUAL_MODE=astro npx playwright test -g "note"
```

Expected: `notes`, `note-repair`, `note-ocr`, `note-biomes` all PASS on both viewports.

- [ ] **Step 4: Commit**

```bash
git add src/pages/notes src/content/notes
git commit -m "feat: port notes index and detail pages to Astro"
```

---

### Task 9: Port the project case-study pages and the portfolio grid

This is the task that pays for the whole migration: after it, adding a project is one MDX file.

**Files:**
- Modify: `src/pages/projects/[slug].astro`, `src/layouts/CaseStudyLayout.astro`, `src/pages/portfolio.astro`
- Reference: `prophetcma.html`, `signature-extraction.html`, `ai-engineering-sandbox.html`, `portfolio/index.html:193-294`
- Verify: `src/content/projects/*.mdx` (4 files exist)

**Interfaces:**
- Consumes: the `projects` collection, schema `{ title, summary, status: 'featured'|'in-progress'|'lab', tools: string[], metrics?: string[], url: string, repo?: string }`
- Produces: `/projects/<slug>/` for each MDX entry; the portfolio grid reads from `getCollection('projects')`

- [ ] **Step 1: Confirm each MDX file carries the real case-study body**

```bash
wc -l src/content/projects/*.mdx
wc -l prophetcma.html signature-extraction.html ai-engineering-sandbox.html
```

Copy the real content across for any stub. `unity-game-lab.mdx` has no static HTML counterpart and therefore no baseline — it is reviewed by eye, not by diff.

- [ ] **Step 2: Fix the `url` field in each MDX file**

Each entry's `url` currently points at the old flat path (for example `/prophetcma.html`). Change each to its new route:

| File | New `url` |
| --- | --- |
| `prophetcma.mdx` | `/projects/prophetcma/` |
| `signature-extraction.mdx` | `/projects/signature-extraction/` |
| `ai-engineering-sandbox.mdx` | `/projects/ai-engineering-sandbox/` |
| `unity-game-lab.mdx` | `/projects/unity-game-lab/` |

- [ ] **Step 3: Convert the portfolio grid to read from the collection**

Replace the hand-written cards at `portfolio/index.html:193-294` (now pasted into `src/pages/portfolio.astro`) with a loop over `getCollection('projects')`, grouped by `status`. The emitted markup must match the hand-written cards exactly — same wrapper classes, same `Featured` and `In progress` headings with their `text-teal-700` and `text-amber-700` classes.

Add to the page frontmatter:

```js
import { getCollection } from 'astro:content';
const projects = await getCollection('projects');
const featured = projects.filter((p) => p.data.status === 'featured');
const inProgress = projects.filter((p) => p.data.status === 'in-progress');
```

- [ ] **Step 4: Build and diff the project pages and the portfolio**

```bash
npx astro build
npm run serve:dist   # terminal 1
VISUAL_MODE=astro npx playwright test -g "proj-|^portfolio$"
```

Expected: `proj-prophetcma`, `proj-signature`, `proj-sandbox` PASS. `portfolio` must still PASS against the snapshot re-baselined in Task 6 — if the grid conversion changed the rendering, the loop markup does not match the hand-written cards. Fix the loop, not the snapshot.

- [ ] **Step 5: Verify the one-file claim**

```bash
cat > src/content/projects/scratch-test.mdx <<'EOF'
---
title: Scratch Test
summary: Temporary entry proving a project is one file.
status: lab
tools: ["Astro"]
url: /projects/scratch-test/
---
Body text.
EOF
npx astro build
test -f dist/projects/scratch-test/index.html && echo "ONE-FILE CLAIM HOLDS"
rm src/content/projects/scratch-test.mdx
npx astro build
```

Expected: `ONE-FILE CLAIM HOLDS`. This is the success criterion from the spec, tested directly.

- [ ] **Step 6: Commit**

```bash
git add src/pages/projects src/pages/portfolio.astro src/content/projects src/layouts/CaseStudyLayout.astro
git commit -m "feat: port case studies to content collection and drive portfolio grid from it"
```

---

### Task 10: Resume page and PDF pipeline

`resume.html` is a self-contained print document: its own `<style>`, sized in inches, no Tailwind, no nav. Wrapping it in `BaseLayout` would put the site nav bar inside the generated PDF.

**Files:**
- Create: `src/layouts/ResumeLayout.astro`
- Create: `src/pages/resume.astro`
- Modify: `scripts/build-resume.mjs`
- Reference: `resume.html` (entire file)

**Interfaces:**
- Consumes: nothing from earlier tasks — this page deliberately bypasses `BaseLayout`
- Produces: `/resume/`; `build-resume.mjs` reads `dist/resume/index.html` and writes `public/resume.pdf`

- [ ] **Step 1: Create `src/layouts/ResumeLayout.astro`**

```astro
---
interface Props {
  title: string;
  description: string;
}
const { title, description } = Astro.props;
const canonical = new URL(Astro.url.pathname, Astro.site).toString();
---
<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>{title}</title>
    <meta name="description" content={description} />
    <link rel="canonical" href={canonical} />
    <style>
      /* paste the entire <style> block from resume.html:7-134 here, verbatim */
    </style>
    <style>
      .no-print { text-align: center; padding: 1rem; }
      @media print { .no-print { display: none !important; } }
    </style>
  </head>
  <body>
    <div class="no-print">
      <a href="/portfolio/">&larr; Back to portfolio</a>
      <a href="/resume.pdf" download>Download PDF</a>
    </div>
    <slot />
  </body>
</html>
```

The `.no-print` block is the only addition to the document. Everything else is copied.

- [ ] **Step 2: Create `src/pages/resume.astro`**

```astro
---
import ResumeLayout from '../layouts/ResumeLayout.astro';
---
<ResumeLayout
  title="Timothy Creekmore Resume"
  description="Resume of Timothy Creekmore — software developer, AI/data builder."
>
  <!-- paste resume.html:136 through the closing </body> here, verbatim -->
</ResumeLayout>
```

- [ ] **Step 3: Rewrite `scripts/build-resume.mjs`**

```js
import { existsSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import { dirname, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const root = dirname(fileURLToPath(new URL('../package.json', import.meta.url)));
const source = join(root, 'dist', 'resume', 'index.html');
const output = join(root, 'public', 'resume.pdf');

if (!existsSync(source)) {
  console.error(`Missing ${source}. Run "npx astro build" first.`);
  process.exit(1);
}

const chromePaths = [
  process.env.CHROME_PATH,
  'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
  'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
  'C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe',
  'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
].filter(Boolean);

const browser = chromePaths.find(existsSync);

if (!browser) {
  console.error('Could not find Chrome or Edge. Set CHROME_PATH to override.');
  process.exit(1);
}

const result = spawnSync(browser, [
  '--headless',
  '--disable-gpu',
  '--no-pdf-header-footer',
  `--print-to-pdf=${output}`,
  pathToFileURL(source).href,
], { stdio: 'inherit' });

process.exit(result.status ?? 0);
```

- [ ] **Step 4: Regenerate the PDF and confirm the nav is absent from it**

```bash
npx astro build
npm run build:resume
test -f public/resume.pdf && echo "PDF REGENERATED"
```

Open `public/resume.pdf` and confirm: one page, no "Back to portfolio" link, no "Download PDF" button, layout matches the previous PDF.

- [ ] **Step 5: Diff the resume page against baseline**

```bash
npm run serve:dist   # terminal 1
VISUAL_MODE=astro npx playwright test -g "^resume$"
```

Expected: FAIL on the `.no-print` header only. That header is an intentional addition. Confirm the diff shows nothing else changed, then re-baseline:

```bash
VISUAL_MODE=astro npx playwright test -g "^resume$" --update-snapshots
```

- [ ] **Step 6: Commit**

```bash
git add src/layouts/ResumeLayout.astro src/pages/resume.astro scripts/build-resume.mjs public/resume.pdf tests/visual/__snapshots__
git commit -m "feat: add /resume page and rebuild PDF from the built output"
```

---

### Task 11: Repoint the link checker at the build output

`check-links.mjs` currently hardcodes a 12-file list including `now/index.html` and `uses/index.html`, both of which are being deleted. A hardcoded list is the same drift problem the migration exists to fix.

**Files:**
- Modify: `scripts/check-links.mjs`

**Interfaces:**
- Consumes: `dist/` produced by `astro build`
- Produces: exit code 0 when every local link resolves, 1 otherwise. Called by `npm run check:links` and by CI in Task 13.

- [ ] **Step 1: Rewrite `scripts/check-links.mjs`**

```js
import { readdirSync, readFileSync, existsSync, statSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = dirname(fileURLToPath(new URL('../package.json', import.meta.url)));
const dist = join(root, 'dist');

if (!existsSync(dist)) {
  console.error('No dist/ directory. Run "npx astro build" first.');
  process.exit(1);
}

function walk(dir) {
  const out = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) out.push(...walk(full));
    else if (entry.name.endsWith('.html')) out.push(full);
  }
  return out;
}

function resolves(urlPath) {
  const clean = urlPath.split('#')[0].split('?')[0];
  if (clean === '' || clean === '/') return existsSync(join(dist, 'index.html'));
  const target = join(dist, clean);
  if (existsSync(target)) {
    return statSync(target).isDirectory()
      ? existsSync(join(target, 'index.html'))
      : true;
  }
  return existsSync(join(dist, clean, 'index.html'));
}

let failures = 0;
const files = walk(dist);

for (const file of files) {
  const html = readFileSync(file, 'utf8');
  const refs = [...html.matchAll(/(?:href|src)="([^"]+)"/g)].map((m) => m[1]);
  for (const ref of refs) {
    // Only local, absolute-path references. Skip external, mailto, tel,
    // data URIs, and bare fragments.
    if (!ref.startsWith('/')) continue;
    if (ref.startsWith('//')) continue;
    if (!resolves(ref)) {
      console.error(`Broken link in ${file.replace(root, '.')}: ${ref}`);
      failures++;
    }
  }
}

console.log(`Checked ${files.length} pages.`);
if (failures > 0) {
  console.error(`${failures} broken link(s).`);
  process.exit(1);
}
console.log('All local links resolve.');
```

- [ ] **Step 2: Point the npm script at the build**

In `package.json`, change:

```json
"check:links": "node scripts/check-links.mjs",
"build": "npm run build:css && npx astro build && npm run check:links"
```

- [ ] **Step 3: Run it against a deliberately broken link**

```bash
npx astro build
node -e "const f='dist/index.html';const fs=require('fs');fs.appendFileSync(f,'<a href=\"/nope/\"></a>')"
node scripts/check-links.mjs
```

Expected: exit code 1 and a `Broken link` line naming `/nope/`. This proves the checker actually fails rather than silently passing.

- [ ] **Step 4: Rebuild and confirm a clean pass**

```bash
npx astro build
node scripts/check-links.mjs
```

Expected: `All local links resolve.` and exit code 0.

- [ ] **Step 5: Commit**

```bash
git add scripts/check-links.mjs package.json
git commit -m "fix: check links against dist/ instead of a hardcoded file list"
```

---

### Task 12: Full parity verification

Every page has been ported individually. This task proves the whole build is correct at once, before anything is deleted.

**Files:**
- No source changes. This is a gate.

**Interfaces:**
- Consumes: everything from Tasks 1–11
- Produces: a green full-suite run, which is the precondition for Task 13

- [ ] **Step 1: Clean build**

```bash
rm -rf dist
npm run build:css
npx astro build
```

- [ ] **Step 2: Confirm the page set matches the spec exactly**

```bash
find dist -name "*.html" | sort
```

Expected exactly these 12 files, and no others:

```
dist/game/index.html
dist/index.html
dist/notes/biomes-data-assets/index.html
dist/notes/index.html
dist/notes/ocr-human-review/index.html
dist/notes/repair-debugging/index.html
dist/portfolio/index.html
dist/projects/ai-engineering-sandbox/index.html
dist/projects/prophetcma/index.html
dist/projects/signature-extraction/index.html
dist/projects/unity-game-lab/index.html
dist/resume/index.html
```

Any `now/`, `uses/`, `shop/`, or `content/` output means a page that should have been dropped is still being built.

- [ ] **Step 3: Run the full visual suite**

```bash
npm run serve:dist   # terminal 1
VISUAL_MODE=astro npx playwright test
```

Expected: every test PASSES except `now` and `uses`, which SKIP. Two intentional re-baselines happened along the way (portfolio in Task 6, resume in Task 10); everything else must match the original static capture.

- [ ] **Step 4: Run the link check**

```bash
node scripts/check-links.mjs
```

Expected: `All local links resolve.`

- [ ] **Step 5: Verify the sitemap generated**

```bash
test -f dist/sitemap-index.xml && echo "SITEMAP OK"
grep -c 'timothycreekmore.com' dist/sitemap-index.xml
```

Expected: `SITEMAP OK` and a count of 1 or more.

- [ ] **Step 6: Verify the social preview renders**

Open `dist/index.html` and copy the `og:image` URL. Confirm `public/assets/images/og-card.svg` exists. Then paste `https://www.timothycreekmore.com/` into a preview validator after cutover — before cutover, confirm locally that all four tags (`og:title`, `og:description`, `og:image`, `twitter:card`) are present on the 11 pages that use `BaseLayout`:

```bash
for f in $(find dist -name "*.html"); do
  c=$(grep -c 'og:image' "$f")
  [ "$c" -eq 0 ] && echo "MISSING og:image: $f"
done
echo "og:image audit complete"
```

Expected: no `MISSING` lines. Note that `dist/resume/index.html` will be flagged — `ResumeLayout` deliberately omits OG tags. Either accept that or add them; decide and record which.

- [ ] **Step 7: Commit the verification state**

```bash
git add -A
git commit -m "test: full parity verification passing across 11 pages"
```

---

### Task 13: Deploy workflow and cutover

Everything up to here is reversible and invisible to visitors. This task makes the switch.

**Files:**
- Create: `.github/workflows/deploy.yml`
- Modify: `tailwind.config.js`, `README.md`, `.gitignore`
- Delete: all root `.html`, `portfolio/`, `game/index.html`, `notes/*.html`, `now/`, `uses/`, `shop/`, `content/`, `sitemap.xml`

**Interfaces:**
- Consumes: Task 12's green gate
- Produces: a live site served from `dist/` via GitHub Actions

- [ ] **Step 1: Commit the lockfile**

`package-lock.json` is currently untracked and `npm ci` cannot run without it.

```bash
git add package-lock.json
git commit -m "chore: track package-lock.json for CI"
```

- [ ] **Step 2: Create `.github/workflows/deploy.yml`**

```yaml
name: Deploy to Pages

on:
  push:
    branches: [main]
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: pages
  cancel-in-progress: true

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 22
          cache: npm
      - run: npm ci
      - run: npm run build:css
      - run: npx astro build
      - run: node scripts/check-links.mjs
      - uses: actions/upload-pages-artifact@v3
        with:
          path: dist

  deploy:
    needs: build
    runs-on: ubuntu-latest
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    steps:
      - id: deployment
        uses: actions/deploy-pages@v4
```

`build-resume.mjs` is deliberately **not** in CI — it needs a local Chrome install. `public/resume.pdf` is committed and ships as-is.

- [ ] **Step 3: Delete the static site**

```bash
git rm -r --cached dist 2>/dev/null || true
git rm index.html portfolio/index.html game/index.html resume.html sitemap.xml
git rm prophetcma.html signature-extraction.html ai-engineering-sandbox.html
git rm notes/index.html notes/repair-debugging.html notes/ocr-human-review.html notes/biomes-data-assets.html
git rm -r now uses shop content
```

`git rm -r --cached dist` is a no-op if `dist/` was never tracked; the `|| true` keeps the sequence going either way.

- [ ] **Step 4: Narrow the Tailwind content paths**

Replace the `content` array in `tailwind.config.js` with:

```js
content: [
  "./src/**/*.{astro,html,js,jsx,md,mdx,ts,tsx}",
  "./public/assets/js/**/*.js"
],
```

- [ ] **Step 5: Rebuild and re-verify after deletion**

Purging is the risk here: a class that only appeared in a now-deleted HTML file will vanish from the CSS.

```bash
npm run build:css
rm -rf dist
npx astro build
npm run serve:dist   # terminal 1
VISUAL_MODE=astro npx playwright test
```

Expected: full suite still PASSES. A failure here means a utility class was purged — find it in the diff and confirm it appears somewhere under `src/`.

- [ ] **Step 6: Update the README structure section**

Rewrite the `## Structure` block to match the Task 12 file list, and update `## Local development` to `npm run astro:dev`. Remove the sentence describing `src/` as "an Astro source scaffold for the next migration step" — it is now the only source.

- [ ] **Step 7: Commit and merge**

```bash
git add -A
git commit -m "feat: cut over to Astro build, delete static site"
git checkout main
git merge astro-consolidation
```

- [ ] **Step 8: Switch GitHub Pages to Actions**

In the repository settings, under Pages, change **Source** from "Deploy from a branch" to "GitHub Actions". Then:

```bash
git push origin main
```

Watch the workflow run. If it fails, the previously deployed site stays live — Pages does not tear down the old deployment on a failed build.

- [ ] **Step 9: Verify the live site**

```bash
curl -sI https://www.timothycreekmore.com/ | head -1
curl -s https://www.timothycreekmore.com/ | grep -c 'og:image'
curl -sI https://www.timothycreekmore.com/portfolio/ | head -1
curl -sI https://www.timothycreekmore.com/resume.pdf | head -1
```

Expected: `HTTP/2 200` on all three pages, and `og:image` present. If the custom domain 404s, confirm `dist/CNAME` contains `www.timothycreekmore.com`.

- [ ] **Step 10: Delete the branch**

```bash
git branch -d astro-consolidation
```

---

## After This Plan

Phase 6 from the spec — the content work — is deliberately not in this plan. Once Task 13 lands, the portfolio looks exactly as it does today, but adding a project is one file and nothing is duplicated. Phase 6 gets its own plan: real case studies structured problem / approach / outcome, a skills section that says what was built with each technology, and the devicon-to-inline-SVG decision the README already anticipates.
