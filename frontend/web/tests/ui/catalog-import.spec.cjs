const { test, expect } = require('@playwright/test');
const { api } = require('./fixtures.cjs');
const preview = {
  title: 'Synthetisches Fragenpaket', catalogVersion: '1.0.0', schemaVersion: '0.1.0',
  sourceRevision: 'synthetic-1', language: 'de-DE', questionCount: 1, mediaCount: 1,
  license: { id: 'CC0-1.0', holder: 'Testautor', attribution: 'Synthetischer Test' },
  questionLicenses: [{ id: 'CC-BY-4.0', holder: 'Bildautor', attribution: 'Testbild' }],
  notices: { 'LICENSES.md': 'Synthetischer Lizenztext', NOTICE: 'Keine echten Fremdinhalte', ATTRIBUTION: 'Testautor' },
  topics: ['Technischer Test'], archiveSha256: 'a'.repeat(64), state: 'new', catalogId: null,
};
async function setup(page, state = 'new') {
  await api(page);
  const imports = [];
  let count = 0;
  await page.route('**/api/v1/catalog-packages/**', async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith('/preview')) return route.fulfill({ json: { data: { ...preview, state } } });
    if (path.endsWith('/import')) {
      count++;
      expect(route.request().postDataBuffer().toString()).toContain('a'.repeat(64));
      imports.push({ id: 'import-1', packageId: 'synthetic.test', catalogVersion: '1.0.0' });
      return route.fulfill({ status: 201, json: { data: { catalogId: 'catalog-import-1', alreadyImported: false } } });
    }
    await route.fulfill({ json: { data: imports } });
  });
  await page.goto('/catalogs');
  await page.getByLabel('LearnPip-Paket (.zip, maximal 25 MiB)').setInputFiles({ name: 'synthetic.zip', mimeType: 'application/zip', buffer: Buffer.from('synthetic-upload') });
  await page.getByRole('button', { name: 'Importvorschau prüfen' }).click();
  await expect(page.getByRole('heading', { name: preview.title })).toBeVisible();
  return () => count;
}

test('requires a reviewed preview and consent before private import', async ({ page }) => {
  const count = await setup(page);
  await expect(page.getByRole('button', { name: 'Import bestätigen' })).toBeDisabled();
  await page.getByText('Lizenzen, Quellen und Attribution prüfen', { exact: true }).click();
  await expect(page.getByText('Synthetischer Lizenztext', { exact: true })).toBeVisible();
  await page.getByRole('checkbox', { name: 'Ich habe die Lizenzangaben geprüft und darf diese Inhalte privat nutzen.' }).check();
  await page.getByRole('button', { name: 'Import bestätigen' }).click();
  await expect(page.getByText('Fragenpaket privat importiert. Es wurde nichts veröffentlicht.', { exact: true })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Importierte Fragen anzeigen' })).toHaveAttribute('href', '/questions?catalog=catalog-import-1');
  expect(count()).toBe(1);
  await expect(page.getByRole('button', { name: 'Import bestätigen' })).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Originalpaket herunterladen' })).toBeVisible();
});

for (const state of ['identical', 'conflict']) {
  test(`prevents importing a ${state} package`, async ({ page }) => {
    const count = await setup(page, state);
    await expect(page.getByRole('button', { name: 'Import bestätigen' })).toHaveCount(0);
    expect(count()).toBe(0);
  });
}

test('changing the file clears prior preview and consent on a phone', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await setup(page);
  await page.getByRole('checkbox', { name: 'Ich habe die Lizenzangaben geprüft und darf diese Inhalte privat nutzen.' }).check();
  await page.getByLabel('LearnPip-Paket (.zip, maximal 25 MiB)').setInputFiles({ name: 'other.zip', mimeType: 'application/zip', buffer: Buffer.from('changed') });
  await expect(page.getByRole('heading', { name: preview.title })).toHaveCount(0);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});
