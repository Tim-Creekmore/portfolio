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
    // Zero tolerance, deliberately. With Vanta suppressed (see
    // tests/visual/pages.spec.js), captures on this toolchain are exactly
    // pixel-reproducible: the same page served twice (static-vs-static,
    // and separately astro-vs-astro) diffs to 0 pixels every time across
    // repeated runs. Full detail, including a caveat about comparing a
    // static baseline against a *different* build (astro) of a
    // gradient-heavy page — cross-build sub-pixel dithering can appear
    // there even with no real content change — is in
    // .superpowers/sdd/2026-08-21-astro-consolidation/harness-repair-report.md.
    // That caveat does not affect this setting's validity: every page's
    // *ongoing* baseline comparison (either astro-vs-static for an
    // unmodified port, or astro-vs-astro once a page has an intentional
    // change and its baseline is recaptured from the astro build) has
    // been verified stable at these exact values.
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
