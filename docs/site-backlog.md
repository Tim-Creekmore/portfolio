# Site backlog

Open items from the whole-branch review of the portfolio redesign
(2026-08-24, branch `portfolio-redesign` at `91ffff0`).

Everything under "must fix before publishing" in that review was fixed in the
follow-up wave (`91ffff0..3fb7a9c`) and independently re-verified. What remains
is recorded here so it is not lost when the scratch workspace is cleared.

**Status (2026-08-28).** Shipped. The post-launch wave merged as PR #3
(`9855fed`) and is live. Nothing in this file is open work inside the repo:
what remains is one item finishing on GitHub's side (the apex certificate) and
two the owner parked deliberately (06, 23). Kept as the record of what was
decided and why, not as a to-do list.

**Status (2026-08-26).** Everything in this file is now either resolved,
parked by the owner's decision, or blocked on access this repo does not have.
Items 2 and 4 were resolved on the résumé pass; 5 and 7–13 on the
worth-doing-soon pass; 14–19 in the housekeeping pass, which turned up 20–23.
On 2026-08-26 the owner settled 1 (keep the résumé's framing), 3 (remove the
panel) and 23 (park it), and the texture note turned out to be a defect rather
than a preference — see "Owner's own note" at the bottom. Item 6 is parked with
the game work. Items marked RESOLVED, DONE or PARKED are kept rather than
deleted so the record shows what was decided and why.

## Needs a decision from the site owner

These were deliberately not changed, because they are statements of fact about
the owner's history or judgement calls about his own presentation.

1. **RESOLVED by decision (2026-08-26). No change.** The owner confirmed the
   facts: employed at Lifeplus from June 2025, onboarding until the current
   project started in February 2026. "June 2025 – Present" on the résumé is
   therefore accurate, and it stays.

   Re-checked what the site actually claims before deciding, because the
   original wording of this item overstated the problem. There is no duration
   statement anywhere in shipped copy — the "five and a half months" phrasing
   lived only in the review notes. The sole date signal is `period: "2026"` on
   the two Lifeplus entries, which renders in the home rail as `Lifeplus /
   2026`. A project dated after a hire date is the normal shape of a résumé and
   reads as recent work, not as a later start.

   Considered and rejected: widening the rail to `2025–26`. That would imply
   the two projects ran eighteen months, which is false, and it is exactly the
   kind of claim an interviewer probes. Vague-but-true beats precise-but-
   inflated.

2. **RESOLVED (2026-08-24).** The résumé's skills section was the pre-Lifeplus
   graduate list and omitted the stack the home page's two flagship entries
   lead with. Reorganised around software engineering in `fbc7f77`, with
   data/ML kept as a genuine secondary strength; the bullets were rewritten to
   name the actual work in `1f958e1`. TypeScript, React, Astro, Vitest,
   Playwright, Docker, CI and T-SQL now all appear.

3. **RESOLVED (2026-08-26): the panel is gone.** The owner delegated the
   call. Removed, for consistency: the redesign's whole language is rules,
   whitespace and the drafting grid, and `.panel` was the one raised surface on
   seven pages — the last structural holdover from the pre-redesign notes
   pages. Reading measure was never its job; `max-w-3xl` already does that, and
   still does.

   Removed from both call sites (`NoteLayout.astro`, `notes/index.astro`) and
   the now-unused rule deleted from the stylesheet. `.card` stays: three note
   cards sit side by side in a grid and need a boundary to read as separate
   items, which is the distinction the CSS comment already drew. The notes
   pages now read on the same paper as every other page, with the grid running
   under the text rather than stopping at a border — which matters more than it
   did, because the grid now renders at all (see the texture note below).

4. **RESOLVED (2026-08-24).** `/coverletter.pdf` was publicly fetchable and
   linked from nowhere. The owner chose to remove it — a cover letter is
   written for one recipient. Deleted in `fbc7f77`.

## Worth doing soon

**DONE (2026-08-24)**, and item 06 closed 2026-09-03 — see the game-pivot
entry at the end of this file.

- ~~05 `/game/` said the same four things twice~~ — two overlapping sections of
  seven cards in two card styles merged into one section of four, keeping every
  distinct idea. The "why this belongs in a portfolio" framing went with it.
- ~~06 `/game/` shows nothing~~ — **RESOLVED (2026-09-03)**, by replacing the
  page rather than illustrating it. Parked on 2026-08-26 pending a settled
  direction; the direction settled as a scrapped Unity project and a new Unreal
  build, so the page was rewritten. See the 2026-09-03 entry below. The
  no-screenshots problem is unchanged and now deliberate: the new plan forbids
  art until week 9, so there is nothing worth showing until roughly November.
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

23. **PARKED by the owner (2026-08-26): the markup still speaks the dead
    theme's dialect.** 48 occurrences across `src/` — dominated by
    `text-gray-600` (14), `text-gray-900` (12) and `text-gray-700` (6), plus 3
    `text-white`, `bg-white`, `bg-gray-50` and `bg-gray-100`. None of them do
    what they say: the block at the end of `tailwind.src.css` overrides every
    one with `!important` under the `body[data-site-theme="light"]` selector,
    so a heading class-named `text-white` renders in near-black ink.

    The cost is that the names lie. Anyone reading `index.astro` has to know
    the override block exists before trusting a colour class, and the override
    only fires inside that body selector — so anything added outside it would
    silently render as literal Tailwind grey. Same family as item 17, but the
    CSS comment records the override as a deliberate choice to avoid class
    churn when porting, so this is a judgement call rather than an oversight.

    Dead weight, not a bug. When it is done: swap the 48 utilities for
    semantic classes, delete the override block, and expect every page to come
    out pixel-identical — which the zero-tolerance visual suite can prove.

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
- **DONE (2026-08-28):** the post-launch review wave shipped as PR #3 (14
  commits, merged as `9855fed`). Re-verified live: all 7 pages and
  `/resume.pdf` return 200, and the drafting grid renders in a clean browser
  (`position: absolute`, `opacity: 0.8`, full viewport height). One gotcha
  worth knowing for future deploys: `/assets/css/tailwind.css` has no content
  hash and is served with `max-age=600`, so anyone who visited in the ten
  minutes before a deploy sees stale CSS until the timer expires. A hard
  refresh fixes it. Raised with the owner, who chose to leave it as-is; the
  durable fix is a hashed filename.

- **The apex domain had no working HTTPS. DNS fixed 2026-08-28; certificate
  was still provisioning when this was written.** `http://timothycreekmore.com`
  redirected correctly, but `https://timothycreekmore.com` failed to connect,
  while `www` worked. GitHub cannot provision a TLS certificate for a name
  whose DNS points somewhere it does not control, so the apex certificate
  never completed. Modern browsers try HTTPS first for a typed bare domain, so
  someone typing `timothycreekmore.com` hit a connection failure instead of the
  site — and `/resume/` prints the domain as bare text (`resume.astro:14`
  links to `www` but *displays* `timothycreekmore.com`), so that is the exact
  path a recruiter reading the PDF takes.

  Registrar is **Namecheap**, identified from the authoritative nameservers
  (`dns*.registrar-servers.com`, their BasicDNS).

  **The diagnosis above was wrong in a way worth recording.** It said to delete
  a stray `162.255.119.244` A record on `@`. No such record existed in the
  Namecheap UI — the owner checked. That address is Namecheap's redirect
  server, and it was being *synthesised* by a **URL Redirect Record** on `@`
  (apex → `http://www.timothycreekmore.com`, unmasked). Deleting A rows would
  never have worked; the redirect record regenerates the address. The error
  came from reading `nslookup` output and inferring the zone's contents rather
  than looking at the zone. Same failure mode as the `.grid-field` bug below:
  inferring state instead of observing it.

  Fixed by deleting the URL Redirect Record. Verified against the authoritative
  nameserver: the apex A set is now exactly the four `185.199.*` addresses, and
  the SOA serial moved (`1745376367` → `1787935710`), confirming a real zone
  republish rather than a cache artifact. Nothing is lost by removing the
  redirect — GitHub Pages performs apex → `www` itself, over HTTPS, which is
  the thing Namecheap's HTTP-only redirect could never do.

  Remaining: GitHub reported `cert.state: dns_changed` ("Detected a change to
  DNS settings. Requesting a new certificate") with the existing certificate
  still covering `www` only. Provisioning can take up to 24 hours. If it has
  not completed, remove and re-add the custom domain in Settings → Pages to
  force a fresh attempt. Check with:

      curl -s -o /dev/null -w "%{http_code}
" https://timothycreekmore.com/

  Anything other than `000` means the certificate landed.

## Owner's own note (2026-08-24) — RESOLVED (2026-08-26), and it was a bug

**More texture.** The site owner reviewed the finished build and approved it for
shipping, with one forward-looking wish: more texture.

The 2026-08-24 entry here said the texture "already exists and is simply
understated" and framed the work as turning a dial. That was wrong, and worth
recording as a lesson: it was written from reading the CSS rather than from
measuring the rendered page.

**The drafting grid never rendered at all.** `.grid-field` is a direct child of
`<body>`, so it was matched by the catch-all
`body[data-site-theme="light"] > *:not(.skip-link) { position: relative;
z-index: 1; }`. Both that selector and `body[data-site-theme="light"]
.grid-field` carry specificity (0,2,1), and the catch-all comes later in the
file — so it won, and overrode the grid layer's `position: absolute` with
`position: relative`. An empty div with no content and no absolute positioning
lays out at height 0. The grid painted nothing, on every page, from `ae7ce0a`
(the commit that added the catch-all as the root-cause fix for the 24px header
offset) onward. The owner was not asking for a subtler thing turned up; he was
asking for a thing that was not there.

How it hid for so long: the zero-tolerance visual suite could not catch it,
because the baselines were recorded after the regression, so "no grid" was
what the suite considered correct. It surfaced only because raising the
opacity from 0.55 to 0.8 produced **zero** pixel change on home and game — a
result that should be impossible, and was the tell. Confirmed with
`getComputedStyle` before and after: `position: relative`, `height: 0` before;
`position: absolute`, `height: 720` after.

**Fixed** by adding a second exclusion, `:not(.grid-field)`, to the catch-all —
the same shape as the `.skip-link` exclusion already there, and for the same
underlying reason: that rule must not clobber the positioning of the two
children that depend on it. Then the dial was turned, deliberately: opacity
0.55 → 0.8, and the mask's fade pushed from `transparent 78%` to `transparent
96%` so the lines survive further down the screen. Visually verified, not
just asserted.

**Known limit, left as-is.** `body` is not positioned, so the absolutely-
positioned grid layer sizes to the initial containing block — one viewport
height, anchored at the document origin — rather than the full scroll length.
The grid therefore covers the first screenful and nothing below it, which is
consistent with the mask's original fade-down-the-page intent. Making it span
the whole document means `body { position: relative }`, a change with a wider
blast radius than this pass warranted. Raise it as its own item if the owner
wants texture the whole way down.

Remaining dials, if more is still wanted: tighten or vary the 28px pitch, add a
finer second grid under the major one, or add a paper-grain layer. Any change
here is caught by the visual suite, so re-baseline deliberately and confirm the
diff shows only the intended change.


## The game pivot (2026-09-03) — `/game/` rewritten, and three pages corrected

The Unity URP voxel/medieval project is scrapped, not paused. Replacing it: a
solo sci-fi extraction shooter in Unreal Engine 5, 8 hrs/week for 17 weeks,
starting 2026-09-07 and shipping 2026-12-31. Fixed scope — one map, one weapon,
two or three bots, salvage, an extraction point, and gear that survives a lost
run.

**This was a correctness problem, not a content refresh.** `/game/` asserted
"Game lab · Unity URP", biomes as data, geometry-shader grass and "Actively in
development". Every one of those became false on the pivot, which is worse than
the defect item 06 recorded. Two other pages carried the same break: the home
page's hero and both meta descriptions said "systems work in Unity", and the
`game-lab` project card said "a third [phase] in progress" with period
`Ongoing`.

**What replaced it.** A build log, not a pitch: a testable five-point definition
of done, the five rules that constrain the work, a four-phase roadmap, and an
empty clip log that says it is empty. The old page's failure mode was
unfalsifiable claims ("engineering taste", "visual polish"); every claim on the
new one can be checked by someone who runs the build, or by waiting.

**Deliberately not computed from the build date.** The phase marker and clip
count are author-set constants. Deriving "current week" from `new Date()` would
fail the zero-tolerance visual suite on the first build after a week rolled
over, and it contradicts the discipline `src/lib/notes.ts` just established,
where `timeZone: 'UTC'` exists so the build machine cannot change what a date
says. Content that changes without a commit cannot be reviewed before it ships.
The trade is a manual edit each week, which pairs with the clip the plan already
requires.

**The project card was kept, in past tense, not deleted.** Two phases of Unity
systems work shipped and that engineering happened; scrapping the project does
not unbuild it. Period corrected `Ongoing` → `Apr–May 2026`, and the outcome now
says plainly what went wrong: no deadline and no definition of done, so it grew
and never shipped.

**Open — the résumé still claims it in present tense.** `src/pages/resume.astro`
reads "Building a Unity URP world demo with procedural terrain, biome data
assets, editor tooling, and C# placement systems." That is now false in the same
way the other three were, but how a side project is described to an employer is
the owner's call, not a correctness fix to make unilaterally. Raise it; do not
silently rewrite it.

**Open — the site still hosts no images or video anywhere.** The plan produces
17 weekly clips, and the notes collection was just rebuilt to be a running
journal fed by exactly this kind of milestone. Nothing in the repo currently
serves media, and video committed to a GitHub Pages repo is the same
bloat mistake the engine project was moved out to avoid. Decide hosting before
the first clip that is worth embedding, not after seventeen have piled up.

**The engine project now lives outside this repo,** at
`repos/extraction-shooter`, with Git LFS and Unreal ignore rules configured
before the first asset lands. The precedent for doing this early is in this
repo's own history: `3dd3a7c` had to untrack a packaged Windows build after the
fact. `game/` here is dead history and should be treated as such.
