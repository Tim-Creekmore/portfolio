import { test, expect } from '@playwright/test';

// Regression test for the crash fixed in fix round 2: an unguarded
// decodeURIComponent() on a malformed request URI (e.g. a bare "%")
// used to throw uncaught inside the request handler and kill the
// whole node process. Under `fullyParallel` that takes down every
// worker sharing this server mid-run, so this must never regress
// silently — assert both that the bad request gets a clean 404 *and*
// that the server is still serving normal requests immediately after.
test('malformed request URI returns 404 and does not crash the server', async ({ request, baseURL }) => {
  const malformed = await request.get(`${baseURL}/%`, { failOnStatusCode: false });
  expect(malformed.status()).toBe(404);

  // The server must still be alive and correct for the next request.
  const followUp = await request.get(`${baseURL}/resume/`, { failOnStatusCode: false });
  expect(followUp.status()).toBe(200);
  expect(await followUp.text()).toContain('<title>Timothy Creekmore Resume');
});
