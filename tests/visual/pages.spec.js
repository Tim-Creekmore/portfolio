import { test, expect } from '@playwright/test';

const MODE = process.env.VISUAL_MODE || 'static';

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
