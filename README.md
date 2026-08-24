# timothycreekmore.com

Personal brand site for Timothy Creekmore — software developer, AI/data builder, and systems thinker.

Live: <https://www.timothycreekmore.com/> (GitHub Pages, custom domain via `CNAME`)

## Stack

- [Astro](https://astro.build/) v5, static output (`output: 'static'`). `src/` is the only source; `npx astro build` renders it to `dist/`.
- Styling: [Tailwind CSS](https://tailwindcss.com/) v3, pre-built and minified into [`public/assets/css/tailwind.css`](public/assets/css/tailwind.css). Source: [`src/styles/tailwind.src.css`](src/styles/tailwind.src.css). Run `npm run build:css` after editing any `.astro`/`.mdx` file or `tailwind.src.css` — the Tailwind content scan reads the markup, so new utility classes in a page change the built stylesheet.
- Type: Instrument Serif (display) and IBM Plex Mono (labels, rails, ticks), with the body in the system sans stack.
- Icons and imagery: inline text and SVG marks only. The site currently ships no raster images — see [TODO assets](#todo-assets).
- No client framework, no contact form, and no third-party runtime dependencies. The only JavaScript that ships is [`public/assets/js/hub-nav.js`](public/assets/js/hub-nav.js) (footer year stamp + an optional analytics click binding) and a small inline script on the home page that reveals the contact email on interaction so it is not sitting in the markup for scrapers.

The visual language is a light "drafting" skin — a paper ground, hairline rules, and a faint 1px grid (`.grid-field`) behind six of the seven pages. It is defined under `body[data-site-theme="light"]` in `tailwind.src.css`. `/resume/` deliberately opts out: it is a print surface.

## Structure

```
/
├── src/                        Astro source — the only source, builds to dist/
│   ├── pages/
│   │   ├── index.astro         Home: selected work, field notes, about, contact
│   │   ├── game.astro          Game lab splash (links to the Godot web export)
│   │   ├── resume.astro        Résumé page (standalone ResumeLayout)
│   │   └── notes/
│   │       ├── index.astro     Field notes index
│   │       └── [slug].astro    Note detail, driven by src/content/notes/*.mdx
│   ├── content/
│   │   ├── notes/*.mdx         Field note bodies (3)
│   │   └── projects/*.mdx      Selected-work entries rendered on the home page (3)
│   ├── content.config.ts       Content collection schemas (projects, notes)
│   ├── layouts/                BaseLayout, NoteLayout, ResumeLayout
│   ├── components/             Nav, Footer, NoteParagraph, Seo
│   └── styles/tailwind.src.css Tailwind input (directives + site overrides)
├── public/                     Copied verbatim into dist/ by astro build
│   ├── assets/css/tailwind.css     Built, minified Tailwind output (committed)
│   ├── assets/images/og-card.svg   Social sharing image
│   ├── assets/js/hub-nav.js        Footer-year stamp + analytics binding
│   ├── game/play/                  Built Godot web export, served at /game/play/
│   ├── portfolio/index.html        Redirect stub → /
│   ├── prophetcma.html             Redirect stub → /
│   ├── signature-extraction.html   Redirect stub → /
│   ├── ai-engineering-sandbox.html Redirect stub → /
│   ├── resume.pdf              Linked from the home page and the résumé page
│   ├── robots.txt
│   └── CNAME                   www.timothycreekmore.com
├── game/                       Game subproject sources — NOT web output
│   ├── unity-world-demo/       Unity URP project
│   └── world-demo/             Godot project (source for public/game/play/'s export)
├── scripts/                    check-css.mjs, check-links.mjs, build-resume.mjs,
│                               static-server.mjs, serve-godot-export.py
├── tests/visual/               Playwright visual regression suite + baselines
├── astro.config.mjs            Site URL, integrations (mdx, sitemap), static output
├── tailwind.config.js          Content paths (src/, public/assets/js/) + brand color tokens
├── package.json                Build, dev, and check scripts
└── .github/workflows/deploy.yml   CI: build + deploy to GitHub Pages
```

The build produces seven pages: `/`, `/game/`, `/notes/`, three note detail pages, and `/resume/`. The four redirect stubs under `public/` are the pre-Astro URLs; they are kept so old links and search results do not 404, and they all now land on the home page because the pages they used to point at no longer exist.

`sitemap.xml` is not committed — `@astrojs/sitemap` generates `dist/sitemap-index.xml` (and `dist/sitemap-0.xml`) at build time.

Active nav state is server-rendered: `Nav.astro` sets `aria-current="page"` on the current link, and the stylesheet keys the visible state off that same attribute. There is no JavaScript involved and no mobile menu — the nav is a single row that wraps.

## Local development

One-time:

```bash
npm install
```

Then in two terminals (or just rebuild after each edit):

```bash
# rebuild CSS on save
npm run watch:css

# Astro dev server with hot reload
npm run astro:dev
```

Open <http://localhost:4321/> (Astro's default dev port).

## Build for deploy

```bash
npm run build
```

This chains `check:css` (fails if the committed stylesheet is stale) → `build:css` (rebuilds `public/assets/css/tailwind.css`) → `npx astro build` (renders `src/` to `dist/`) → `check:links` (verifies every internal link/asset reference in `dist/` resolves). All four must pass; the committed `public/assets/css/tailwind.css` should always match a fresh `build:css` output.

Rebuild the résumé PDF (not part of CI — needs a local Chrome install):

```bash
npm run build:resume
```

## Visual regression suite

`tests/visual/` screenshots all seven pages at desktop (1440×900) and mobile (390×844) and compares them against committed baselines at **zero tolerance** — `threshold: 0`, `maxDiffPixelRatio: 0`. Any pixel change fails.

The Playwright config has no `webServer` block, so it will not start anything for you. Build first and serve `dist/` on port 3000 yourself:

```bash
npm run build
npm run serve:dist        # node scripts/static-server.mjs dist 3000
npm run visual            # in a second terminal
```

Because tolerance is zero, anything nondeterministic in a page will fail the suite intermittently rather than loudly. Sort keys are the usual culprit: give every list a total ordering rather than one that can tie. When a change is *meant* to alter the rendering, re-record deliberately with `npx playwright test --update-snapshots` and check that the committed baseline diff shows only the intended change.

## Deploy

Pushes to `main` are built and published by `.github/workflows/deploy.yml` via GitHub Actions → GitHub Pages (`npm ci` → `npm run build` → upload `dist/`). The `public/CNAME` file (copied to `dist/CNAME` by the build) pins the custom domain `www.timothycreekmore.com`.

## TODO assets

- The site ships no images at all. `/game/` in particular describes geometry-shader grass, low-poly water, and visual polish while showing none of it. Screenshots or a short capture from the Unity and Godot projects are the cheapest large improvement available.
- `og:image` is currently an SVG (`public/assets/images/og-card.svg`), which most social platforms will not render. A 1200×630 PNG is needed for link previews to work.

## Analytics (opt-in)

No analytics ship by default. `hub-nav.js` already binds click events on any element carrying a `data-analytics` attribute and forwards them to `window.plausible` if it exists, so adding [Plausible](https://plausible.io/) is just a matter of loading it — drop this into `BaseLayout.astro` and `ResumeLayout.astro`:

```html
<script defer data-domain="timothycreekmore.com" src="https://plausible.io/js/script.js"></script>
```

[Umami](https://umami.is/) (self-hostable) works the same way.
