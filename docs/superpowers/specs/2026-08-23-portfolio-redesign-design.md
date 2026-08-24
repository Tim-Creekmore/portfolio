# Design: Portfolio redesign — content, structure, and visual system

Date: 2026-08-23
Status: Awaiting review
Builds on: `astro-consolidation` branch (the Astro migration, unpublished; `main` at `e26eec2`)

## Problem

The Astro migration preserved the site exactly. That was its point — but it
means the site still says what it said before, and the owner's position has
changed underneath it.

Three things are now wrong with the content:

1. **The featured work is stale.** ProphetCMA, Signature Extraction and the AI
   Sandbox are Lyon College-era projects. They anchor a reader to who the owner
   was as a student rather than who he is as a working developer.
2. **The current work is invisible.** Five and a half months of production
   software at Lifeplus appears nowhere, because it is employer work and the
   site had no way to represent it.
3. **The visual identity does not fit.** The warm-dark amber-and-teal system
   with an animated network background reads as a particular flavour of
   developer-portfolio the owner does not identify with.

There is also a structural redundancy: `/` is a splash page whose job is
pointing at `/portfolio/`, which carries the substance. With the case studies
removed, the split has nothing left to justify it.

## Goals

- The site reads as a person who builds things, not an employee listing duties.
- Current capability is visible without disclosing an employer's systems.
- A visual identity the owner would choose rather than tolerate.
- Fewer pages, each of which earns its place.

## Non-goals

- No new infrastructure. The Astro build, the visual gate, the link checker and
  the CSS guard all stay exactly as they are.
- No rewrite of the field notes' prose.
- No new projects built for the purpose. This works with what exists.

## The decision that shaped everything else

An earlier draft of this design put four Lifeplus entries on the home page. The
owner rejected it in one sentence: *"I don't want my entire portfolio to just be
about what I do at Lifeplus. It's a glorified LinkedIn page at that point."*

That is correct, and it is the organising principle here.

A LinkedIn page shows what someone was **employed** to do. A portfolio shows
what they would build if nobody asked. Employer work proves you can ship on a
team — genuinely valuable, and most junior portfolios cannot show it — but it
proves nothing about self-direction. The only substantial evidence of that is
the game lab, which four employer entries would crowd out entirely.

Hence the ratio below. It is the spec's central constraint, and any later change
that pushes it back toward employer-heavy defeats the purpose.

## Decisions

### D1. Home and portfolio collapse into one page

`/` becomes the portfolio. Hero, selected work, about, contact — all on one
page. `/portfolio/` redirects to `/`.

The "two paths, one story" framing goes with it. That metaphor existed to split
software from the game lab; with the game lab as one entry among several, there
are no longer two paths.

### D2. The balance is 2 employer : 1 self-directed : 3 written

**Selected work** carries three entries:

- Auditing a data-integrity bug across production history *(Lifeplus)*
- A regression suite where every test was proven capable of failing *(Lifeplus)*
- Game lab — Unity systems *(self-directed)*

**Field notes** are surfaced as a real section, not a nav link. Three existing
pieces, in the owner's own voice.

The two Lifeplus entries were chosen over the other ten because both are about
**judgement under uncertainty** rather than bug-fixing: a repair that refuses to
run when the numbers look wrong, and a test suite nobody trusted until each test
had been watched to fail. They share a through-line the others do not.

### D3. Lifeplus content sits at "judgement, not system"

The employer is named. What was done may be described. How their systems work
may not.

The line: **describing your reasoning is fine; describing their system is not.**
"I made it report before it writes, with a threshold that aborts if the count
comes back higher than expected" is a pattern applicable anywhere and discloses
nothing. "It queries the transactions table joined to order lines" is Lifeplus's
architecture.

Never carried across, per the source ledger's flag 05: production identifiers,
addresses, internal IP addresses, ticket keys, vendor names, and internal
system, database, table or procedure names.

**Operational figures are rounded hard.** Because the employer is named, a
precise record count becomes a statement about Lifeplus's transaction volume.
"Hundreds of thousands of records" rather than an exact figure. The one
unrounded number retained is "29 of 58 units" — a single order's line count,
which reads as a specific incident rather than a disclosure of scale.

**The period is five and a half months** (9 March – 22 August 2026), not a year.
The commit history is the source and it does not support a longer claim.

### D4. AI assistance is not mentioned, and solo authorship is never claimed

Nearly every Lifeplus commit carries an AI co-author trailer. The owner's
decision is to describe outcomes and say nothing about tooling — standard
practice, and nobody lists their IDE.

The constraint this creates: silence is fine, an affirmative claim is not.
"I built X, which did Y" stays accurate. "Hand-rolled", "wrote every line
myself" and "from scratch" are contradicted by the record and must not appear.

### D5. The roadmap moves to `/game/`, and keeps its dates

It is a game-lab roadmap. It was only ever proposed for the home page because
the game lab was the only content with a genuine sequence. On `/game/` it sits
with the thing it describes, and the home page stays uncluttered.

It stays **dated** — a freshness stamp plus completion months on shipped phases.
Two shipped phases four months apart is evidence of pace, which a
dateless version discards. A stamp that starts looking stale is a prompt to
update it; this site's failure mode has always been quiet neglect, so making
neglect visible is a feature.

Sequence markers are honest here because a roadmap genuinely is a sequence. They
are used nowhere else on the site.

### D6. Visual system — restraint as the distinctive quality

Light warm-neutral paper, serif display, monospace labels, ink-blue accent, and
a drafting grid fading down the page. Visual interest comes from **structure**
(hairline rules, a metadata rail, tick marks, aligned columns) and **texture**
(the grid), not from colour or motion.

The reference is Metronovon — a media artist working in what he calls *Silent
Futures*, translating *mono no aware* into speculative settings, interested in
"what humanity quietly carries through technological change." This is the quiet
end of science fiction, not the neon end. The owner independently selected the
two most restrained of four candidate directions and rejected the atmospheric
warm one, which agrees with the reference rather than contradicting it.

"Lofi" belongs to the same register: warmth, imperfection and restraint. Grain
rather than glow. Muted rather than saturated. Negative space used with
confidence.

Tokens:

| role | value |
| --- | --- |
| ground | `#f6f3ec` |
| ink | `#17171a` |
| ink secondary | `#4b4740` |
| ink tertiary | `#8b8578` |
| rule | `#e0d9c9` |
| accent | `#23405e` |
| surface | `#fffdf8` |

| role | face |
| --- | --- |
| display | Instrument Serif, 400 |
| body | IBM Plex Sans, 400/500/600 |
| label, metadata | IBM Plex Mono, 400/500 |

The existing `data-site-theme="warm-dark"` block in
`src/styles/tailwind.src.css` is replaced, not extended. It works by overriding
Tailwind utility classes with `!important`; leaving it in place while adding a
second theme would produce a fight neither wins.

**`Nav.astro`'s four variants collapse to one.** The variants existed because
the old site had four structurally different navigation bars — a five-link home,
an eight-link portfolio with section anchors, a three-link violet game page, and
a three-link teal project page. With `/portfolio/` merged into `/`, the project
pages gone, and one visual system across the site, there is a single nav: the
name plus links to Work, Notes, Game and Résumé.

Keep the throw-on-invalid guard rather than deleting it with the variants. It
exists because the component previously rendered *empty* when misconfigured,
producing a page with no navigation and a green build. That failure mode is
independent of how many variants there are.

The same applies to `Footer.astro`'s variants and to `BaseLayout`'s required
`active` prop: fewer values, same guards.

### D7. Sparse is acceptable; the site is built to grow

The owner expects to start new projects shortly and add them here. Three
selected-work entries is a starting state, not a target, and a site that looks
slightly under-filled is preferable to one padded with work he has disowned.

This has a concrete consequence. **Adding a project must be a one-file change.**

The Astro migration concluded that this was not achievable and said so plainly.
That conclusion was correct *for the content that existed*: the case studies were
bespoke 180-line pages with five sections, an embedded results chart and an
interactive demo, and squeezing them into a uniform content shape is what had
already hollowed them out to two sentences each in the collection.

The entries in this design are a different shape — problem, built, outcome, a
tech line, in roughly 120 words. That is uniform, and uniform content is exactly
what a content collection is for. So the promise becomes achievable again,
because the content changed rather than because the tooling did.

Selected work is therefore driven by `src/content/projects/`, with a schema
matching the entry format above. The collection's existing entries are replaced
wholesale — their current bodies are the hollowed-out stubs, and their `url`
fields point at pages this spec deletes.

The selected-work section must render every collection entry rather than a
hardcoded list, or adding a file will not be sufficient. This is the reverse of
the migration's decision, and deliberately so: there, driving the section from
the collection would have silently dropped two projects, because the collection
did not match the page. Here the collection becomes the single source, so it
cannot disagree with itself.

**Entries render inline on `/` only. There are no per-project detail pages.**
That is what makes an entry one file: the ~120 words in the collection are the
whole of it. An entry may carry an outbound link — the game lab's points at
`/game/` — but the migration's `/projects/<slug>/` route is not reinstated. If a
future project genuinely warrants a long-form page, that is a deliberate
addition at the time, not a route standing empty waiting for one.

### D8. The old case studies are deleted; their redirects survive

`src/pages/projects/prophetcma.astro`, `signature-extraction.astro` and
`ai-engineering-sandbox.astro` are removed, along with their entries in the
`projects` content collection.

The redirect stubs at `public/prophetcma.html` and its siblings **stay**, but
repoint to `/`. Those URLs have been live and may have been shared; they should
land somewhere sensible rather than 404.

## Page inventory

| Page | Status |
| --- | --- |
| `/` | Rebuilt — hero, selected work (3), field notes, about, contact |
| `/notes/` + 3 articles | Kept as-is, restyled |
| `/game/` | Kept, restyled, gains the dated roadmap |
| `/resume/` | Kept, restyled |
| `/portfolio/` | Redirects to `/` |
| `/projects/*` | Deleted |

Six pages plus three redirect stubs, down from twelve.

## Content — the three selected-work entries

Drafted at the D3 level. These are the copy, not a summary of it.

**Auditing a data-integrity bug across production history**

> A per-transaction flag had been written incorrectly by an earlier bug. Nobody
> knew how many historical records were wrong, and a blind mass-update across a
> live production table was not an acceptable way to find out.
>
> *Built* — An audit that recomputes the correct value for every record and
> reports disagreements without writing anything, then repairs only what the fix
> is responsible for, with a threshold that aborts the run if the disagreement
> count comes back higher than expected.
>
> *Outcome* — Scanned hundreds of thousands of production records in about two
> seconds. Production came back clean, making the repair a verified no-op. A
> pre-production environment was repaired. A third tripped the abort threshold
> and was deliberately left alone for investigation rather than mass-written.

**A regression suite where every test was proven capable of failing**

> A test that passes proves nothing on its own — a check that can never fail
> looks identical to one that works. On a system where a wrong result means
> mis-shipped goods, a green suite was being trusted without anyone establishing
> it would go red for the right reasons.
>
> *Built* — Coverage across unit, integration, full-API and browser-driven
> levels, with a standing practice: for every new test, deliberately reintroduce
> the bug it guards and record that it goes red, plus a control proving it does
> not fire on a legitimate look-alike.
>
> *Outcome* — Over a hundred test files and a full-API harness at roughly eleven
> thousand lines. One change's records covered fourteen injected faults, each
> confirmed to turn its test red. It caught real gaps: an over-broad string match
> passed its own test while silently hijacking an unrelated case, found only
> because the control used verbatim production text rather than an invented one.

**Game lab — Unity systems**

> A Unity URP world demo, built to keep the systems muscle working on something
> nobody commissioned.
>
> *Built* — Procedural terrain, biome data assets driving placement and
> presentation through a registry, editor tooling for authoring them, plus
> combat, squads, save/load and scene setup.
>
> *Outcome* — Two phases shipped across four months, a third in progress. Full
> roadmap on the game page.

## Consequence for the visual regression gate

**Every baseline for a redesigned page becomes invalid.** The gate was built to
prove nothing changed; this project's whole purpose is that things change.

The gate is not weakened for this. The sequence is:

1. Land the content and structure changes first, page by page, re-baselining
   each deliberately after reviewing its diff.
2. Land the visual system as a single change, re-baselining everything at once.
3. From then on the gate resumes its normal job of catching unintended change.

Splitting it that way keeps every re-baseline reviewable. A combined
content-and-restyle change would produce diffs too large to inspect, which is
the same as having no gate.

The link checker and CSS drift guard are unaffected and stay active throughout.

## Risks

**The contract conversation may not go the owner's way.** Both Lifeplus entries
depend on permission to describe employer work publicly. If that is refused, the
site falls back to the game lab, the field notes and the roadmap, and the
selected-work section shrinks to one entry. Worth confirming before the copy is
written rather than after.

**Two employer entries is a floor, not a target.** The ratio exists to stop the
site becoming a work history. Adding more Lifeplus entries later without adding
self-directed work undoes the decision this spec is built on.

**Light and restrained can read as unfinished** if the structural detail is
executed loosely. The drafting grid and the hairline rules are load-bearing, not
decoration — they are what stops negative space reading as absence. This matters
more than usual given D7: a sparse site with confident structure reads as
deliberate, and the same site with weak structure reads as unfinished. The
difference is entirely in the execution of the rules, spacing and alignment.

**The two Lifeplus entries are load-bearing while the site is sparse.** Until
new projects land, they are two of three entries. If the contract conversation
removes them, the site is down to the game lab alone — thin enough that
publishing might be worth deferring until a new project is ready. Worth knowing
before the copy is written.

## Deliberately deferred

**The colophon.** A "how this site is built" page — the visual regression gate,
the link checker, the CSS drift guard, CI that refuses to deploy broken output.
It is strong material, entirely owned, and directly answers "what am I capable
of now." It is also a second piece of work, and this spec is already large.
Revisit once this lands.

**The invisible mobile menu button** on the current portfolio page. It uses
FontAwesome icons that are never loaded. The redesign replaces that markup
anyway, so it resolves itself — but if any part of the old nav survives, this
must be fixed rather than carried across.

## Success criteria

- The home page carries three selected-work entries in the 2:1 ratio, plus a
  field-notes section.
- No page contains an internal system, table, procedure or vendor name, a
  production identifier, or an unrounded operational figure.
- No copy claims solo authorship.
- `npm run build` passes: CSS drift check, build, link check.
- The visual suite is green against deliberately re-established baselines.
- `/portfolio/`, `/prophetcma.html` and its two siblings all resolve rather
  than 404.
- Six pages build, plus three redirect stubs.
