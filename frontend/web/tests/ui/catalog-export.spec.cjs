const { test, expect } = require('@playwright/test');
const AxeBuilder = require('@axe-core/playwright').default;
const { api } = require('./fixtures.cjs');
const candidates = [
  { id: 'question-1', catalogId: 'catalog-1', prompt: 'Pflanzenzelle mit Bild', subject: 'Biologie', topic: 'Zellen', language: 'de', hasDraft: true, audience: 'Klasse 9', difficulty: 'easy', tags: ['Biologie / Zellen'] },
  { id: 'question-2', catalogId: null, prompt: 'Tierische Zelle', subject: 'Biologie', topic: 'Zellen', language: 'en', hasDraft: false },
  { id: 'question-3', catalogId: 'catalog-1', prompt: 'Addition', subject: 'Mathematik', topic: 'Grundlagen', language: 'de', hasDraft: true },
];
const preview = {
  questionCount: 1, mediaCount: 2, archiveBytes: 2048, previewSha256: 'b'.repeat(64), schemaVersion: '0.2.0', topics: ['Biologie', 'Zellen'],
  licenses: [{ id: 'LicenseRef-Private', holder: 'Testautor', attribution: 'Synthetische Originalfrage' }],
  notices: { 'LICENSES.md': 'Private synthetische Frage', NOTICE: 'Keine Veröffentlichung', ATTRIBUTION: 'Testautor' },
};
async function setup(page, downloadStatus = 200) {
  await api(page);
  const calls = [];
  await page.route('**/api/v1/catalog-exports/**', async route => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith('/questions')) return route.fulfill({ json: { data: candidates } });
    const input = route.request().postDataJSON();
    calls.push({ path, input });
    if (path.endsWith('/preview')) return route.fulfill({ json: { data: preview } });
    if (downloadStatus !== 200) return route.fulfill({ status: downloadStatus, json: { message: 'Inhalt geändert. Bitte erneut prüfen.' } });
    return route.fulfill({ status: 200, headers: { 'Content-Type': 'application/zip', 'Content-Disposition': 'attachment; filename="learnpip-selection.zip"' }, body: Buffer.from('synthetic-zip') });
  });
  await page.goto('/questions');
  await page.getByText('Fragen auswählen und exportieren', { exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Eigene Fragen als Paket exportieren' })).toBeVisible();
  return calls;
}
async function select(page) {
  const section = page.locator('app-catalog-package-export');
  await section.getByLabel('Fach', { exact: true }).fill('Biologie');
  await section.getByLabel('Sprache', { exact: true }).fill('de');
  await section.getByLabel('Klasse / Zielgruppe', { exact: true }).fill('Klasse 9');
  await section.getByLabel('Schlagwort / Themenhierarchie', { exact: true }).fill('Biologie / Zellen');
  await section.getByRole('combobox', { name: 'Schwierigkeitsgrad', exact: true }).selectOption('easy');
  await section.getByRole('button', { name: 'Gefilterte Fragen auswählen' }).click();
  await section.getByLabel('Pakettitel', { exact: true }).fill('Meine Auswahl');
  await section.getByLabel('Herausgeber / Attributionsname', { exact: true }).fill('Testautor');
  await section.getByRole('button', { name: 'Exportvorschau prüfen' }).click();
  await expect(section.getByRole('heading', { name: 'Exportvorschau', exact: true })).toBeVisible();
  return section;
}

test('exports only a combined filter selection after preview and explicit consent', async ({ page }) => {
  const calls = await setup(page);
  const section = await select(page);
  const download = section.getByRole('button', { name: 'LearnPip-Paket (.zip) herunterladen' });
  await expect(download).toBeDisabled();
  expect(calls[0].input.questionIds).toEqual(['question-1']);
  expect(calls[0].input.rightsConfirmed).toBe(false);
  await section.getByText('Lizenzen, Quellen und Attribution prüfen', { exact: true }).click();
  await expect(section.getByText('Private synthetische Frage', { exact: true })).toBeVisible();
  await section.getByRole('checkbox', { name: /^Ich bestätige die Exportrechte/ }).check();
  const downloaded = page.waitForEvent('download');
  await download.click();
  expect((await downloaded).suggestedFilename()).toBe('learnpip-selection.zip');
  expect(calls[1].input.previewSha256).toBe('b'.repeat(64));
  expect(calls[1].input.rightsConfirmed).toBe(true);
  expect(calls[1].input.questionIds).toEqual(['question-1']);
});

test('selection or metadata changes clear preview and consent', async ({ page }) => {
  const calls = await setup(page);
  const section = await select(page);
  await section.getByRole('checkbox', { name: /^Ich bestätige die Exportrechte/ }).check();
  await section.getByLabel('Pakettitel', { exact: true }).fill('Neue Auswahl');
  await expect(section.getByRole('button', { name: 'LearnPip-Paket (.zip) herunterladen' })).toHaveCount(0);
  await section.getByRole('button', { name: 'Alle eigenen Fragen auswählen' }).click();
  await section.getByRole('button', { name: 'Exportvorschau prüfen' }).click();
  expect(calls[1].input.questionIds).toEqual(['question-1', 'question-2', 'question-3']);
  await expect(section.getByRole('checkbox', { name: /^Ich bestätige die Exportrechte/ })).not.toBeChecked();
  await section.getByRole('checkbox', { name: /Pflanzenzelle/ }).uncheck();
  await expect(section.getByRole('heading', { name: 'Exportvorschau', exact: true })).toHaveCount(0);
});

test('stale previews require a fresh confirmation and stay accessible on a dark phone', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.emulateMedia({ colorScheme: 'dark' });
  await setup(page, 409);
  const section = await select(page);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  expect((await new AxeBuilder({ page }).include('app-catalog-package-export').analyze()).violations).toEqual([]);
  await section.getByRole('checkbox', { name: /^Ich bestätige die Exportrechte/ }).check();
  await section.getByRole('button', { name: 'LearnPip-Paket (.zip) herunterladen' }).click();
  await expect(section.getByText('Inhalt geändert. Bitte erneut prüfen.', { exact: true })).toBeVisible();
  await expect(section.getByRole('button', { name: 'LearnPip-Paket (.zip) herunterladen' })).toHaveCount(0);
});
