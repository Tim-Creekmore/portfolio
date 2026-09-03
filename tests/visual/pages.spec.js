import { test, expect } from '@playwright/test';

// This file used to open with a `beforeEach` that aborted the three.js and
// vanta.net CDN requests and overrode `window.matchMedia` to force
// `prefers-reduced-motion: reduce`, all to stop Vanta.NET's live WebGL
// background from rendering into every screenshot. The redesign deleted that
// animation: nothing in `src/` or `public/` references Vanta, three.js, or
// `#vanta-bg` any more, and `hub-nav.js` no longer loads anything from a CDN.
// The suppression was removed with it. If a live background is ever
// reintroduced, it will need equivalent suppression here — the zero-tolerance
// threshold in playwright.config.js cannot absorb an animated surface.

// `expectTitle` is a distinctive substring of the page's real <title>,
// read directly from each source file. It is asserted right after
// navigation, before the screenshot is taken, so a wrong response
// (e.g. a directory listing, a 404 page, a redirect to the wrong
// place) fails loudly instead of silently becoming the baseline.
// It is used as `new RegExp(expectTitle)`, so a literal "|" in a
// title (e.g. "Foo | Bar") must be escaped as "\|". No current row
// needs this, but any future page whose title uses a pipe will.
//
// These baselines represent the redesigned site as of this commit. The
// suite now has exactly one mode — the static site these tests used to
// also check against no longer exists, so there is nothing left to switch.
const PAGES = [
  { name: 'home',        astro: '/',                          expectTitle: 'Software Developer' },
  { name: 'game',        astro: '/game/',                     expectTitle: 'Extraction shooter' },
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
