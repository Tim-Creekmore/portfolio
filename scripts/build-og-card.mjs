// Renders the social share card to public/assets/images/og-card.png.
//
// This exists because og:image was an SVG, and no major platform renders SVG
// link previews -- every share of this site produced a blank card. The card is
// generated rather than hand-exported so it stays in step with the site's
// palette and type, and so it can be regenerated after a rebrand without
// needing a design tool.
//
// Run: node scripts/build-og-card.mjs
//
// 1200x630 is the size Open Graph consumers crop against; deviceScaleFactor 1
// keeps the file small enough that slow clients still fetch it before the
// preview is composed.

import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const output = join(root, 'public', 'assets', 'images', 'og-card.png');

const html = `<!doctype html><html><head><meta charset="utf-8">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;500&family=IBM+Plex+Sans:wght@400;500&family=Instrument+Serif&display=swap">
<style>
  *{margin:0;padding:0;box-sizing:border-box}
  body{width:1200px;height:630px;background:#f6f3ec;color:#17171a;
       font-family:'IBM Plex Sans',sans-serif;position:relative;overflow:hidden}
  .grid{position:absolute;inset:0;opacity:.55;
    background-image:linear-gradient(#dcd5c6 1px,transparent 1px),
                     linear-gradient(90deg,#dcd5c6 1px,transparent 1px);
    background-size:48px 48px;
    -webkit-mask-image:linear-gradient(160deg,#000 0%,rgba(0,0,0,.25) 60%,transparent 92%)}
  .in{position:relative;padding:84px 88px;height:100%;
      display:flex;flex-direction:column;justify-content:space-between}
  .eyebrow{font-family:'IBM Plex Mono',monospace;font-size:17px;letter-spacing:.2em;
           text-transform:uppercase;color:#726d62}
  h1{font-family:'Instrument Serif',Georgia,serif;font-weight:400;font-size:104px;
     line-height:1.02;letter-spacing:-.015em;margin:26px 0 20px}
  .sub{font-size:29px;color:#4b4740;line-height:1.45;max-width:820px}
  .rule{height:1px;background:#e0d9c9;margin:0 0 26px}
  .foot{display:flex;justify-content:space-between;align-items:baseline;
        font-family:'IBM Plex Mono',monospace;font-size:19px;color:#726d62;letter-spacing:.06em}
  .foot b{color:#23405e;font-weight:500}
</style></head><body>
<div class="grid"></div>
<div class="in">
  <div>
    <p class="eyebrow">Software Developer</p>
    <h1>Timothy Creekmore</h1>
    <p class="sub">Production software, tested properly &mdash; plus a systems lab disguised as a medieval world.</p>
  </div>
  <div>
    <div class="rule"></div>
    <div class="foot">
      <span><b>timothycreekmore.com</b></span>
      <span>TypeScript &middot; React &middot; Python &middot; SQL</span>
    </div>
  </div>
</div></body></html>`;

const browser = await chromium.launch();
const page = await browser.newPage({
  viewport: { width: 1200, height: 630 },
  deviceScaleFactor: 1,
});
await page.setContent(html, { waitUntil: 'networkidle' });
// The display=swap faces paint in a fallback first; without this the card can
// be captured mid-swap with the wrong glyph metrics.
await page.evaluate(() => document.fonts.ready);
await mkdir(dirname(output), { recursive: true });
await page.screenshot({ path: output });
await browser.close();

console.log(`wrote ${output}`);
