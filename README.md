# timothycreekmore.com

Personal brand site for Timothy Creekmore — software developer, AI/data builder, and systems thinker.

Live: <https://www.timothycreekmore.com/> (GitHub Pages, custom domain via `CNAME`)

## Stack

- [Astro](https://astro.build/) v5, static output (`output: 'static'`). `src/` is the only source; `npx astro build` renders it to `dist/`.
- Styling: [Tailwind CSS](https://tailwindcss.com/) v3, pre-built and minified into [`public/assets/css/tailwind.css`](public/assets/css/tailwind.css). Source: [`src/styles/tailwind.src.css`](src/styles/tailwind.src.css). Run `npm run build:css` after editing any `.astro`/`.mdx` file or `tailwind.src.css`.
- Icons: mostly inline text/SVG-style marks. Font/CDN icon usage is being removed as pages are touched.
- Portfolio hero animation: [Vanta.NET](https://www.vantajs.com/) + Three.js r134. Lazy-loaded on `IntersectionObserver`-style gate — skipped on `prefers-reduced-motion` and viewports under 768 px.
- Contact form: [FormSubmit](https://formsubmit.co/) — no backend required.
- Shared nav behavior: [`public/assets/js/hub-nav.js`](public/assets/js/hub-nav.js) (mobile toggle + active-link highlight via `<body data-hub="…">` + footer year stamp).

## Structure

```
/
├── src/                        Astro source — the only source, builds to dist/
│   ├── pages/
│   │   ├── index.astro         Hub home
│   │   ├── portfolio.astro     Portfolio (about, skills, projects, cover letter, contact)
│   │   ├── game.astro          Voxel game splash (linked to Unity URP demo)
│   │   ├── resume.astro        Resume page (standalone ResumeLayout)
│   │   ├── notes/
│   │   │   ├── index.astro     Field notes index
│   │   │   └── [slug].astro    Note detail, driven by src/content/notes/*.mdx
│   │   └── projects/
│   │       ├── prophetcma.astro
│   │       ├── signature-extraction.astro
│   │       └── ai-engineering-sandbox.astro
│   ├── content/
│   │   ├── notes/*.mdx         Field note bodies
│   │   └── projects/*.mdx      Case study bodies
│   ├── content.config.ts       Content collection schemas
│   ├── layouts/                BaseLayout, CaseStudyLayout, NoteLayout, ResumeLayout
│   ├── components/             Nav, Footer, GlassPanel, MetricCard, NoteParagraph, Seo
│   └── styles/tailwind.src.css Tailwind input (directives + site overrides)
├── public/                     Copied verbatim into dist/ by astro build
│   ├── assets/css/tailwind.css     Built, minified Tailwind output (committed)
│   ├── assets/images/og-card.svg   Social sharing image
│   ├── assets/js/hub-nav.js        Shared nav + footer-year behavior
│   ├── game/play/                  Built Godot web export, served at /game/play/
│   ├── prophetcma.html             Redirect stub → /projects/prophetcma/
│   ├── signature-extraction.html   Redirect stub → /projects/signature-extraction/
│   ├── ai-engineering-sandbox.html Redirect stub → /projects/ai-engineering-sandbox/
│   ├── robots.txt
│   ├── coverletter.pdf         Linked from portfolio
│   ├── resume.pdf              Linked from every page nav
│   ├── MAEresults.png          ProphetCMA chart
│   └── CNAME                   www.timothycreekmore.com
├── game/                       Game subproject sources — NOT web output
│   ├── unity-world-demo/       Unity URP project
│   └── world-demo/             Godot project (source for public/game/play/'s export)
├── scripts/                    check-css.mjs, check-links.mjs, build-resume.mjs, static-server.mjs
├── tests/visual/               Playwright visual regression suite + baselines
├── astro.config.mjs            Site URL, integrations (mdx, sitemap), static output
├── tailwind.config.js          Content paths (src/, public/assets/js/) + brand color tokens
├── package.json                Build, dev, and check scripts
└── .github/workflows/deploy.yml   CI: build + deploy to GitHub Pages
```

`sitemap.xml` is no longer committed — `@astrojs/sitemap` generates `dist/sitemap-index.xml` (and `dist/sitemap-0.xml`) at build time.

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

Rebuild the resume PDF (not part of CI — needs a local Chrome install):

```bash
npm run build:resume
```

## Deploy

Pushes to `main` are built and published by `.github/workflows/deploy.yml` via GitHub Actions → GitHub Pages. The `public/CNAME` file (copied to `dist/CNAME` by the build) pins the custom domain `www.timothycreekmore.com`.

## TODO assets

- Project-specific screenshots for ProphetCMA, Signature Extraction, the AI Engineering Sandbox, and the Unity Game Lab.

## Analytics (opt-in)

No analytics ship by default. To add [Plausible](https://plausible.io/) — privacy-friendly, no cookie banner needed in most jurisdictions — drop this into every page's `<head>`:

```html
<script defer data-domain="timothycreekmore.com" src="https://plausible.io/js/script.js"></script>
```

Or use [Umami](https://umami.is/) (self-hostable). Either way, add once per page (`BaseLayout.astro`/`ResumeLayout.astro`) or wrap into `public/assets/js/hub-nav.js` for one-line propagation.

## Form security

The contact form uses [FormSubmit](https://formsubmit.co/). The `action` URL currently embeds the Gmail address, which means scrapers can harvest it. To fix:

1. Submit the form once from the live site so FormSubmit registers your email.
2. Confirm via the email FormSubmit sends.
3. In the dashboard, copy the **hashed endpoint** (`https://formsubmit.co/el/<random>`).
4. Replace `https://formsubmit.co/timcreekmore2002@gmail.com` in `src/pages/portfolio.astro` with the hashed URL.

`_captcha=true`, `_template=table`, and a `_honey` honeypot are already wired up.
