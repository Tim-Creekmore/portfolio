import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/visual',
  snapshotDir: './tests/visual/__snapshots__',
  fullyParallel: true,
  reporter: 'list',
  use: {
    // There is no `webServer` block: this suite expects a server already
    // running at baseURL. Build, then `npm run serve:dist`, then `npm run
    // visual`. See the "Visual regression suite" section of README.md.
    baseURL: process.env.BASE_URL || 'http://localhost:3000',
    // Emulates `prefers-reduced-motion: reduce` for CSS media queries, which
    // is what the site's own reduced-motion rule keys off. Note this does not
    // change what `window.matchMedia` reports to page scripts (verified on
    // Playwright 1.62.1) — no current page script asks, so nothing depends on
    // it, but a future one would need its own handling.
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
    // Zero tolerance, deliberately. During the migration a loose threshold
    // masked a real regression: a navigation bar covering 7% of the pixels
    // passed as unchanged. Zero is achievable because the site ships no
    // animation and no live background — the Vanta.NET/WebGL hero that once
    // forced a loose threshold was deleted in the redesign.
    //
    // The cost of zero tolerance is that it also catches nondeterminism, and
    // reads it as a regression. Treat an unexplained failure as a real
    // question about the page, not as a threshold that needs loosening: the
    // home page's Field Notes list once reordered between builds because
    // every note shared a pubDate and the sort had no tiebreak, and this
    // suite is what surfaced it. Caveat: a static baseline compared against a
    // differently-built Astro output can surface sub-pixel dithering, but
    // same-build runs are deterministic. Before loosening, re-verify with
    // repeated identical runs.
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
