# Portfolio Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebalance the site away from employer work, merge the portfolio into the home page, and replace the warm-dark visual identity with a light structural one.

**Architecture:** Content moves into the `projects` content collection so adding a project is one file. `/` absorbs `/portfolio/`. The `warm-dark` theme block in `tailwind.src.css` is replaced wholesale by a light system built on the same override mechanism. Pages are rebuilt against the new system in dependency order — data, then theme, then shell, then pages.

**Tech Stack:** Astro 5.8 (static), `@astrojs/mdx`, `@astrojs/sitemap`, Tailwind 3.4 (pre-built, committed), `@playwright/test` (visual regression at exact-zero tolerance), Node 24 local / Node 22 CI.

**Spec:** `docs/superpowers/specs/2026-08-23-portfolio-redesign-design.md`

## Sequencing note — read before Task 1

This plan assumes **the Astro migration is published first**. The
`astro-consolidation` branch is verified and unpublished; its parity proof is
true of that build and loses meaning as the tree moves. Publishing it first also
means that if this redesign stalls, the infrastructure improvement is already
banked.

That is the owner's call, not this plan's. If the decision is to ship both
together, nothing here changes — the tasks are identical either way.

## Global Constraints

- **The balance is 2 employer : 1 self-directed : 3 written.** Two Lifeplus
  entries, the game lab, three field notes. Adding employer entries without
  adding self-directed work defeats the spec's central decision.
- **Lifeplus copy describes reasoning, never their system.** No internal system,
  database, table, procedure or vendor names; no production identifiers,
  addresses, IP addresses or ticket keys.
- **Operational figures are rounded.** "Hundreds of thousands of records", never
  an exact count. The single exception is "29 of 58 units" — one order's line
  count, not business scale.
- **The period is five and a half months** (9 March – 22 August 2026). Never "a
  year".
- **Never claim solo authorship.** "Hand-rolled", "wrote every line myself" and
  "from scratch" are contradicted by the commit record and must not appear.
- **Entries render inline on `/` only.** No `/projects/<slug>/` routes.
- **Sequence markers (Phase A, Phase B…) appear only in the game roadmap**,
  which genuinely is a sequence. Nowhere else.
- Node 22 floor. Never commit `dist/`.
- The visual gate runs at `threshold: 0, maxDiffPixelRatio: 0`.

## The visual gate during a redesign

The gate exists to prove nothing changed. This project changes things
deliberately, so **most tasks here will fail it on purpose**.

The discipline that replaces parity, for the duration:

1. Build the page.
2. Serve `dist` and **look at it in a browser** — not at a diff.
3. Only when it looks right, re-baseline that page and commit the PNGs in the
   same commit as the markup.

Re-baselining without looking is the one thing that makes this worthless. A task
that runs `--update-snapshots` before viewing the page has skipped the only
check it had.

Once Task 8 lands, the gate resumes its normal job.

---

## File Structure

**Create:**
- `src/content/projects/lifeplus-data-audit.mdx` — Lifeplus entry 1
- `src/content/projects/lifeplus-fault-injection.mdx` — Lifeplus entry 2
- `src/content/projects/game-lab.mdx` — self-directed entry
- `public/portfolio/index.html` — redirect stub, `/portfolio/` → `/`

**Modify:**
- `src/content.config.ts` — new `projects` schema
- `src/styles/tailwind.src.css` — replace the `warm-dark` block
- `src/components/Nav.astro` — four variants collapse to one
- `src/components/Footer.astro` — three variants collapse to one
- `src/layouts/BaseLayout.astro` — `active` values reduce; `bodyClass` default
- `src/pages/index.astro` — rebuilt, absorbs the portfolio
- `src/pages/game.astro` — restyled, gains the roadmap
- `src/pages/notes/index.astro`, `src/pages/notes/[slug].astro`, `src/layouts/NoteLayout.astro`
- `src/pages/resume.astro`, `src/layouts/ResumeLayout.astro`
- `tests/visual/pages.spec.js` — PAGES table
- `public/prophetcma.html`, `public/signature-extraction.html`, `public/ai-engineering-sandbox.html` — repoint to `/`
- `README.md`

**Delete:**
- `src/pages/portfolio.astro`
- `src/pages/projects/prophetcma.astro`, `signature-extraction.astro`, `ai-engineering-sandbox.astro`
- `src/content/projects/prophetcma.mdx`, `signature-extraction.mdx`, `ai-engineering-sandbox.mdx`, `unity-game-lab.mdx`
- `src/components/GlassPanel.astro`, `src/components/MetricCard.astro`, `src/layouts/CaseStudyLayout.astro` (zero importers; the spec's Phase-6 reason for keeping them has now passed)

---

### Task 1: Content collection — schema and three entries

Data only. No page reads this yet, so the visual suite must stay **green**.

**Files:**
- Modify: `src/content.config.ts`
- Create: `src/content/projects/lifeplus-data-audit.mdx`
- Create: `src/content/projects/lifeplus-fault-injection.mdx`
- Create: `src/content/projects/game-lab.mdx`
- Delete: the four existing `src/content/projects/*.mdx`

**Interfaces:**
- Produces: a `projects` collection whose entries carry
  `{ title: string, kind: 'employed' | 'self-directed', org?: string, period: string, order: number, tech: string[], link?: { href: string, label: string } }`
  and a body containing exactly three `##` sections: `Problem`, `Built`, `Outcome`.
  Task 4 renders these.

- [ ] **Step 1: Replace the `projects` schema**

`src/content.config.ts` — replace the `projects` definition, leave `notes` untouched:

```ts
const projects = defineCollection({
  type: 'content',
  schema: z.object({
    title: z.string(),
    // 'employed' vs 'self-directed' is load-bearing: the spec's whole point is
    // that a portfolio shows what you'd build unasked, not just what you were
    // paid for. Rendering can label or group on this.
    kind: z.enum(['employed', 'self-directed']),
    org: z.string().optional(),
    period: z.string(),
    order: z.number(),
    tech: z.array(z.string()),
    link: z.object({ href: z.string(), label: z.string() }).optional()
  })
});
```

- [ ] **Step 2: Delete the four stale entries**

```bash
git rm src/content/projects/prophetcma.mdx \
       src/content/projects/signature-extraction.mdx \
       src/content/projects/ai-engineering-sandbox.mdx \
       src/content/projects/unity-game-lab.mdx
```

- [ ] **Step 3: Create `src/content/projects/lifeplus-data-audit.mdx`**

```mdx
---
title: Auditing a data-integrity bug across production history
kind: employed
org: Lifeplus
period: 2026
order: 1
tech: ["SQL", "T-SQL", "TypeScript", "Database migrations"]
---

## Problem

A per-transaction flag had been written incorrectly by an earlier bug. Nobody knew how many historical records were wrong, and a blind mass-update across a live production table was not an acceptable way to find out.

## Built

An audit that recomputes the correct value for every record and reports disagreements without writing anything, then repairs only what the fix is responsible for — with a threshold that aborts the run if the disagreement count comes back higher than expected.

## Outcome

Scanned hundreds of thousands of production records in about two seconds. Production came back clean, making the repair a verified no-op. A pre-production environment was repaired. A third tripped the abort threshold and was deliberately left alone for investigation rather than mass-written.
```

- [ ] **Step 4: Create `src/content/projects/lifeplus-fault-injection.mdx`**

```mdx
---
title: A regression suite where every test was proven capable of failing
kind: employed
org: Lifeplus
period: 2026
order: 2
tech: ["TypeScript", "Vitest", "Playwright", "Docker", "SQL", "CI"]
---

## Problem

A test that passes proves nothing on its own — a check that can never fail looks identical to one that works. On a system where a wrong result means mis-shipped goods, a green suite was being trusted without anyone establishing it would go red for the right reasons.

## Built

Coverage across unit, integration, full-API and browser-driven levels, with a standing practice: for every new test, deliberately reintroduce the bug it guards and record that it goes red, plus a control proving it does not fire on a legitimate look-alike.

## Outcome

Over a hundred test files and a full-API harness at roughly eleven thousand lines. One change's records covered fourteen injected faults, each confirmed to turn its test red. It caught real gaps: an over-broad string match passed its own test while silently hijacking an unrelated case, found only because the control used verbatim production text rather than an invented one.
```

- [ ] **Step 5: Create `src/content/projects/game-lab.mdx`**

```mdx
---
title: Game lab — Unity systems
kind: self-directed
period: 2026
order: 3
tech: ["Unity URP", "C#", "Editor tooling"]
link: { href: "/game/", label: "Roadmap and detail" }
---

## Problem

Production work is bounded by what the business needs. I wanted something with no brief attached, where the architecture decisions were mine and the only deadline was my own.

## Built

A Unity URP world demo: procedural terrain, biome data assets driving placement and presentation through a registry, editor tooling for authoring them, plus combat, squads, save/load and scene setup.

## Outcome

Two phases shipped across four months, a third in progress. The biome system replaced hardcoded placement logic with data assets, which is the same instinct as the work above — put the rules somewhere you can inspect them.
```

- [ ] **Step 6: Verify the collection type-checks and the suite is still green**

```bash
npx astro build
```
Expected: exit 0. No page reads `projects` yet, so nothing renders differently.

```bash
node scripts/static-server.mjs dist 3000 &
VISUAL_MODE=astro npx playwright test
```
Expected: **24 passed, 4 skipped, 0 failed** — unchanged. If anything fails, this task touched rendering and should not have.

Check port 3000 is free first and kill the server after.

- [ ] **Step 7: Commit**

```bash
git add src/content.config.ts src/content/projects
git commit -m "feat: replace projects collection with the rebalanced three entries"
```

---

### Task 2: Visual system — replace the warm-dark theme

The biggest single change. Every page restyles at once.

**Files:**
- Modify: `src/styles/tailwind.src.css`

**Interfaces:**
- Consumes: nothing
- Produces: `body[data-site-theme="light"]` carrying the token set below, plus
  the utility classes `.rule`, `.rail`, `.tick`, `.eyebrow`, `.grid-field`
  used by Tasks 4–7.

- [ ] **Step 1: Replace the theme block**

Delete everything in `src/styles/tailwind.src.css` from the comment
`/* Warm futuristic site skin */` to the end of the file, and replace with the
light system. Keep the file's opening `@tailwind` directives, the
`html { scroll-behavior }` rule, `.hub-link-active`, the reduced-motion block,
and `.skip-link` exactly as they are.

```css
/* Light structural skin — see docs/superpowers/specs/2026-08-23-portfolio-redesign-design.md */
body[data-site-theme="light"] {
  --ground: #f6f3ec;
  --surface: #fffdf8;
  --ink: #17171a;
  --ink-2: #4b4740;
  --ink-3: #8b8578;
  --rule: #e0d9c9;
  --accent: #23405e;

  background: var(--ground);
  color: var(--ink);
}

/* The drafting grid. Fades down the page so it never competes with content. */
body[data-site-theme="light"] .grid-field {
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  opacity: 0.55;
  background-image:
    linear-gradient(#dcd5c6 1px, transparent 1px),
    linear-gradient(90deg, #dcd5c6 1px, transparent 1px);
  background-size: 28px 28px;
  -webkit-mask-image: linear-gradient(180deg, #000 0%, rgba(0,0,0,.35) 42%, transparent 78%);
  mask-image: linear-gradient(180deg, #000 0%, rgba(0,0,0,.35) 42%, transparent 78%);
}
body[data-site-theme="light"] > * { position: relative; z-index: 1; }

body[data-site-theme="light"] .eyebrow {
  font-family: "IBM Plex Mono", ui-monospace, monospace;
  font-size: 0.65rem;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  color: var(--accent);
  font-weight: 500;
}

body[data-site-theme="light"] .rule { border-top: 1px solid var(--rule); }

body[data-site-theme="light"] .rail {
  font-family: "IBM Plex Mono", ui-monospace, monospace;
  font-size: 0.6rem;
  line-height: 1.75;
  color: var(--ink-3);
  border-right: 1px solid var(--rule);
}

body[data-site-theme="light"] .tick {
  font-family: "IBM Plex Mono", ui-monospace, monospace;
  font-size: 0.58rem;
  letter-spacing: 0.08em;
  color: var(--ink-3);
}

/* Override the Tailwind utilities the markup uses, the same mechanism the old
   theme used — so ported markup keeps working without class churn. */
body[data-site-theme="light"] .bg-white,
body[data-site-theme="light"] .bg-gray-50,
body[data-site-theme="light"] .bg-gray-100 { background-color: var(--surface) !important; }

body[data-site-theme="light"] .text-gray-900,
body[data-site-theme="light"] .text-white { color: var(--ink) !important; }

body[data-site-theme="light"] .text-gray-700,
body[data-site-theme="light"] .text-gray-600 { color: var(--ink-2) !important; }

body[data-site-theme="light"] .text-gray-500,
body[data-site-theme="light"] .text-gray-400 { color: var(--ink-3) !important; }

body[data-site-theme="light"] .border-gray-100,
body[data-site-theme="light"] .border-gray-200 { border-color: var(--rule) !important; }

body[data-site-theme="light"] .text-teal-600,
body[data-site-theme="light"] .text-teal-700,
body[data-site-theme="light"] .hover\:text-teal-600:hover { color: var(--accent) !important; }
```

- [ ] **Step 2: Load the typefaces**

In `src/components/Seo.astro`, add before the existing stylesheet link:

```astro
<link rel="preconnect" href="https://fonts.googleapis.com" />
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;500&family=IBM+Plex+Sans:wght@400;500;600&family=Instrument+Serif&display=swap" />
```

Remove the devicon stylesheet link from `BaseLayout.astro` in the same step — it
exists for the old skills section, which this redesign removes, and it is a
render-blocking request on every page.

- [ ] **Step 3: Switch the body attribute**

In `src/layouts/BaseLayout.astro`, change `data-site-theme="warm-dark"` to
`data-site-theme="light"`. Search the whole repo for any other occurrence:

```bash
grep -rn "warm-dark" src/ public/ tests/
```
Expected after the change: no hits outside comments.

- [ ] **Step 4: Rebuild CSS and look at the site**

```bash
npm run build:css
npx astro build
node scripts/static-server.mjs dist 3000 &
```

Open `http://localhost:3000/` **in a browser**. The pages still carry old
markup, so expect them to look plain — that is correct at this stage. What you
are checking is that the palette reads as intended and nothing is unreadable
(dark text on dark, invisible borders). Report anything that looks broken rather
than adjusting tokens on instinct.

- [ ] **Step 5: Re-baseline everything, then verify stability**

```bash
VISUAL_MODE=astro npx playwright test --update-snapshots
VISUAL_MODE=astro npx playwright test
```
Expected: the second run passes 24/4/0. If it does not, captures are not
deterministic and that must be fixed before continuing — every later task
depends on this gate.

- [ ] **Step 6: Commit**

```bash
git add src/styles/tailwind.src.css public/assets/css/tailwind.css \
        src/layouts/BaseLayout.astro src/components/Seo.astro \
        tests/visual/__snapshots__
git commit -m "feat: replace warm-dark theme with the light structural system"
```

---

### Task 3: Nav, Footer and BaseLayout simplification

**Files:**
- Modify: `src/components/Nav.astro`, `src/components/Footer.astro`, `src/layouts/BaseLayout.astro`

**Interfaces:**
- Consumes: the tokens from Task 2
- Produces: `<Nav active={'work'|'notes'|'game'|'resume'} />` and `<Footer />`
  with no props. `BaseLayout`'s `active` prop accepts the same four values.

- [ ] **Step 1: Collapse Nav to one variant**

Replace `src/components/Nav.astro` entirely:

```astro
---
interface Props {
  active: 'work' | 'notes' | 'game' | 'resume';
}
const { active } = Astro.props;

const SECTIONS = ['work', 'notes', 'game', 'resume'];
if (!SECTIONS.includes(active)) {
  throw new Error(
    `Nav.astro: invalid active ${JSON.stringify(active)}. ` +
    `Expected one of ${SECTIONS.join(', ')}. ` +
    `A page rendering Nav without a valid value would ship with no navigation.`
  );
}

const LINKS = [
  { href: '/',        key: 'work',   label: 'Work' },
  { href: '/notes/',  key: 'notes',  label: 'Notes' },
  { href: '/game/',   key: 'game',   label: 'Game' },
  { href: '/resume/', key: 'resume', label: 'Résumé' },
];
---
<nav id="hub-navbar" class="border-b border-gray-200">
  <div class="max-w-3xl mx-auto px-6 h-14 flex items-center justify-between gap-4">
    <a href="/" class="text-sm font-medium text-gray-900 hover:opacity-70 transition">Timothy Creekmore</a>
    <div class="flex items-center gap-5 text-xs">
      {LINKS.map((l) => (
        <a
          href={l.href}
          class="text-gray-600 hover:text-teal-600 transition"
          aria-current={l.key === active ? 'page' : undefined}
        >{l.label}</a>
      ))}
    </div>
  </div>
</nav>
```

The guard stays. It exists because the component previously rendered empty when
misconfigured — a page with no navigation and a green build. That failure mode
is independent of variant count.

Note the nav is no longer `fixed`. Pages must drop the `pt-28` clearance they
carried for it; Tasks 4–7 handle that per page.

- [ ] **Step 2: Collapse Footer to one variant**

Replace `src/components/Footer.astro`:

```astro
---
---
<footer class="rule mt-20">
  <div class="max-w-3xl mx-auto px-6 py-10 flex flex-wrap gap-x-5 gap-y-2 justify-between text-xs text-gray-500">
    <span>&copy; <span id="footer-year">2026</span> Timothy Creekmore</span>
    <span class="flex gap-5">
      <a href="/notes/" class="hover:text-teal-600 transition">Notes</a>
      <a href="https://github.com/Tim-Creekmore" target="_blank" rel="noopener noreferrer" class="hover:text-teal-600 transition">GitHub</a>
      <a href="https://www.linkedin.com/in/timcreekmore" target="_blank" rel="noopener noreferrer" class="hover:text-teal-600 transition">LinkedIn</a>
    </span>
  </div>
</footer>
```

`#footer-year` must survive — `public/assets/js/hub-nav.js` writes the year into it.

- [ ] **Step 3: Update BaseLayout's prop values**

In `src/layouts/BaseLayout.astro`, change the `active` union and its guard list
from `'home' | 'portfolio' | 'game'` to `'work' | 'notes' | 'game' | 'resume'`.
Change the `bodyClass` default to `'bg-gray-100 text-gray-800'` → `''` (the
theme now sets ground and ink on `body` directly).

- [ ] **Step 4: Build — expect failures, and read them**

```bash
npx astro build
```
Expected: **fails**, because existing pages still pass `active="home"` etc. That
is the guard working. Note which pages fail; Tasks 4–7 fix them.

To get a build for the visual check, temporarily pass valid values in the four
page files, then revert before committing.

- [ ] **Step 5: Commit**

```bash
git add src/components/Nav.astro src/components/Footer.astro src/layouts/BaseLayout.astro
git commit -m "refactor: collapse Nav and Footer to a single variant"
```

Commit with the build red. The next task resolves it. Say so in the commit body.

---

### Task 4: Rebuild the home page

The centrepiece. `/` absorbs `/portfolio/`.

**Files:**
- Modify: `src/pages/index.astro`
- Delete: `src/pages/portfolio.astro`
- Create: `public/portfolio/index.html`

**Interfaces:**
- Consumes: the `projects` collection (Task 1), the theme classes (Task 2),
  `Nav`/`Footer` (Task 3)
- Produces: `/` carrying hero, selected work, field notes, about, contact

- [ ] **Step 1: Rebuild `src/pages/index.astro`**

```astro
---
import BaseLayout from '../layouts/BaseLayout.astro';
import Nav from '../components/Nav.astro';
import Footer from '../components/Footer.astro';
import { getCollection, render } from 'astro:content';

const projects = (await getCollection('projects')).sort((a, b) => a.data.order - b.data.order);
const rendered = await Promise.all(projects.map(async (p) => ({ data: p.data, Content: (await render(p)).Content })));

const notes = (await getCollection('notes')).sort((a, b) => b.data.pubDate.valueOf() - a.data.pubDate.valueOf());
---
<BaseLayout
  title="Timothy Creekmore — Software Developer"
  description="Software developer at Lifeplus. Production systems, Unity systems work, and short write-ups on what actually broke."
  ogTitle="Timothy Creekmore — Software Developer"
  ogDescription="Production systems, Unity systems work, and short write-ups on what actually broke."
  ogType="profile"
  active="work"
>
  <div class="grid-field" aria-hidden="true"></div>
  <Nav active="work" />

  <main id="main-content" class="max-w-3xl mx-auto px-6">

    <header class="pt-16 pb-14">
      <p class="eyebrow mb-4">Software developer · Lifeplus</p>
      <h1 class="text-4xl md:text-5xl mb-5" style="font-family:'Instrument Serif',Georgia,serif;line-height:1.03;letter-spacing:-0.015em;">
        I build the boring parts properly.
      </h1>
      <p class="text-gray-600 leading-relaxed max-w-xl">
        Production software at Lifeplus, systems work in Unity, and short write-ups on what actually broke.
      </p>
    </header>

    <section aria-labelledby="work-heading" class="rule pt-8">
      <h2 id="work-heading" class="eyebrow mb-8">Selected work</h2>
      {rendered.map((p) => (
        <article class="grid grid-cols-[5rem_1fr] gap-5 pb-10 mb-10 rule-b">
          <div class="rail pr-3">
            {p.data.org ?? 'Personal'}<br />{p.data.period}
          </div>
          <div>
            <h3 class="text-lg font-semibold text-gray-900 mb-3">{p.data.title}</h3>
            <div class="prose-entry text-sm text-gray-700 leading-relaxed"><p.Content /></div>
            <p class="tick mt-4">{p.data.tech.join(' · ')}</p>
            {p.data.link && <p class="mt-2 text-sm"><a href={p.data.link.href} class="text-teal-600 hover:underline">{p.data.link.label} &rarr;</a></p>}
          </div>
        </article>
      ))}
    </section>

    <section aria-labelledby="notes-heading" class="rule pt-8">
      <h2 id="notes-heading" class="eyebrow mb-8">Field notes</h2>
      <ul class="space-y-5 list-none p-0">
        {notes.map((n) => (
          <li>
            <a href={`/notes/${n.id.replace(/\.mdx?$/, '')}/`} class="block group">
              <span class="text-base font-semibold text-gray-900 group-hover:text-teal-600 transition">{n.data.title}</span>
              <span class="block text-sm text-gray-600 mt-1">{n.data.summary}</span>
            </a>
          </li>
        ))}
      </ul>
    </section>

    <section aria-labelledby="about-heading" class="rule pt-8">
      <h2 id="about-heading" class="eyebrow mb-6">About</h2>
      <div class="text-gray-700 leading-relaxed space-y-4 max-w-xl">
        <p>I write production software at Lifeplus — features, internal tooling and workflow support on a real team, shipping into a system where a wrong result means goods go to the wrong place.</p>
        <p>Before that I studied Computer Science and Data Science at Lyon College, and spent seven years repairing appliances. That second one turns out to matter more than it sounds: diagnosis under uncertainty, and the discipline not to trust your first theory.</p>
      </div>
    </section>

    <section aria-labelledby="contact-heading" class="rule pt-8 pb-4">
      <h2 id="contact-heading" class="eyebrow mb-6">Contact</h2>
      <p class="text-gray-700 mb-4 max-w-xl">Open to talking about software, data work, or systems problems.</p>
      <p class="text-sm">
        <a id="contact-email" href="#contact" data-user="timcreekmore2002" data-domain="gmail.com" class="text-teal-600 hover:underline">click to reveal email</a>
        <span class="text-gray-400 mx-2">·</span>
        <a href="/resume.pdf" class="text-teal-600 hover:underline">Résumé (PDF)</a>
      </p>
    </section>

  </main>
  <Footer />

  <script slot="scripts" is:inline>
    (function () {
      var el = document.getElementById('contact-email');
      if (!el) return;
      var reveal = function () {
        var addr = el.dataset.user + '@' + el.dataset.domain;
        el.href = 'mailto:' + addr;
        el.textContent = addr;
        el.removeEventListener('mouseenter', reveal);
        el.removeEventListener('focus', reveal);
      };
      el.addEventListener('mouseenter', reveal);
      el.addEventListener('focus', reveal);
      el.addEventListener('click', function (e) {
        if (el.getAttribute('href') === '#contact') { e.preventDefault(); reveal(); }
      });
    })();
  </script>
</BaseLayout>
```

`is:inline` is mandatory on that script — without it Astro bundles it and the
reveal handlers bind after the element is interactive.

- [ ] **Step 2: Add the `.rule-b` and `.prose-entry` rules**

In `src/styles/tailwind.src.css`, inside the light block:

```css
body[data-site-theme="light"] .rule-b { border-bottom: 1px solid var(--rule); }
body[data-site-theme="light"] .rule-b:last-child { border-bottom: 0; }
body[data-site-theme="light"] .prose-entry h2 {
  font-family: "IBM Plex Mono", ui-monospace, monospace;
  font-size: 0.6rem;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--ink-3);
  margin: 1rem 0 0.3rem;
}
body[data-site-theme="light"] .prose-entry h2:first-child { margin-top: 0; }
body[data-site-theme="light"] .prose-entry p { margin: 0 0 0.5rem; }
```

The MDX bodies use `## Problem` / `## Built` / `## Outcome`; this styles them as
labels rather than headings. Astro's scoped styles do not reach MDX content, so
these must live in the global stylesheet — not in a `<style>` block on the page.

- [ ] **Step 3: Delete the portfolio page and add its redirect**

```bash
git rm src/pages/portfolio.astro
mkdir -p public/portfolio
```

`public/portfolio/index.html`:

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta http-equiv="refresh" content="0; url=/" />
  <link rel="canonical" href="https://www.timothycreekmore.com/" />
  <title>Redirecting…</title>
</head>
<body style="font-family:system-ui,sans-serif;padding:2rem">
  <p>This page moved. Redirecting to <a href="/">timothycreekmore.com</a>…</p>
</body>
</html>
```

- [ ] **Step 4: Build, then look at it**

```bash
npm run build:css && npx astro build
node scripts/static-server.mjs dist 3000 &
```

Open `http://localhost:3000/` in a browser. Check: the grid fades correctly, the
three entries render with their Problem/Built/Outcome labels, the rail shows
org and period, the notes list is populated, and the email reveal works on hover
and on click.

Then check `http://localhost:3000/portfolio/` redirects to `/`.

- [ ] **Step 5: Re-baseline home, verify the copy constraints**

```bash
VISUAL_MODE=astro npx playwright test -g "home$" --update-snapshots
```

Then check the built page against the global constraints:

```bash
grep -icE "hand-rolled|from scratch|wrote every line" dist/index.html   # expect 0
grep -oE "[0-9]{3},[0-9]{3}" dist/index.html                            # expect no matches
grep -c "a year" dist/index.html                                        # expect 0
```

- [ ] **Step 6: Commit**

```bash
git add src/pages/index.astro src/styles/tailwind.src.css public/assets/css/tailwind.css \
        public/portfolio tests/visual/__snapshots__
git rm --cached src/pages/portfolio.astro 2>/dev/null || true
git commit -m "feat: rebuild home page, absorbing the portfolio"
```

---

### Task 5: Delete the project pages and repoint redirects

**Files:**
- Delete: `src/pages/projects/prophetcma.astro`, `signature-extraction.astro`, `ai-engineering-sandbox.astro`
- Delete: `src/components/GlassPanel.astro`, `src/components/MetricCard.astro`, `src/layouts/CaseStudyLayout.astro`
- Modify: `public/prophetcma.html`, `public/signature-extraction.html`, `public/ai-engineering-sandbox.html`

- [ ] **Step 1: Confirm the components really have no importers**

```bash
grep -rn "GlassPanel\|MetricCard\|CaseStudyLayout" src/ tests/ scripts/
```
Expected: no hits. If any appear, stop and report — do not delete something in use.

- [ ] **Step 2: Delete**

```bash
git rm src/pages/projects/prophetcma.astro \
       src/pages/projects/signature-extraction.astro \
       src/pages/projects/ai-engineering-sandbox.astro \
       src/components/GlassPanel.astro \
       src/components/MetricCard.astro \
       src/layouts/CaseStudyLayout.astro
rmdir src/pages/projects 2>/dev/null || true
```

- [ ] **Step 3: Repoint the three redirect stubs**

In each of `public/prophetcma.html`, `public/signature-extraction.html` and
`public/ai-engineering-sandbox.html`, change the refresh target and the link
from `/projects/<slug>/` to `/`, and update the visible text to say the page
moved. Keep the files — those URLs have been live.

- [ ] **Step 4: Build and check every old URL still resolves**

```bash
npx astro build
node scripts/static-server.mjs dist 3000 &
for u in /prophetcma.html /signature-extraction.html /ai-engineering-sandbox.html /portfolio/; do
  printf "%-34s %s\n" "$u" "$(curl -s -o /dev/null -w '%{http_code}' http://localhost:3000$u)"
done
```
Expected: `200` for all four.

```bash
node scripts/check-links.mjs
```
Expected: exit 0.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: delete project pages and dead components, repoint old URLs to /"
```

---

### Task 6: Game page and the dated roadmap

**Files:**
- Modify: `src/pages/game.astro`

- [ ] **Step 1: Restyle the page and add the roadmap**

Rewrite `src/pages/game.astro` on the new system: `<Nav active="game" />`, the
`grid-field` div, `max-w-3xl mx-auto px-6` main, and the existing game copy
reflowed to match the home page's rhythm. Then add the roadmap section:

```astro
<section aria-labelledby="roadmap-heading" class="rule pt-8">
  <div class="flex items-baseline justify-between mb-8">
    <h2 id="roadmap-heading" class="eyebrow">Roadmap</h2>
    <p class="tick">Updated Aug 2026</p>
  </div>

  <ol class="list-none p-0 m-0">
    <li class="grid grid-cols-[1.5rem_1fr] gap-4 pb-6">
      <span class="phase-dot done" aria-hidden="true"></span>
      <div>
        <p class="tick float-right">Apr 2026</p>
        <p class="font-semibold text-gray-900 text-sm mb-1">Phase A · Core systems</p>
        <p class="text-sm text-gray-600">Camera modes, death system, combat, squads, arena.</p>
      </div>
    </li>
    <li class="grid grid-cols-[1.5rem_1fr] gap-4 pb-6">
      <span class="phase-dot done" aria-hidden="true"></span>
      <div>
        <p class="tick float-right">Jun 2026</p>
        <p class="font-semibold text-gray-900 text-sm mb-1">Phase A5 · Polish and persistence</p>
        <p class="text-sm text-gray-600">Ambient pass, save/load, enemy state, playtest fixes.</p>
      </div>
    </li>
    <li class="grid grid-cols-[1.5rem_1fr] gap-4 pb-6">
      <span class="phase-dot wip" aria-hidden="true"></span>
      <div>
        <p class="tick float-right">Building</p>
        <p class="font-semibold text-gray-900 text-sm mb-1">Phase B · Biome system</p>
        <p class="text-sm text-gray-600">Data assets and registry, editor builder, placement and boundaries.</p>
      </div>
    </li>
    <li class="grid grid-cols-[1.5rem_1fr] gap-4">
      <span class="phase-dot todo" aria-hidden="true"></span>
      <div>
        <p class="tick float-right">Next</p>
        <p class="font-semibold text-gray-500 text-sm mb-1">Phase C · Browser-playable build</p>
        <p class="text-sm text-gray-500">Turn the splash into a demo people can play in a tab.</p>
      </div>
    </li>
  </ol>
</section>
```

- [ ] **Step 2: Add the dot styles**

In the light block of `src/styles/tailwind.src.css`:

```css
body[data-site-theme="light"] .phase-dot {
  width: 15px; height: 15px; border-radius: 50%; margin-top: 3px; display: block;
}
body[data-site-theme="light"] .phase-dot.done { background: var(--accent); border: 1px solid var(--accent); }
body[data-site-theme="light"] .phase-dot.wip  { background: var(--ground); border: 2px solid var(--accent); }
body[data-site-theme="light"] .phase-dot.todo { background: var(--ground); border: 1px dashed var(--ink-3); }
```

State is encoded in shape as well as in the text label, so the roadmap reads at
a glance and still works for anyone who cannot distinguish the fills.

- [ ] **Step 3: Build, look, re-baseline**

```bash
npm run build:css && npx astro build
node scripts/static-server.mjs dist 3000 &
```
Open `http://localhost:3000/game/` and check the roadmap reads correctly and the
play-build link still resolves. Then:

```bash
VISUAL_MODE=astro npx playwright test -g "game$" --update-snapshots
```

- [ ] **Step 4: Commit**

```bash
git add src/pages/game.astro src/styles/tailwind.src.css public/assets/css/tailwind.css tests/visual/__snapshots__
git commit -m "feat: restyle game page and add the dated roadmap"
```

---

### Task 7: Notes and résumé under the new system

**Files:**
- Modify: `src/pages/notes/index.astro`, `src/layouts/NoteLayout.astro`, `src/pages/resume.astro`, `src/layouts/ResumeLayout.astro`

- [ ] **Step 1: Notes index and note layout**

Four concrete changes to each of `src/pages/notes/index.astro` and
`src/layouts/NoteLayout.astro`:

1. `<Nav active="portfolio" />` → `<Nav active="notes" />`, and the same value
   on `BaseLayout`'s `active` prop.
2. Add `<div class="grid-field" aria-hidden="true"></div>` as the first child
   inside `BaseLayout`, matching the home page.
3. Container `max-w-5xl` → `max-w-3xl mx-auto px-6`, so notes share the home
   page's measure.
4. `class="px-6 pt-28 pb-16"` → `class="pt-12 pb-16"` on `<main>`. The `pt-28`
   existed as clearance for a `fixed` nav; Task 3's nav is in normal flow, so
   that padding is now a 112-pixel gap with nothing in it.

`NoteLayout` keeps `id="main-content"` on its `<main>` and keeps its
"← Back to notes" link. Its `.note-prose :global(p + p)` spacing rule and the
skip-link override stay — both were added for reasons unrelated to this
redesign.

The note detail pages have no footer in the source. Add `<Footer />` to
`NoteLayout` now that there is a single footer variant — a reader landing on an
article from search currently has only the back-link.

- [ ] **Step 2: Résumé**

`ResumeLayout` deliberately does **not** use `BaseLayout` — it is a standalone
print document with its own inline stylesheet, and wrapping it would put the
site nav into the generated PDF. Leave that structure alone.

Change only the `.no-print` bar to match the new palette:
`background: #f6f3ec`, links in `#23405e`. The `@page` rules and the resume's
own `@media print` block are untouched.

- [ ] **Step 3: Build, look at all five pages, re-baseline**

```bash
npm run build:css && npx astro build
node scripts/static-server.mjs dist 3000 &
```
Open `/notes/`, one note, and `/resume/`. Then:

```bash
VISUAL_MODE=astro npx playwright test -g "notes$|note-|resume$" --update-snapshots
```

- [ ] **Step 4: Regenerate the PDF and open it**

```bash
npm run build:resume
```
Open `public/resume.pdf`. Confirm one page, no navigation bar, and that the
`.no-print` colour change did not leak into print.

- [ ] **Step 5: Commit**

```bash
git add src/pages/notes src/layouts/NoteLayout.astro src/pages/resume.astro \
        src/layouts/ResumeLayout.astro public/resume.pdf tests/visual/__snapshots__
git commit -m "feat: restyle notes and resume under the light system"
```

---

### Task 8: Update the visual test table

**Files:**
- Modify: `tests/visual/pages.spec.js`

- [ ] **Step 1: Rewrite the PAGES table**

Remove `proj-prophetcma`, `proj-signature`, `proj-sandbox`, `now` and `uses`.
Change `portfolio` to point at the redirect and expect the home page's title.
Update `home`'s `expectTitle` to match the new `<title>`.

```js
const PAGES = [
  { name: 'home',        astro: '/',                          expectTitle: 'Timothy Creekmore' },
  { name: 'game',        astro: '/game/',                     expectTitle: 'Voxel game' },
  { name: 'notes',       astro: '/notes/',                    expectTitle: 'Field Notes' },
  { name: 'note-repair', astro: '/notes/repair-debugging/',   expectTitle: 'Appliance Repair Taught Me About Debugging' },
  { name: 'note-ocr',    astro: '/notes/ocr-human-review/',   expectTitle: 'OCR Projects Need Human Review' },
  { name: 'note-biomes', astro: '/notes/biomes-data-assets/', expectTitle: 'Designing Biomes as Data Assets' },
  { name: 'resume',      astro: '/resume/',                   expectTitle: 'Timothy Creekmore Resume' },
];
```

Drop the `static:` column and the `MODE` switch entirely — the static site was
deleted at cutover and that mode has not been runnable since. Replace the
cutover note with a short comment saying the baselines now represent the
redesigned site as of this commit.

- [ ] **Step 2: Delete the orphaned baselines**

```bash
cd tests/visual/__snapshots__/pages.spec.js-snapshots
git rm proj-*.png now-*.png uses-*.png portfolio-*.png
cd -
```

- [ ] **Step 3: Verify the selectors still work**

```bash
npx playwright test --list
```
Confirm 7 page tests × 2 projects, plus the static-server spec. Check that
`-g "home$"` selects exactly 2.

- [ ] **Step 4: Full suite**

```bash
node scripts/static-server.mjs dist 3000 &
VISUAL_MODE=astro npx playwright test
```
Expected: **16 passed, 0 failed, 0 skipped** (7 pages × 2 + 2 static-server).

- [ ] **Step 5: Commit**

```bash
git add tests/visual
git commit -m "test: retarget the visual suite at the redesigned page set"
```

---

### Task 9: Full verification

A gate, not a work step. **Fix nothing here** — report anything that fails.

- [ ] **Step 1: Clean build**

```bash
rm -rf dist && npm run build
```
Expected: exit 0. Read the exit code directly — do not pipe through `tail`.

- [ ] **Step 2: Page set**

```bash
find dist -name "*.html" | sort
```

Expected exactly these **12**, and nothing else:

```
dist/ai-engineering-sandbox.html      <- redirect stub
dist/game/index.html
dist/game/play/index.html             <- copied from public/, pre-existing
dist/index.html
dist/notes/biomes-data-assets/index.html
dist/notes/index.html
dist/notes/ocr-human-review/index.html
dist/notes/repair-debugging/index.html
dist/portfolio/index.html             <- redirect stub
dist/prophetcma.html                  <- redirect stub
dist/resume/index.html
dist/signature-extraction.html        <- redirect stub
```

That is 7 real pages, 4 redirect stubs, and the pre-existing game-play stub. Any
`projects/` output is a defect.

Note the spec's success criteria says "six pages plus three redirect stubs" —
written before `/portfolio/` gained its own redirect. Seven and four is correct;
the spec undercounts.

- [ ] **Step 3: Suite, three times**

```bash
VISUAL_MODE=astro npx playwright test
```
Three consecutive runs, identical results, 16 passed. At zero tolerance any
instability is a real problem.

- [ ] **Step 4: Constraint audit across the built site**

```bash
grep -rilE "hand-rolled|from scratch|wrote every line" dist/ || echo "no authorship claims — OK"
grep -rhoE "[0-9]{3},[0-9]{3}" dist/*.html || echo "no unrounded figures — OK"
grep -ril "warm-dark" dist/ || echo "old theme fully removed — OK"
```

Then read `dist/index.html` and confirm by eye that no internal system, table,
procedure or vendor name appears in the two Lifeplus entries.

- [ ] **Step 5: Old URLs**

```bash
for u in /portfolio/ /prophetcma.html /signature-extraction.html /ai-engineering-sandbox.html; do
  printf "%-34s %s\n" "$u" "$(curl -s -o /dev/null -w '%{http_code}' http://localhost:3000$u)"
done
```
Expected: `200` for all four.

- [ ] **Step 6: Record it**

Write `docs/verification-2026-08-23-redesign.md` — a tracked file, following the
pattern of `docs/verification-2026-08-22.md` — with the commit verified and
verbatim output for every step above. Commit it.

---

## After this plan

The colophon page — "how this site is built", covering the visual gate, the link
checker and the CSS drift guard — is deliberately out of scope and gets its own
spec. It is strong material and entirely owned, which makes it the natural next
piece once this lands.
