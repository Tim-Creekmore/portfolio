import { test, expect } from '@playwright/test';

const MODE = process.env.VISUAL_MODE || 'static';

// Vanta.NET is live WebGL and renders into every desktop screenshot unless
// actively suppressed — Playwright's `reducedMotion: 'reduce'` context
// option does NOT make `window.matchMedia('(prefers-reduced-motion:
// reduce)').matches` true in this Playwright/Chromium combination (verified:
// it reports `false` even with the setting applied), so hub-nav.js's own
// `prefers-reduced-motion` check never trips and Vanta loads and animates.
// Belt-and-suspenders fix, proven by a throwaway probe spec to bring
// `#vanta-bg canvas` count to 0 on BOTH desktop and mobile projects:
//   1. Abort the two CDN requests (three.js, vanta.net) hub-nav.js loads
//      Vanta from, so the library never arrives and no canvas is ever
//      created.
//   2. Override `window.matchMedia` via `addInitScript` (runs before any
//      page script) so a `prefers-reduced-motion` query reports
//      `matches: true`, in case Vanta is ever loaded a different way.
test.beforeEach(async ({ page }) => {
  await page.route('https://cdnjs.cloudflare.com/ajax/libs/three.js/**', (route) => route.abort());
  await page.route('https://cdn.jsdelivr.net/npm/vanta@**', (route) => route.abort());
  await page.addInitScript(() => {
    const originalMatchMedia = window.matchMedia.bind(window);
    window.matchMedia = (query) => {
      if (typeof query === 'string' && query.includes('prefers-reduced-motion')) {
        return {
          matches: true,
          media: query,
          onchange: null,
          addListener() {},
          removeListener() {},
          addEventListener() {},
          removeEventListener() {},
          dispatchEvent() {
            return false;
          },
        };
      }
      return originalMatchMedia(query);
    };
  });
});

// Static and Astro URLs differ for project and note detail pages.
// `astro: null` means the page is intentionally deleted in the migration
// (spec D4) and is baselined but never compared.
//
// `expectTitle` is a distinctive substring of the page's real <title>,
// read directly from each source file. It is asserted right after
// navigation, before the screenshot is taken, so a wrong response
// (e.g. a directory listing, a 404 page, a redirect to the wrong
// place) fails loudly instead of silently becoming the baseline.
// It is used as `new RegExp(expectTitle)`, so a literal "|" in a
// title (e.g. "Now | Timothy Creekmore") must be escaped as "\|".
// CUTOVER NOTE (Task 13, commit cce9d70): the static site this
// `static:` column points at was deleted at cutover — all root `.html`
// files, `portfolio/`, `game/index.html`, `notes/*.html`, `now/`, `uses/`,
// `shop/`, and `content/` are gone from the working tree as of that commit.
// `VISUAL_MODE=static` can therefore no longer run; every `static:` URL
// below now 404s against a checkout at or after this commit. The columns
// are kept (not deleted) because they document what the pre-migration site
// looked like and are what the committed baselines under
// `tests/visual/__snapshots__/` were originally captured from. The
// baselines remain the reference for `VISUAL_MODE=astro` (the default/only
// runnable mode going forward) — do not re-baseline against them expecting
// `static:` to still work.
const PAGES = [
  { name: 'home',              static: '/',                              astro: '/',                                  expectTitle: 'Systems Thinker' },
  { name: 'portfolio',         static: '/portfolio/',                    astro: '/portfolio/',                        expectTitle: 'Portfolio' },
  { name: 'game',              static: '/game/',                         astro: '/game/',                             expectTitle: 'Voxel game' },
  { name: 'notes',             static: '/notes/',                        astro: '/notes/',                            expectTitle: 'Field Notes' },
  { name: 'note-repair',       static: '/notes/repair-debugging.html',   astro: '/notes/repair-debugging/',           expectTitle: 'Appliance Repair Taught Me About Debugging' },
  { name: 'note-ocr',          static: '/notes/ocr-human-review.html',   astro: '/notes/ocr-human-review/',           expectTitle: 'OCR Projects Need Human Review' },
  { name: 'note-biomes',       static: '/notes/biomes-data-assets.html', astro: '/notes/biomes-data-assets/',         expectTitle: 'Designing Biomes as Data Assets' },
  { name: 'proj-prophetcma',   static: '/prophetcma.html',               astro: '/projects/prophetcma/',              expectTitle: 'ProphetCMA' },
  { name: 'proj-signature',    static: '/signature-extraction.html',     astro: '/projects/signature-extraction/',    expectTitle: 'Signature Data Extraction' },
  { name: 'proj-sandbox',      static: '/ai-engineering-sandbox.html',   astro: '/projects/ai-engineering-sandbox/',  expectTitle: 'AI Engineering Sandbox' },
  { name: 'resume',            static: '/resume.html',                   astro: '/resume/',                          expectTitle: 'Timothy Creekmore Resume' },
  { name: 'now',               static: '/now/',                          astro: null,                                 expectTitle: 'Now \\| Timothy Creekmore' },
  { name: 'uses',              static: '/uses/',                         astro: null,                                 expectTitle: 'Uses \\| Timothy Creekmore' },
];

for (const p of PAGES) {
  test(`${p.name}`, async ({ page }) => {
    const path = MODE === 'static' ? p.static : p.astro;
    test.skip(path === null, `${p.name} is deleted in the Astro build`);

    await page.goto(path, { waitUntil: 'networkidle' });
    // Correctness gate: fail loudly if this isn't actually the expected
    // page (wrong file served, directory listing, 404, bad redirect)
    // instead of silently baselining the wrong screenshot.
    await expect(page).toHaveTitle(new RegExp(p.expectTitle));
    // The footer year is written by hub-nav.js; wait for it so captures are stable.
    await page.waitForFunction(() => {
      const el = document.getElementById('footer-year');
      return el === null || el.textContent.trim().length === 4;
    });
    await expect(page).toHaveScreenshot(`${p.name}.png`, { fullPage: true });
  });
}
