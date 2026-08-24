#!/usr/bin/env node
/**
 * Minimal static file server for capturing/checking visual-regression
 * screenshots against the static site or the Astro `dist/` build.
 *
 * Usage:
 *   node scripts/static-server.mjs <root> <port>
 *   node scripts/static-server.mjs . 3000
 *   node scripts/static-server.mjs dist 3000
 *
 * Deliberately minimal (node:http/node:fs/node:path only, no deps):
 *   - A request for a directory serves that directory's index.html.
 *   - `.html` URLs are served exactly as requested — never rewritten,
 *     never redirected, never stripped. This is the property the
 *     `npx serve` based harness got wrong (see task-1-report.md).
 *   - Anything that resolves outside `root` is refused.
 *   - Malformed request URIs and any other per-request error return an
 *     HTTP error response instead of crashing the process — this server
 *     backs `serve:dist` for later tasks under 8-way parallel workers,
 *     and one bad request must not take down every worker at once.
 */

import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';

const [, , rootArg, portArg] = process.argv;

if (!rootArg || !portArg) {
  console.error('Usage: node scripts/static-server.mjs <root> <port>');
  process.exit(1);
}

const root = path.resolve(process.cwd(), rootArg);
const port = Number(portArg);

if (!fs.existsSync(root) || !fs.statSync(root).isDirectory()) {
  console.error(`Root directory does not exist or is not a directory: ${root}`);
  process.exit(1);
}

const MIME_TYPES = {
  '.html': 'text/html; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.mjs': 'text/javascript; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.xml': 'application/xml; charset=utf-8',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.gif': 'image/gif',
  '.webp': 'image/webp',
  '.ico': 'image/x-icon',
  '.pdf': 'application/pdf',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2',
  '.ttf': 'font/ttf',
  '.txt': 'text/plain; charset=utf-8',
};

function contentTypeFor(filePath) {
  return MIME_TYPES[path.extname(filePath).toLowerCase()] || 'application/octet-stream';
}

/**
 * Resolve a request path to an absolute file path under `root`, or
 * null if nothing servable exists / the resolved path escapes root /
 * the URI can't be decoded at all (e.g. a bare `%`).
 *
 * Directory-style URLs (`/portfolio/` or `/portfolio`) map to
 * `<dir>/index.html`. Anything with an explicit extension (notably
 * `.html`) is served exactly as named — no rewriting.
 */
function resolveRequestPath(urlPath) {
  const rawPath = urlPath.split('?')[0].split('#')[0];

  let decoded;
  try {
    decoded = decodeURIComponent(rawPath);
  } catch {
    // Malformed URI (e.g. a bare "%"). Not servable — 404, not a crash.
    return null;
  }

  const relative = decoded.replace(/^\/+/, '');
  let candidate = path.join(root, relative);

  // Path traversal guard: the resolved path must stay under root.
  const resolved = path.resolve(candidate);
  const rootWithSep = root.endsWith(path.sep) ? root : root + path.sep;
  if (resolved !== root && !resolved.startsWith(rootWithSep)) {
    return null;
  }

  let stat;
  try {
    stat = fs.statSync(resolved);
  } catch {
    return null;
  }

  if (stat.isDirectory()) {
    const indexPath = path.join(resolved, 'index.html');
    if (fs.existsSync(indexPath) && fs.statSync(indexPath).isFile()) {
      return indexPath;
    }
    return null;
  }

  if (stat.isFile()) {
    return resolved;
  }

  return null;
}

const server = http.createServer((req, res) => {
  try {
    const filePath = resolveRequestPath(req.url || '/');

    if (!filePath) {
      res.writeHead(404, { 'Content-Type': 'text/plain; charset=utf-8' });
      res.end(`404 Not Found: ${req.url}`);
      return;
    }

    fs.readFile(filePath, (err, data) => {
      try {
        if (err) {
          res.writeHead(404, { 'Content-Type': 'text/plain; charset=utf-8' });
          res.end(`404 Not Found: ${req.url}`);
          return;
        }
        res.writeHead(200, { 'Content-Type': contentTypeFor(filePath) });
        res.end(data);
      } catch (innerErr) {
        console.error('static-server: error writing response', innerErr);
        if (!res.headersSent) {
          res.writeHead(500, { 'Content-Type': 'text/plain; charset=utf-8' });
        }
        res.end('500 Internal Server Error');
      }
    });
  } catch (err) {
    // Defensive: any unexpected throw returns 500 instead of taking down
    // the process (and every other worker mid-run) with it.
    console.error('static-server: unhandled request error', err);
    if (!res.headersSent) {
      res.writeHead(500, { 'Content-Type': 'text/plain; charset=utf-8' });
    }
    res.end('500 Internal Server Error');
  }
});

server.listen(port, () => {
  console.log(`Serving ${root} at http://localhost:${port}/`);
});
