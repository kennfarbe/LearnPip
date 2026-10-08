const { test, expect } = require('@playwright/test');
const path = require('node:path');
const { api } = require('../ui/fixtures.cjs');

async function capture(page, name) {
  await page.evaluate(async () => {
    await document.fonts.ready;
    document.activeElement?.blur();
    window.scrollTo(0, 0);
    await new Promise(requestAnimationFrame);
    await new Promise(requestAnimationFrame);
  });
  await page.screenshot({
    path: path.resolve(__dirname, '../../../../docs/screenshots', `${name}.png`),
    fullPage: true,
    animations: 'disabled',
  });
}

test('document the current application with synthetic sample data', async ({ page }) => {
  await api(page);
  await page.goto('/overview');
  await expect(page.getByText('6', { exact: true })).toBeVisible();
  await capture(page, 'overview');

  await page.goto('/questions');
  await page.locator('.draft-list button').first().click();
  await expect(page.getByLabel('Frage', { exact: true })).toHaveValue('Was ergibt 2 + 2?');
  await page.getByText('Sprache und Katalogzuordnung', { exact: true }).click();
  await capture(page, 'question-editor');

  await page.goto('/catalogs');
  await expect(page.getByText('Mathematik', { exact: true })).toBeVisible();
  await page.getByText('Katalog bearbeiten', { exact: true }).click();
  await capture(page, 'catalogs');

  await page.goto('/settings');
  await page.getByText('Lokales Passwort ändern', { exact: true }).click();
  await capture(page, 'admin-password');
  await page.getByLabel('Design', { exact: true }).selectOption('dark');
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/learn');
  await page.getByRole('button', { name: 'Sitzung starten' }).click();
  await page.getByRole('radio', { name: 'Vier', exact: true }).check();
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
  await capture(page, 'learning-mobile-dark');

  // Review the new export surface with synthetic data; the five documentation images remain stable.
  await page.route('**/api/v1/catalog-exports/**', async route => {
    if (new URL(route.request().url()).pathname.endsWith('/questions')) {
      return route.fulfill({ json: { data: [{ id: 'q-export', catalogId: null, prompt: 'Synthetische Bildfrage', subject: 'Biologie', topic: 'Zellen', language: 'de', hasDraft: true }] } });
    }
    return route.fulfill({ json: { data: {
      questionCount: 1, mediaCount: 1, archiveBytes: 4096, previewSha256: 'a'.repeat(64), schemaVersion: '0.2.0', topics: ['Biologie', 'Zellen'],
      licenses: [{ id: 'LicenseRef-Private', holder: 'Testautor', attribution: 'Synthetischer Originalinhalt' }],
      notices: { 'LICENSES.md': 'Private synthetische Testdaten', NOTICE: 'Keine Veröffentlichung', ATTRIBUTION: 'Testautor' },
    } } });
  });
  await page.goto('/questions');
  await page.getByText('Fragen auswählen und exportieren', { exact: true }).click();
  const exported = page.locator('app-catalog-package-export');
  await exported.getByRole('button', { name: 'Alle eigenen Fragen auswählen' }).click();
  await exported.getByLabel('Pakettitel', { exact: true }).fill('Meine lokale Auswahl');
  await exported.getByLabel('Herausgeber / Attributionsname', { exact: true }).fill('Testautor');
  await exported.getByText('Rechte eigener Originaltexte und Originalbilder', { exact: true }).click();
  await exported.getByLabel('Rechteinhaber der Texte', { exact: true }).fill('Testautor');
  await exported.getByLabel('Attribution der Texte', { exact: true }).fill('Synthetischer Originalinhalt');
  await exported.getByLabel('Rechteinhaber der Bilder', { exact: true }).fill('Testautor');
  await exported.getByLabel('Attribution der Bilder', { exact: true }).fill('Synthetisches Originalbild');
  await exported.getByText('Rechte eigener Originaltexte und Originalbilder', { exact: true }).click();
  await exported.getByRole('button', { name: 'Exportvorschau prüfen' }).click();
  await expect(exported.getByRole('heading', { name: 'Exportvorschau', exact: true })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: test.info().outputPath('catalog-export-review.png'), fullPage: true, animations: 'disabled' });

});
