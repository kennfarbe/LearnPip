const { test, expect } = require('@playwright/test');
const AxeBuilder = require('@axe-core/playwright').default;
const { api } = require('./fixtures.cjs');
const notices = { 'LICENSES.md': 'CC BY-SA 4.0 – synthetischer Inhalt', NOTICE: 'Keine Kontodaten', ATTRIBUTION: 'Testautor' };
const item = { id: 'package-1', packageId: 'synthetic.package', catalogVersion: '2', archiveSha256: 'a'.repeat(64), available: true, title: 'Synthetisches Lernpaket', description: 'Nur Testinhalte', language: 'de', sourceRevision: 'revision-2', questionCount: 5, archiveBytes: 4096, expandedBytes: 8192, notices };

test('administrator explicitly confirms package installation and separately confirms removal', async ({ page }) => {
  await api(page, { capabilities: { administration: true, moderation: true } });
  const calls = [];
  await page.route('**/api/v1/instance-catalogs/**', async route => {
    const path = new URL(route.request().url()).pathname;
    const method = route.request().method();
    if (path.endsWith('/sources/')) return route.fulfill({ json: { data: [] } });
    if (method === 'GET') return route.fulfill({ json: { data: [item] } });
    calls.push({ path, method, body: route.request().postData() });
    if (path.endsWith('/preview')) return route.fulfill({ json: { data: { package: item, previousId: null, previousVersion: null, previousSha256: null, newQuestions: 5, removedQuestions: 0, sharedQuestions: 0 } } });
    return route.fulfill({ status: 204 });
  });
  await page.goto('/administration');
  const section = page.locator('app-instance-catalog-library');
  await section.locator('input[type=file]').setInputFiles({ name: 'synthetic.zip', mimeType: 'application/zip', buffer: Buffer.from('fixture') });
  await section.getByRole('button', { name: 'Paketfassung und Änderungen prüfen' }).click();
  const install = section.getByRole('button', { name: 'Paketfassung bereitstellen' });
  await expect(install).toBeDisabled();
  expect(calls).toHaveLength(1);
  await section.getByRole('checkbox', { name: /^Ich habe Inhalte, Lizenz-/ }).check();
  await install.click();
  await expect(section.getByRole('status')).toContainText('Paketfassung bereitgestellt');
  expect(calls[1].body).toContain('name="archiveSha256"');
  expect(calls[1].body).toContain('a'.repeat(64));
  const remove = section.getByRole('button', { name: 'Paketfassung endgültig entfernen' });
  await expect(remove).toBeDisabled();
  await section.getByRole('checkbox', { name: /^Ich bestätige die endgültige Entfernung/ }).check();
  await remove.click();
  await expect(section.getByRole('status')).toContainText('Instanzdatei entfernt');
  expect(calls[2].method).toBe('DELETE');
  expect(JSON.parse(calls[2].body).confirmed).toBe(true);
});

test('content rights bind each image and the confirmation to the loaded content', async ({ page }) => {
  await api(page);
  const saved = [];
  await page.route('**/api/v1/catalog-exports/questions', route => route.fulfill({ json: { data: [{ id: 'question-1', prompt: 'Synthetische Frage' }] } }));
  await page.route('**/api/v1/catalog-rights/**', async route => {
    if (route.request().method() === 'PUT') { saved.push(route.request().postDataJSON()); return route.fulfill({ json: { data: { saved: true } } }); }
    return route.fulfill({ json: { data: { contentSha256: 'b'.repeat(64), stale: true, source: 'Eigenes Original', license: 'LicenseRef-Private', rights: null, media: [{ id: '11111111-1111-1111-1111-111111111111', altText: 'Synthetisches Diagramm' }] } } });
  });
  await page.goto('/questions');
  await page.getByText('Quellen und Rechte je Frage und Bild', { exact: true }).click();
  const section = page.locator('app-catalog-content-rights');
  await section.getByRole('combobox', { name: /^Frage/ }).selectOption('question-1');
  await expect(section.getByRole('alert')).toBeVisible();
  const fields = section.locator('fieldset fieldset');
  for (const index of [0, 1]) {
    await fields.nth(index).getByLabel('Rechteinhaber', { exact: true }).fill(index ? 'Bildautor' : 'Fragenautor');
    await fields.nth(index).getByLabel('Attribution / Autorenangabe').fill(index ? 'Eigene Zeichnung' : 'Eigene Frage');
  }
  const save = section.getByRole('button', { name: 'Rechteangaben speichern' });
  await expect(save).toBeDisabled();
  const consent = section.getByRole('checkbox', { name: /^Ich habe Inhalt, Quellen/ });
  await consent.check();
  await expect(save).toBeEnabled();
  await section.getByLabel('Klasse / Zielgruppe des Inhalts', { exact: true }).fill('Klasse 9');
  await expect(consent).not.toBeChecked();
  await expect(save).toBeDisabled();
  await consent.check();
  await expect(save).toBeEnabled();
  await save.click();
  await expect(section.getByRole('status')).toContainText('Rechteangaben für diese Fassung gespeichert');
  expect(saved[0].contentSha256).toBe('b'.repeat(64));
  expect(saved[0].rights.media['11111111-1111-1111-1111-111111111111'].license.holder).toBe('Bildautor');
  expect(saved[0].rights.license.holder).toBe('Fragenautor');
  expect(saved[0].rights.metadata.age_band).toBe('Klasse 9');
  await expect(section.getByRole('alert')).toHaveCount(0);
  await expect(section.getByRole('checkbox')).not.toBeChecked();
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  expect((await new AxeBuilder({ page }).include('app-catalog-content-rights').analyze()).violations).toEqual([]);
});

test('private update requires licence consent and a version-specific confirmation', async ({ page }) => {
  await api(page);
  const calls = [];
  await page.route('**/api/v1/catalog-packages/**', async route => {
    if (route.request().method() === 'GET') return route.fulfill({ json: { data: [] } });
    calls.push(route.request().postData());
    if (new URL(route.request().url()).pathname.endsWith('/preview')) return route.fulfill({ json: { data: { title: 'Updatepaket', catalogVersion: '2', schemaVersion: '0.2.0', sourceRevision: '2', language: 'de', questionCount: 5, mediaCount: 0, archiveBytes: 1024, expandedBytes: 1024, topics: ['Technik'], license: { id: 'CC-BY-SA-4.0', holder: 'Testautor', attribution: 'Synthetisch' }, questionLicenses: [], notices, archiveSha256: 'c'.repeat(64), state: 'conflict', catalogId: 'catalog-1', canUpdate: true, previousFingerprint: 'd'.repeat(64) } } });
    return route.fulfill({ json: { data: { catalogId: 'catalog-1' } } });
  });
  await page.goto('/catalogs');
  const section = page.locator('app-catalog-package-import');
  await section.locator('input[type=file]').setInputFiles({ name: 'synthetic.zip', mimeType: 'application/zip', buffer: Buffer.from('fixture') });
  await section.getByRole('button', { name: 'Importvorschau prüfen' }).click();
  const install = section.getByRole('button', { name: 'Import bestätigen' });
  await section.getByRole('checkbox', { name: /^Ich habe die Lizenzangaben/ }).check();
  await expect(install).toBeDisabled();
  await section.getByRole('checkbox', { name: /^Ich bestätige den Wechsel/ }).check();
  await install.click();
  await expect(section.getByText('Fragenpaket privat importiert.', { exact: false })).toBeVisible();
  expect(calls[1]).toContain('name="updateConfirmed"');
  expect(calls[1]).toContain('d'.repeat(64));
  expect(calls[1]).toContain('c'.repeat(64));
});
