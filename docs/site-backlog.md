# Site backlog

Open items from the whole-branch review of the portfolio redesign
(2026-08-24, branch `portfolio-redesign` at `91ffff0`).

Everything under "must fix before publishing" in that review was fixed in the
follow-up wave (`91ffff0..3fb7a9c`) and independently re-verified. What remains
is recorded here so it is not lost when the scratch workspace is cleared.

## Needs a decision from the site owner

These were deliberately not changed, because they are statements of fact about
the owner's history or judgement calls about his own presentation.

1. **Tenure disagrees between the résumé and the site.** The résumé reads
   "Junior Software Developer — Lifeplus, June 2025 – Present"; the home page
   rail and the source work-ledger put the developer work in 2026, about five
   and a half months. Both can be true — employment from June 2025, the
   developer role from March 2026 — but a reader sees a contradiction. If it is
   two phases of one job, saying so explicitly is stronger than either version.

2. **The résumé's skills section is the pre-Lifeplus graduate list.** It names
   Python, R, pandas, scikit-learn, XGBoost, Azure Document Intelligence, and
   omits TypeScript, Vitest, Playwright, Docker, CI and T-SQL — the stack the
   home page's two flagship entries lead with. The résumé and the projects
   currently describe two different candidates.

3. **The notes pages wrap their content in a bordered panel; home and game do
   not.** `.glass-panel` appears nowhere else on the site. Inherited from the
   pre-redesign notes pages rather than part of the new structural system, but a
   reading surface for long-form text is a defensible choice. Purely aesthetic.

4. **`/coverletter.pdf` is publicly fetchable and linked from nowhere.** Likely
   fine; a cover letter is usually written for one recipient, so worth an
   explicit decision rather than an assumption.

## Worth doing soon — visible, and it costs credibility

5. **`/game/` says the same four things twice** across seven cards in two
   different card styles.

6. **`/game/` shows nothing.** It claims geometry-shader grass, low-poly water
   and visual polish, and contains zero images. The whole site has none. This is
   the cheapest large improvement available: the work already exists.

7. **Three of seven pages never use the display serif.** `/notes/` and the
   article pages are heavy sans; the résumé is its own thing. The type system
   does not read as one system across the site.

8. **`/notes/` is hardcoded rather than collection-driven** and has already
   drifted from its own content collection once. It should render from the
   collection like the projects do.

9. **PROBLEM / BUILT / OUTCOME render as `<h2>` nested under an `<h3>`** —
   nine mis-levelled headings, an accessibility and semantics defect.

10. **The notes carry `pubDate` and never render it.** Dated writing reads as
    maintained; undated writing reads as abandoned.

11. **Home entries measure ~32 characters per line at 375px.** Too narrow to
    read comfortably.

12. **`og:image` is an SVG**, so every LinkedIn and Slack share renders a blank
    card. Needs a 1200×630 PNG at `public/assets/images/og-card.png`.

13. **Stale JSON-LD.**

## Housekeeping

14. Unreferenced published files (`/MAEresults.png`, and see item 4).
15. `html { scroll-padding-top: 5.5rem }` is clearance for a fixed nav that no
    longer exists.
16. `README.md` still documents the deleted Vanta.NET/Three.js hero animation.
17. `.glass-panel` and `.metric-card` are vocabulary from the deleted theme.
18. `<nav>` has no `aria-label`.
19. `scroll-behavior: smooth` is not gated on `prefers-reduced-motion`.

## Also outstanding, outside this repo

- **Publication permission for the employer-attributed work has not been
  confirmed.** The two Lifeplus entries obey the agreed disclosure rules — no
  identifiers, addresses, ticket keys, vendor names, or internal system,
  database, table or procedure names, and volume figures rounded hard — but
  whether the work may be described publicly at all is governed by the
  employment agreement, not by how carefully it has been anonymised.
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
