import { readdirSync, readFileSync, existsSync, statSync } from 'node:fs';
import { dirname, join, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = dirname(fileURLToPath(new URL('../package.json', import.meta.url)));
const dist = join(root, 'dist');
const distWithSep = dist.endsWith(sep) ? dist : dist + sep;

// Matches astro.config.mjs `site`. og:image/twitter:image meta content
// values are absolute URLs on this origin; strip it so the remaining path
// can be checked against dist/ like any other internal link.
const SITE_ORIGIN = 'https://www.timothycreekmore.com';

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

// A path is inside dist/ only if it equals dist exactly or starts with
// dist + a trailing separator — a plain startsWith(dist) would let a
// sibling directory like dist-evil/ prefix-match and slip through.
function isInsideDist(absPath) {
  return absPath === dist || absPath.startsWith(distWithSep);
}

// Resolve a clean (fragment/query already stripped) absolute-path URL
// against dist/, returning the concrete file it resolves to, or null if
// it's broken — either because nothing exists there, or because it
// escapes dist/ entirely (e.g. "/../package.json"). Internal links may
// point at either a real file (/resume.pdf) or a route directory served
// via its index.html (/portfolio/ -> .../index.html),
// so both are valid resolutions.
function resolveTarget(cleanPath) {
  if (cleanPath === '' || cleanPath === '/') {
    const p = join(dist, 'index.html');
    return existsSync(p) ? p : null;
  }
  const target = join(dist, cleanPath);
  if (!isInsideDist(target)) return null;
  if (existsSync(target)) {
    if (statSync(target).isDirectory()) {
      const idx = join(target, 'index.html');
      return existsSync(idx) ? idx : null;
    }
    return target;
  }
  const idx = join(target, 'index.html');
  return existsSync(idx) ? idx : null;
}

// Memoized per-file set of `id="..."` values, used to validate fragments
// — both bare same-page fragments (#about) and the fragment half of an
// internal link that carries one (/portfolio/#current-focus).
const idsCache = new Map();
function idsOf(filePath) {
  if (idsCache.has(filePath)) return idsCache.get(filePath);
  const content = readFileSync(filePath, 'utf8');
  const ids = new Set([...content.matchAll(/\bid=["']([^"']+)["']/g)].map((m) => m[1]));
  idsCache.set(filePath, ids);
  return ids;
}

// og:image and twitter:image meta tags carry real, checkable asset paths
// (absolute URLs on this site's own origin) that the href/src regex never
// sees. Extract them from the whole <meta ...> tag so attribute order
// (property/name before or after content) doesn't matter.
function extractMetaImageRefs(html) {
  const refs = [];
  const metaTags = html.match(/<meta\b[^>]*>/gi) ?? [];
  for (const tag of metaTags) {
    if (!/(?:property|name)=["'](?:og:image|twitter:image)["']/i.test(tag)) continue;
    const contentMatch = tag.match(/content=["']([^"']+)["']/i);
    if (!contentMatch) continue;
    let value = contentMatch[1];
    if (value.startsWith(SITE_ORIGIN)) {
      value = value.slice(SITE_ORIGIN.length) || '/';
    }
    refs.push(value);
  }
  return refs;
}

let failures = 0;
let linkCount = 0;
const files = walk(dist);

for (const file of files) {
  const html = readFileSync(file, 'utf8');
  const hrefSrcRefs = [...html.matchAll(/(?:href|src)="([^"]+)"/g)].map((m) => m[1]);
  const refs = [...hrefSrcRefs, ...extractMetaImageRefs(html)];
  // Note: no `srcset` or `poster` attribute appears anywhere in the built
  // output today, so they're intentionally not extracted here.

  for (const ref of refs) {
    if (ref.startsWith('#')) {
      // Bare same-page fragment: verify the id exists in this document.
      const id = ref.slice(1);
      linkCount++;
      if (id && !idsOf(file).has(id)) {
        console.error(`Broken link in ${file.replace(root, '.')}: ${ref} (no element with id="${id}")`);
        failures++;
      }
      continue;
    }

    // Only local, absolute-path references are checkable offline. Skip:
    //   - external links (https://, http://) and protocol-relative (//)
    //     (an og:image/twitter:image value already had this site's own
    //     origin stripped above, so a surviving http(s) URL here is a
    //     genuinely different origin, correctly left unchecked)
    //   - mailto:, tel:, data: URIs (e.g. the inline SVG favicon)
    if (!ref.startsWith('/')) continue;
    if (ref.startsWith('//')) continue;

    linkCount++;

    const withoutHash = ref.split('#')[0];
    const fragment = ref.includes('#') ? ref.slice(ref.indexOf('#') + 1) : null;
    const cleanPath = withoutHash.split('?')[0];

    const target = resolveTarget(cleanPath);
    if (!target) {
      console.error(`Broken link in ${file.replace(root, '.')}: ${ref}`);
      failures++;
      continue;
    }

    if (fragment && target.endsWith('.html')) {
      if (!idsOf(target).has(fragment)) {
        console.error(
          `Broken link in ${file.replace(root, '.')}: ${ref} (${target.replace(root, '.')} has no element with id="${fragment}")`
        );
        failures++;
      }
    }
  }
}

console.log(`Checked ${files.length} pages, ${linkCount} local links.`);
if (failures > 0) {
  console.error(`${failures} broken link(s).`);
  process.exit(1);
}
console.log('All local links resolve.');
