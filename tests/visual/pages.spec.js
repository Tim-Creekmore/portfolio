import { test, expect } from '@playwright/test';

const MODE = process.env.VISUAL_MODE || 'static';

// Static and Astro URLs differ for project and note detail pages.
// `astro: null` means the page is intentionally deleted in the migration
// (spec D4) and is baselined but never compared.
const PAGES = [
  { name: 'home',              static: '/',                              astro: '/' },
  { name: 'portfolio',         static: '/portfolio/',                    astro: '/portfolio/' },
  { name: 'game',              static: '/game/',                         astro: '/game/' },
  { name: 'notes',             static: '/notes/',                        astro: '/notes/' },
  { name: 'note-repair',       static: '/notes/repair-debugging.html',   astro: '/notes/repair-debugging/' },
  { name: 'note-ocr',          static: '/notes/ocr-human-review.html',   astro: '/notes/ocr-human-review/' },
  { name: 'note-biomes',       static: '/notes/biomes-data-assets.html', astro: '/notes/biomes-data-assets/' },
  { name: 'proj-prophetcma',   static: '/prophetcma.html',               astro: '/projects/prophetcma/' },
  { name: 'proj-signature',    static: '/signature-extraction.html',     astro: '/projects/signature-extraction/' },
  { name: 'proj-sandbox',      static: '/ai-engineering-sandbox.html',   astro: '/projects/ai-engineering-sandbox/' },
  { name: 'resume',            static: '/resume.html',                   astro: '/resume/' },
  { name: 'now',               static: '/now/',                          astro: null },
  { name: 'uses',              static: '/uses/',                         astro: null },
];

for (const p of PAGES) {
  test(`${p.name}`, async ({ page }) => {
    const path = MODE === 'static' ? p.static : p.astro;
    test.skip(path === null, `${p.name} is deleted in the Astro build`);

    await page.goto(path, { waitUntil: 'networkidle' });
    // The footer year is written by hub-nav.js; wait for it so captures are stable.
    await page.waitForFunction(() => {
      const el = document.getElementById('footer-year');
      return el === null || el.textContent.trim().length === 4;
    });
    await expect(page).toHaveScreenshot(`${p.name}.png`, { fullPage: true });
  });
}
