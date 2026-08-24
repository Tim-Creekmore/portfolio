import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/visual',
  snapshotDir: './tests/visual/__snapshots__',
  fullyParallel: true,
  reporter: 'list',
  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:3000',
    // NOTE: this does NOT suppress Vanta.NET's live WebGL background.
    // Verified: `window.matchMedia('(prefers-reduced-motion: reduce)')
    // .matches` still reports `false` inside the page with this option
    // set (Playwright 1.62.1), so hub-nav.js's own reduced-motion check
    // never trips. Kept for its actual effect (CSS/JS `prefers-reduced-motion`
    // media-query-independent animation suppression Playwright performs
    // itself), but Vanta suppression is handled separately in
    // tests/visual/pages.spec.js via route interception + a matchMedia
    // override in an addInitScript — see the comment there.
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
    // Zero tolerance, deliberately. Vanta.NET's live WebGL background renders
    // into every screenshot because Playwright's `reducedMotion: 'reduce'`
    // never actually suppresses it—verified: window.matchMedia still reports
    // false. During migration, a loose threshold masked this; a navigation bar
    // covering 7% of pixels passed as unchanged. Zero is achievable because
    // test.beforeEach (pages.spec.js) suppresses Vanta via two CDN request
    // aborts (three.js, vanta.net) and window.matchMedia override—these are
    // a pair; weakening one requires reconsidering the other. Caveat: a static
    // baseline vs. a differently-built astro output can surface sub-pixel
    // dithering, but same-build runs are deterministic. Before loosening,
    // re-verify with repeated identical runs.
    toHaveScreenshot: {
      threshold: 0,
      maxDiffPixelRatio: 0,
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
