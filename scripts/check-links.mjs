import { readdirSync, readFileSync, existsSync, statSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = dirname(fileURLToPath(new URL('../package.json', import.meta.url)));
const dist = join(root, 'dist');

if (!existsSync(dist)) {
  console.error('No dist/ directory. Run "npx astro build" first.');
  process.exit(1);
}

function walk(dir) {
  const out = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) out.push(...walk(full));
    else if (entry.name.endsWith('.html')) out.push(full);
  }
  return out;
}

// Resolve an absolute-path URL (fragment/query already stripped) against
// dist/. Internal links may point at either a real file (/resume.pdf,
// /MAEresults.png) or a route directory served via its index.html
// (/portfolio/ -> dist/portfolio/index.html), so both are valid resolutions.
function resolves(urlPath) {
  const clean = urlPath.split('#')[0].split('?')[0];
  if (clean === '' || clean === '/') return existsSync(join(dist, 'index.html'));
  const target = join(dist, clean);
  if (existsSync(target)) {
    return statSync(target).isDirectory()
      ? existsSync(join(target, 'index.html'))
      : true;
  }
  return existsSync(join(dist, clean, 'index.html'));
}

let failures = 0;
let linkCount = 0;
const files = walk(dist);

for (const file of files) {
  const html = readFileSync(file, 'utf8');
  const refs = [...html.matchAll(/(?:href|src)="([^"]+)"/g)].map((m) => m[1]);
  for (const ref of refs) {
    // Only local, absolute-path references are checkable offline. Skip:
    //   - external links (https://, http://) and protocol-relative (//)
    //   - mailto:, tel:, data: URIs (e.g. the inline SVG favicon)
    //   - bare same-page fragments (#about) — these aren't path references
    //     and resolving them would mean parsing every document's ids just
    //     to validate a same-page anchor, which is out of scope for a
    //     link-existence checker. Skipped, not "assumed valid".
    if (!ref.startsWith('/')) continue;
    if (ref.startsWith('//')) continue;

    linkCount++;
    if (!resolves(ref)) {
      console.error(`Broken link in ${file.replace(root, '.')}: ${ref}`);
      failures++;
    }
  }
}

console.log(`Checked ${files.length} pages, ${linkCount} local links.`);
if (failures > 0) {
  console.error(`${failures} broken link(s).`);
  process.exit(1);
}
console.log('All local links resolve.');
