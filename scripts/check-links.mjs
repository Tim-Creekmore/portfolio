import { existsSync, readFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = dirname(fileURLToPath(new URL('../package.json', import.meta.url)));
const htmlFiles = [
  'index.html',
  'portfolio/index.html',
  'game/index.html',
  'prophetcma.html',
  'signature-extraction.html',
  'ai-engineering-sandbox.html',
  'now/index.html',
  'notes/index.html',
  'notes/repair-debugging.html',
  'notes/ocr-human-review.html',
  'notes/biomes-data-assets.html',
  'uses/index.html'
];

let failures = 0;

for (const file of htmlFiles) {
  const full = join(root, file);
  if (!existsSync(full)) {
    console.error(`Missing HTML file: ${file}`);
    failures++;
    continue;
  }

  const html = readFileSync(full, 'utf8');
  const links = [...html.matchAll(/\b(?:href|src)=["']([^"']+)["']/g)].map((m) => m[1]);

  for (const link of links) {
    if (
      link.startsWith('http') ||
      link.startsWith('mailto:') ||
      link.startsWith('data:') ||
      link.startsWith('#')
    ) continue;

    const clean = link.split('#')[0].split('?')[0];
    if (!clean) continue;

    const target = clean.startsWith('/')
      ? join(root, clean.replace(/^\//, ''))
      : join(dirname(full), clean);
    const indexTarget = clean.endsWith('/') ? join(root, clean.replace(/^\//, ''), 'index.html') : target;

    if (!existsSync(indexTarget)) {
      console.error(`Broken link in ${file}: ${link}`);
      failures++;
    }
  }
}

if (failures > 0) {
  console.error(`Link check failed with ${failures} issue(s).`);
  process.exit(1);
}

console.log('Link check passed.');
