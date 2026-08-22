import { spawnSync } from 'node:child_process';
import { readFileSync, existsSync, unlinkSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { tmpdir } from 'node:os';
import { randomUUID } from 'node:crypto';

const root = dirname(fileURLToPath(new URL('../package.json', import.meta.url)));
const source = join(root, 'src', 'styles', 'tailwind.src.css');
const committed = join(root, 'public', 'assets', 'css', 'tailwind.css');

// Write the freshly-generated CSS to a private temp file — never to the
// committed path — so a failure partway through can never leave the real
// public/assets/css/tailwind.css modified.
const tempOut = join(tmpdir(), `tailwind-check-${process.pid}-${randomUUID()}.css`);

const tailwindBin = join(
  root,
  'node_modules',
  '.bin',
  process.platform === 'win32' ? 'tailwindcss.cmd' : 'tailwindcss'
);

function run() {
  // shell: true is required on Windows because .bin/tailwindcss.cmd is a
  // batch shim, not a directly-spawnable executable; it's harmless on POSIX.
  // No argument here contains shell metacharacters, so this is safe.
  const result = spawnSync(tailwindBin, ['-i', source, '-o', tempOut, '--minify'], {
    cwd: root,
    encoding: 'utf8',
    shell: true
  });

  if (result.status !== 0) {
    console.error('Failed to run tailwindcss to regenerate CSS for comparison.');
    if (result.stderr) console.error(result.stderr);
    return 1;
  }

  if (!existsSync(committed)) {
    console.error(`Missing committed stylesheet: ${committed}`);
    return 1;
  }

  if (!existsSync(tempOut)) {
    console.error('tailwindcss did not produce output to compare.');
    return 1;
  }

  const fresh = readFileSync(tempOut);
  const current = readFileSync(committed);

  if (!fresh.equals(current)) {
    console.error('Committed public/assets/css/tailwind.css is stale.');
    console.error(
      'The Tailwind content scan of the current source no longer matches the committed CSS.'
    );
    console.error('Run "npm run build:css" and commit the result.');
    return 1;
  }

  console.log('public/assets/css/tailwind.css matches a fresh build:css output.');
  return 0;
}

let exitCode = 1;
try {
  exitCode = run();
} finally {
  if (existsSync(tempOut)) {
    unlinkSync(tempOut);
  }
}

process.exit(exitCode);
