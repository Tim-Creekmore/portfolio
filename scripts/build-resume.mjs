import { existsSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import { dirname, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const root = dirname(fileURLToPath(new URL('../package.json', import.meta.url)));
const source = join(root, 'dist', 'resume', 'index.html');
const output = join(root, 'public', 'resume.pdf');

if (!existsSync(source)) {
  console.error(`Missing ${source}. Run "npx astro build" first.`);
  process.exit(1);
}

const chromePaths = [
  process.env.CHROME_PATH,
  'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
  'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
  'C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe',
  'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
].filter(Boolean);

const browser = chromePaths.find(existsSync);

if (!browser) {
  console.error('Could not find Chrome or Edge. Set CHROME_PATH to override.');
  process.exit(1);
}

const result = spawnSync(browser, [
  '--headless',
  '--disable-gpu',
  '--no-pdf-header-footer',
  `--print-to-pdf=${output}`,
  pathToFileURL(source).href,
], { stdio: 'inherit' });

process.exit(result.status ?? 0);
