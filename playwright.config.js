import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/visual',
  snapshotDir: './tests/visual/__snapshots__',
  fullyParallel: true,
  reporter: 'list',
  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:3000',
    // Vanta.NET is live WebGL. tailwind.src.css hides #vanta-bg canvas under
    // prefers-reduced-motion, which is what makes these captures stable.
    reducedMotion: 'reduce',
  },
  expect: {
    // Default 5000ms is too tight for full-page screenshots (some pages
    // exceed 2MB) under this machine's default 8-worker fullyParallel
    // concurrency: screenshots were observed timing out mid-capture with
    // no pixel diff (no -actual.png/-diff.png produced, only the
    // baseline), i.e. resource contention, not visual non-determinism.
    // Widened for headroom rather than reducing parallelism, since every
    // later task re-runs this same suite.
    timeout: 15000,
    toHaveScreenshot: {
      maxDiffPixelRatio: 0.01,
      animations: 'disabled',
    },
  },
  projects: [
    {
      name: 'desktop',
      use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 900 } },
    },
    {
      name: 'mobile',
      use: { ...devices['Desktop Chrome'], viewport: { width: 390, height: 844 }, isMobile: false },
    },
  ],
});
