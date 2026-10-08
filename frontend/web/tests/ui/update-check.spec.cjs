const { test, expect } = require('@playwright/test');
const AxeBuilder = require('@axe-core/playwright').default;
const { api } = require('./fixtures.cjs');
const oldStatus = () => ({ installedVersion: 'v1.14.0', latestVersion: 'v1.14.0', state: 'current', interval: 'daily', lastCheckedAtUtc: '2026-10-07T18:00:58Z', nextCheckAtUtc: '2026-10-08T18:00:58Z', error: null, release: null, job: null });
const newStatus = () => ({ ...oldStatus(), latestVersion: 'v1.15.0', state: 'update_available', lastCheckedAtUtc: '2026-10-08T13:00:00Z', release: { version: 'v1.15.0', name: 'LearnPip v1.15.0', notes: 'Synthetisch', url: 'https://github.com/kennfarbe/LearnPip/releases/tag/v1.15.0', publishedAtUtc: '2026-10-08T06:44:30Z' } });
async function setup(page) {
  await api(page, { capabilities: { administration: true, moderation: true } });
  await page.route('**/api/v1/admin/updates', route => route.fulfill({ json: oldStatus() }));
  await page.goto('/administration');
  await expect(page.getByRole('heading', { name: 'LearnPip-Updates', exact: true })).toBeVisible();
}

for (const code of [401, 403, 429, 500]) {
  test(`manual update check reports HTTP ${code} instead of silently claiming current`, async ({ page }) => {
    await setup(page);
    await page.route('**/api/v1/admin/updates/check', route => route.fulfill({ status: code }));
    await page.getByRole('button', { name: 'Jetzt nach Updates suchen', exact: true }).click();
    const update = page.locator('app-admin-updates');
    await expect(update.getByRole('alert')).toContainText(code === 401 || code === 403 ? 'Administrator-Anmeldung' : code === 429 ? 'Zu viele' : 'fehlgeschlagen');
    await expect(update.locator('.badge')).toHaveText('Prüfung fehlgeschlagen');
    await expect(update.getByRole('button', { name: 'Jetzt nach Updates suchen', exact: true })).toBeEnabled();
    if (code === 401 || code === 403) await expect(update.getByRole('link', { name: 'Zur Administrator-Anmeldung' })).toHaveAttribute('href', '/settings');
    await page.route('**/api/v1/admin/updates/check', route => route.fulfill({ json: newStatus() }));
    await update.getByRole('button', { name: 'Jetzt nach Updates suchen', exact: true }).click();
    await expect(update.getByRole('alert')).toHaveCount(0);
    await expect(update.locator('.badge')).toHaveText('Update verfügbar');
    await expect(update.getByRole('link', { name: 'Release Notes anzeigen' })).toHaveAttribute('href', newStatus().release.url);
  });
}

test('network failure leaves an actionable error and resets the button on mobile', async ({ page }) => {
  await setup(page);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.route('**/api/v1/admin/updates/check', route => route.abort('failed'));
  await page.getByRole('button', { name: 'Jetzt nach Updates suchen', exact: true }).click();
  const update = page.locator('app-admin-updates');
  await expect(update.getByRole('alert')).toContainText('Verbindung');
  await expect(update.getByRole('button', { name: 'Jetzt nach Updates suchen', exact: true })).toBeEnabled();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  expect((await new AxeBuilder({ page }).include('app-admin-updates').analyze()).violations).toEqual([]);
  await update.screenshot({ path: test.info().outputPath('update-error-mobile.png') });
});

test('an in-flight manual check visibly waits and replaces the cached version when it completes', async ({ page }) => {
  await setup(page);
  let finish;
  await page.route('**/api/v1/admin/updates/check', async route => {
    await new Promise(resolve => { finish = resolve; });
    await route.fulfill({ json: newStatus() });
  });
  await page.getByRole('button', { name: 'Jetzt nach Updates suchen', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Updates werden geprüft …', exact: true })).toBeDisabled();
  await expect(page.locator('app-admin-updates').getByRole('status')).toContainText('Releaseinformationen');
  await expect.poll(() => typeof finish).toBe('function');
  finish();
  await expect(page.locator('app-admin-updates .badge')).toHaveText('Update verfügbar');
  await expect(page.getByRole('button', { name: 'Jetzt nach Updates suchen', exact: true })).toBeEnabled();
});
