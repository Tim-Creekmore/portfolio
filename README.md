# timothycreekmore.com

Personal hub for Timothy Creekmore — data scientist and AI engineer.

Live: <https://www.timothycreekmore.com/> (GitHub Pages, custom domain via `CNAME`)

## Stack

- Static HTML.
- Styling: [Tailwind CSS](https://tailwindcss.com/) v3, pre-built and minified into [`assets/css/tailwind.css`](assets/css/tailwind.css) (~20 KB). Source: [`assets/css/tailwind.src.css`](assets/css/tailwind.src.css). Run `npm run build:css` after editing any HTML / `tailwind.src.css`.
- Icons: [Font Awesome 6](https://fontawesome.com/) via CDN, plus [Devicon](https://devicon.dev/) on the portfolio for tech-stack logos. Planned: subset / replace with inline SVG to drop another CDN.
- Portfolio hero animation: [Vanta.NET](https://www.vantajs.com/) + Three.js r134. Lazy-loaded on `IntersectionObserver`-style gate — skipped on `prefers-reduced-motion` and viewports under 768 px.
- Contact form: [FormSubmit](https://formsubmit.co/) — no backend required.
- Shared nav behavior: [`assets/js/hub-nav.js`](assets/js/hub-nav.js) (mobile toggle + active-link highlight via `<body data-hub="…">` + footer year stamp).

## Structure

```
/                          GitHub Pages root
├── index.html             Hub home
├── portfolio/index.html   Portfolio (about, skills, projects, cover letter, contact)
├── shop/index.html        Shop placeholder
├── content/index.html     Content/video placeholder
├── game/index.html        Voxel game splash (linked to Unity URP demo)
├── prophetcma.html        Project subpage — ProphetCMA
├── signature-extraction.html  Project subpage — signature OCR
├── robots.txt
├── sitemap.xml
├── coverletter.pdf        Linked from portfolio
├── resume.pdf             Linked from every page nav
├── MAEresults.png         ProphetCMA chart
├── CNAME                  www.timothycreekmore.com
├── assets/
│   ├── css/tailwind.src.css   Input (directives + site overrides)
│   ├── css/tailwind.css       Built, minified output (committed)
│   └── js/hub-nav.js          Shared nav + footer-year behavior
├── package.json           Tailwind build scripts
├── tailwind.config.js     Content paths + brand color tokens
└── game/                  Game subproject (Unity + Godot demos)
```

## Local development

One-time:

```bash
npm install
```

Then in two terminals (or just rebuild after each HTML edit):

```bash
# rebuild CSS on save
npm run watch:css

# serve the static site
npm run serve
```

Open <http://localhost:3000/> (port from `npx serve`).

## Build for deploy

```bash
npm run build:css
```

The committed `assets/css/tailwind.css` is the artifact GitHub Pages serves. Re-run after any change to HTML or `tailwind.src.css`.

## Deploy

Pushes to `main` are published by GitHub Pages. The `CNAME` file pins the custom domain `www.timothycreekmore.com`.

## TODO assets

- `assets/images/og-card.png` — 1200×630 Open Graph share image (referenced by every page's `og:image` / `twitter:image` meta). Until added, link previews on LinkedIn/Discord/X will fall back to no image.

## Analytics (opt-in)

No analytics ship by default. To add [Plausible](https://plausible.io/) — privacy-friendly, no cookie banner needed in most jurisdictions — drop this into every page's `<head>`:

```html
<script defer data-domain="timothycreekmore.com" src="https://plausible.io/js/script.js"></script>
```

Or use [Umami](https://umami.is/) (self-hostable). Either way, add once per page or wrap into `assets/js/hub-nav.js` for one-line propagation.

## Form security

The contact form uses [FormSubmit](https://formsubmit.co/). The `action` URL currently embeds the Gmail address, which means scrapers can harvest it. To fix:

1. Submit the form once from the live site so FormSubmit registers your email.
2. Confirm via the email FormSubmit sends.
3. In the dashboard, copy the **hashed endpoint** (`https://formsubmit.co/el/<random>`).
4. Replace `https://formsubmit.co/timcreekmore2002@gmail.com` in `portfolio/index.html` with the hashed URL.

`_captcha=true`, `_template=table`, and a `_honey` honeypot are already wired up.
