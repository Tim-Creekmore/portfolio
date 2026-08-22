import { existsSync } from 'node:fs';
import { spawnSync } from 'node:child_process';

const chromePaths = [
  'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
  'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
  'C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe',
  'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe'
];

const browser = chromePaths.find(existsSync);

if (!browser) {
  console.error('Could not find Chrome or Edge for PDF export.');
  process.exit(1);
}

const result = spawnSync(browser, [
  '--headless',
  '--disable-gpu',
  '--no-pdf-header-footer',
  '--print-to-pdf=resume.pdf',
  'file:///C:/Users/timcr/repos/TimsHouse/resume.html'
], { stdio: 'inherit' });

process.exit(result.status ?? 0);
