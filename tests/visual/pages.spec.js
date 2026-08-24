import { test, expect } from '@playwright/test';

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

// `expectTitle` is a distinctive substring of the page's real <title>,
// read directly from each source file. It is asserted right after
// navigation, before the screenshot is taken, so a wrong response
// (e.g. a directory listing, a 404 page, a redirect to the wrong
// place) fails loudly instead of silently becoming the baseline.
// It is used as `new RegExp(expectTitle)`, so a literal "|" in a
// title (e.g. "Now | Timothy Creekmore") must be escaped as "\|".
//
// These baselines represent the redesigned site as of this commit. The
// suite now has exactly one mode — the static site these tests used to
// also check against no longer exists, so there is nothing left to switch.
const PAGES = [
  { name: 'home',        astro: '/',                          expectTitle: 'Software Developer' },
  { name: 'game',        astro: '/game/',                     expectTitle: 'Voxel game' },
  { name: 'notes',       astro: '/notes/',                    expectTitle: 'Field Notes' },
  { name: 'note-repair', astro: '/notes/repair-debugging/',   expectTitle: 'Appliance Repair Taught Me About Debugging' },
  { name: 'note-ocr',    astro: '/notes/ocr-human-review/',   expectTitle: 'OCR Projects Need Human Review' },
  { name: 'note-biomes', astro: '/notes/biomes-data-assets/', expectTitle: 'Designing Biomes as Data Assets' },
  { name: 'resume',      astro: '/resume/',                   expectTitle: 'Timothy Creekmore Resume' },
];

for (const p of PAGES) {
  test(`${p.name}`, async ({ page }) => {
    await page.goto(p.astro, { waitUntil: 'networkidle' });
    // Correctness gate: fail loudly if this isn't actually the expected
    // page (wrong file served, directory listing, 404, bad redirect)
    // instead of silently baselining the wrong screenshot.
    await expect(page).toHaveTitle(new RegExp(p.expectTitle));
    // The footer year is written by hub-nav.js; wait for it so captures are stable.
    await page.waitForFunction(() => {
      const el = document.getElementById('footer-year');
      return el === null || el.textContent.trim().length === 4;
    });
    // display=swap (Seo.astro) paints in a fallback font immediately and
    // swaps to the real face once it loads. `networkidle` only guarantees
    // the font files finished downloading, not that the swap has painted --
    // a capture inside that window would record fallback-font glyph
    // metrics, which at this suite's zero-tolerance threshold is a diff.
    // Do not remove this as "redundant with networkidle": it closes a real
    // gap networkidle leaves open, even though it did not turn out to be
    // the cause of the notes-mobile flake this was added to chase (that
    // one traces to anti-aliasing jitter on a rounded-corner border, not
    // to fonts -- see task-7-report.md, fix round 2).
    await page.evaluate(() => document.fonts.ready);
    await expect(page).toHaveScreenshot(`${p.name}.png`, { fullPage: true });
  });
}
