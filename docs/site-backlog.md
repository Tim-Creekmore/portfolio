# Site backlog

Open items from the whole-branch review of the portfolio redesign
(2026-08-24, branch `portfolio-redesign` at `91ffff0`).

Everything under "must fix before publishing" in that review was fixed in the
follow-up wave (`91ffff0..3fb7a9c`) and independently re-verified. What remains
is recorded here so it is not lost when the scratch workspace is cleared.

**Status (2026-08-24).** Items 2 and 4 have since been resolved on the
résumé pass. The entire Housekeeping section (14–19) is done, and doing it
turned up four more items, recorded as 20–23. Items 1, 3, and 5–13 are still
open. Items marked RESOLVED or DONE are kept rather than deleted so the record
shows what was decided and why.

## Needs a decision from the site owner

These were deliberately not changed, because they are statements of fact about
the owner's history or judgement calls about his own presentation.

1. **Tenure disagrees between the résumé and the site.** The résumé reads
   "Junior Software Developer — Lifeplus, June 2025 – Present"; the home page
   rail and the source work-ledger put the developer work in 2026, about five
   and a half months. Both can be true — employment from June 2025, the
   developer role from March 2026 — but a reader sees a contradiction. If it is
   two phases of one job, saying so explicitly is stronger than either version.

2. **RESOLVED (2026-08-24).** The résumé's skills section was the pre-Lifeplus
   graduate list and omitted the stack the home page's two flagship entries
   lead with. Reorganised around software engineering in `fbc7f77`, with
   data/ML kept as a genuine secondary strength; the bullets were rewritten to
   name the actual work in `1f958e1`. TypeScript, React, Astro, Vitest,
   Playwright, Docker, CI and T-SQL now all appear.

3. **The notes pages wrap their content in a bordered panel; home and game do
   not.** That surface (renamed from `.glass-panel` to `.panel` in item 17)
   appears nowhere else on the site. Inherited from the pre-redesign notes
   pages rather than part of the new structural system, but a reading surface
   for long-form text is a defensible choice. Purely aesthetic. Still open.

4. **RESOLVED (2026-08-24).** `/coverletter.pdf` was publicly fetchable and
   linked from nowhere. The owner chose to remove it — a cover letter is
   written for one recipient. Deleted in `fbc7f77`.

## Worth doing soon

**DONE (2026-08-24)** except item 06, which needs screenshots from the owner.

- ~~05 `/game/` said the same four things twice~~ — two overlapping sections of
  seven cards in two card styles merged into one section of four, keeping every
  distinct idea. The "why this belongs in a portfolio" framing went with it.
- **06 `/game/` shows nothing. STILL OPEN — needs the owner.** The page claims
  geometry-shader grass, low-poly water and visual polish and contains zero
  images; the whole site has none. Cheapest large improvement available, and the
  page carrying the non-employer work is the one that looks least finished.
- ~~07 Three of seven pages never used the display serif~~ — the Instrument
  Serif h1 treatment was an inline style duplicated on two pages; now one rule
  covering all of them. Home and game verified pixel-identical after the move.
- ~~08 `/notes/` hardcoded rather than collection-driven~~ — now rendered from
  the collection. It had already drifted: the biomes card read "Why world
  variation belongs in data" against the note's own "World variation belongs in
  data".
- ~~09 Nine mis-levelled headings~~ — Problem/Built/Outcome were `h2` under an
  `h3`; now `h4`. Fixed with zero pixel change.
- ~~10 Notes carried `pubDate` and never rendered it~~ — replaced with an
  author-set `order`. All three shared one date, so the sort fell to
  content-layer yield and the home page reordered between builds.
- ~~11 Home entries ~32 characters per line at 375px~~ — the metadata rail took
  100px of 327px at every width; below `sm` it now stacks above the entry.
  Measured 32 to 47 characters. Desktop unchanged.
- ~~12 `og:image` was an SVG, so every share rendered blank~~ — added
  `scripts/build-og-card.mjs`, which renders a real 1200x630 PNG from the site's
  own palette, type and grid texture. The orphaned SVG is removed.
- ~~13 Stale JSON-LD~~ — `knowsAbout` was the graduate list, leading with Data
  Science and naming R while omitting TypeScript, React and testing. Now
  consistent with the resume.

## Housekeeping

**All six done (2026-08-24).** The whole section was cleared in one pass on
`worktree-housekeeping-chores`. Every page's screenshot was byte-identical
before and after except the home page, which changed for the separate reason
recorded as item 20 below. Verification: `npm run build` green (7 pages, 96
local links resolve, `check:css` clean) and the zero-tolerance visual suite
16/16, run twice across a fresh rebuild.

14. **DONE.** `public/MAEresults.png` deleted — it was the `og:image` for the
    old ProphetCMA project page, which is now a redirect stub to `/`, so
    nothing referenced it. The stale mention of it in `scripts/check-links.mjs`
    was corrected too. The other half of this item (`/coverletter.pdf`, via
    item 4) was already resolved in `fbc7f77`.
15. **DONE.** `scroll-padding-top: 5.5rem` removed. Confirmed first that
    nothing anywhere makes the nav `fixed` or `sticky` — it is a normal
    in-flow element that scrolls away, so there was nothing to clear.
16. **DONE, wider than written.** The README did not just describe the deleted
    Vanta.NET/Three.js hero. It also documented a `portfolio.astro` page,
    three `projects/*.astro` pages, a `CaseStudyLayout`, `GlassPanel` and
    `MetricCard` components, `coverletter.pdf` and `MAEresults.png` — none of
    which exist — plus a FormSubmit contact form and a whole "Form security"
    section for a form the site does not have. Rewritten against the actual
    tree, with a new "Visual regression suite" section (see item 21).
17. **DONE.** `.glass-panel` → `.panel`, `.metric-card` → `.card`, across the
    CSS and all 12 call sites. Pure rename: the declarations are unchanged and
    `game`, `notes` and the three article pages — the pages that use them —
    are pixel-identical to their existing baselines.
18. **DONE.** `<nav>` now carries `aria-label="Primary"`. There is only one
    `<nav>` on the site; the footer's links sit in a plain `<footer>`.
19. **DONE.** `scroll-behavior: smooth` is now inside
    `@media (prefers-reduced-motion: no-preference)`. This one had a real
    victim: the site's only in-page target is the skip link, so the
    unconditional smooth scroll was animating the one control that exists for
    users most likely to have asked for less motion.

## Found while doing the housekeeping

20. **The home page's Field Notes order was nondeterministic — now fixed.**
    All three notes carry the same `pubDate` (`2026-05-23`), so the
    `sort()` comparator returned 0 for every pair and the rendered order was
    whatever the content layer happened to yield. It changed between builds,
    which is why the zero-tolerance visual suite was failing on `home` before
    any of this work started — a pre-existing failure, not something this pass
    introduced. Fixed with a tiebreak (`b.id.localeCompare(a.id)`), chosen
    because it reproduces the order `/notes/` currently hardcodes, so the two
    pages now agree. `home`'s baselines were re-recorded once, deliberately.

    This is a patch over a data problem, and it connects to two open items
    above. The three identical dates are placeholders, which means item 10
    ("the notes carry `pubDate` and never render it") would currently render
    the same date three times — worth knowing before doing it. The durable fix
    is either real distinct publication dates, which is the owner's fact to
    supply, or an explicit `order` field on the notes schema, the way the
    projects collection already does it. Doing item 8 (make `/notes/`
    collection-driven) without one of those would reintroduce the same flake
    on a second page.

21. **The visual suite has no `webServer`, and nothing said so.** Running
    `npx playwright test` on a clean checkout fails all 16 tests with
    `ECONNREFUSED`, because the config expects a server already running on
    port 3000. Now documented in the README and in a comment on the config.

22. **Vanta suppression machinery outlived Vanta.** `tests/visual/pages.spec.js`
    had a `beforeEach` aborting the three.js and vanta.net CDN requests and
    overriding `window.matchMedia`, and `playwright.config.js` justified its
    zero threshold with the same story. Nothing in `src/` or `public/`
    references Vanta, three.js or `#vanta-bg` any more, and `hub-nav.js` loads
    nothing from a CDN. Removed, and the zero-tolerance rationale rewritten
    around the reason that now actually holds — with item 20 recorded in it as
    the worked example of what zero tolerance buys.

23. **Not done, deliberately: the markup still speaks the dead theme's
    dialect.** `text-white` and `bg-white`/`bg-gray-*`/`text-gray-*` appear all
    over the markup and are overridden wholesale in `tailwind.src.css` — a
    heading class-named `text-white` renders in near-black ink. Same family as
    item 17, but the CSS comment says the override block is a deliberate
    choice to avoid class churn when porting, so it is a judgement call rather
    than an oversight. Left alone; raising it as its own item instead.

## Also outstanding, outside this repo

- **RESOLVED (2026-08-24):** Publication permission for the employer-attributed
  work has been confirmed by the site owner. The two Lifeplus entries stay. The
  disclosure rules they were written under still apply to anything added later:
  no production identifiers, addresses, internal IP addresses, ticket keys,
  carrier/printer/ERP vendor names, or internal system, database, table or
  stored-procedure names; operational figures rounded hard.
- **DONE (2026-08-24):** GitHub Pages source set to "GitHub Actions"; the
  branch was merged and the site deployed. All 7 pages, all 4 legacy redirects
  and `/resume.pdf` verified live on `www.timothycreekmore.com`.

- **The apex domain has no working HTTPS.** `http://timothycreekmore.com`
  redirects correctly, but `https://timothycreekmore.com` fails to connect,
  while `www` works. Cause: the apex A record set contains
  `162.255.119.244` — a parking/forwarding address — alongside the four
  correct GitHub Pages addresses (`185.199.108-111.153`). GitHub cannot
  provision a TLS certificate for a name whose DNS points somewhere it does
  not control, so the apex certificate never completes. Modern browsers
  increasingly try HTTPS first for a typed bare domain, so someone typing
  `timothycreekmore.com` can hit a connection failure instead of the site.
  Fix at the registrar: delete the `162.255.119.244` A record for `@`, keep
  the four `185.199.*` ones, then confirm "Enforce HTTPS" in Settings → Pages.
  Requires registrar access, not repo access.

## Owner's own note (2026-08-24)

**More texture.** The site owner reviewed the finished build and approved it for
shipping, with one forward-looking wish: more texture.

Useful starting point — the texture already exists and is simply understated.
`.grid-field` (`src/styles/tailwind.src.css:46`) draws a 1px drafting grid in
`#dcd5c6` at `opacity: 0.55` over the `#f6f3ec` ground, and renders on six of
the seven pages (`index`, `game`, `notes/index`, and all three articles via
`NoteLayout`). It is deliberately absent from `/resume/`, which is a print
surface.

So this is a dial, not a rebuild: raise the opacity, tighten or vary the grid
pitch, or add a second texture layer. Any change here is caught by the
zero-tolerance visual suite, so re-baseline deliberately and confirm the diff
shows only the intended change.
