const { test, expect } = require('@playwright/test');
const { api } = require('./fixtures.cjs');
const AxeBuilder = require('@axe-core/playwright').default;
const keys = ['create','readOwn','editOwn','deleteOwn','readShared','readForeign','readPrivate','editForeign','deleteForeign','approve','withdraw','reports','batch','import','export','community'];
const defaults = role => Object.fromEntries(keys.map(key => [key, role === 'moderator' || ['create','readOwn','editOwn','deleteOwn','readShared','import','export','community'].includes(key)]));

test('private permissions need preview and separate confirmations and can be reset on mobile', async ({ page }) => {
  await api(page, { capabilities: { administration: true, moderation: true } });
  const writes = [];
  await page.route('**/api/v1/admin/question-permissions/**', async route => {
    if (route.request().method() === 'PUT') {
      writes.push(route.request().postDataJSON());
      return route.fulfill({ status: 204 });
    }
    return route.fulfill({ json: { data: { roles: ['user','moderator'].map(role => ({ role, value: JSON.stringify(defaults(role)), rights: defaults(role), defaults: defaults(role) })) } } });
  });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/administration');
  await page.locator('summary').filter({ hasText: 'Benutzer und Rollen: Fragenberechtigungen' }).click();
  const settings = page.locator('app-question-permissions');
  const user = settings.getByRole('group', { name: 'Benutzer', exact: true });
  await user.getByLabel('Fremde private und Gruppenfragen im Moderationsbereich lesen', { exact: true }).check();
  await user.getByLabel('Fremde Fragen im Moderationsbereich lesen', { exact: true }).check();
  await settings.getByRole('button', { name: 'Änderungen prüfen', exact: true }).click();
  const save = settings.getByRole('button', { name: 'Geprüfte Rechte speichern' });
  await expect(save).toBeDisabled();
  await settings.getByLabel('Ich bestätige die Erweiterung der Inhalts- und Moderationsrechte separat.').check();
  await expect(save).toBeDisabled();
  await settings.getByLabel('Ich habe den Betreiberhinweis geprüft und bestätige den Zugriff auf private Fragen.').check();
  await save.click();
  await expect(settings.getByRole('status')).toContainText('Rechte gespeichert');
  expect(writes).toHaveLength(1);
  expect(writes[0]).toMatchObject({ foreignAccessConfirmed: true, privacyConfirmed: true, rights: { readPrivate: true, readForeign: true } });
  await user.getByRole('button', { name: 'Standardwerte wiederherstellen' }).click();
  await settings.getByRole('button', { name: 'Änderungen prüfen', exact: true }).click();
  await expect(settings.getByLabel('Ich bestätige die Erweiterung der Inhalts- und Moderationsrechte separat.')).toHaveCount(0);
  await save.click();
  await expect(settings.getByRole('status')).toContainText('Rechte gespeichert');
  expect(writes).toHaveLength(2);
  expect(writes[1].rights.readPrivate).toBe(false);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  const accessibility = await new AxeBuilder({ page }).withTags(['wcag2a','wcag2aa','wcag21aa']).analyze();
  expect(accessibility.violations).toEqual([]);
  await page.screenshot({ path: test.info().outputPath('question-permissions-mobile.png'), fullPage: true });
});

test('moderation requires a purpose, preserves provenance, and confirms deletion', async ({ page }) => {
  await api(page, { capabilities: { administration: false, moderation: true } });
  let version = 1;
  let revision;
  let deletion;
  await page.route('**/api/v1/questions/permissions', route => route.fulfill({ json: { data: defaults('moderator') } }));
  await page.route('**/api/v1/moderation/questions/**', async route => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith('/browse')) return route.fulfill({ json: { data: [{ questionId: 'foreign-question', versionId: 'foreign-version', versionNumber: version, visibility: 'private', subject: 'Biologie', topic: 'Zellen' }] } });
    if (path.endsWith('/inspect')) return route.fulfill({ json: { data: { version, selectionMode: 'single', subject: 'Biologie', topic: 'Zellen', language: 'de', source: 'Originalquelle', license: 'CC BY-SA 4.0', authorAttribution: 'Originalautor', prompt: [{ kind: 'text', text: 'Originalfrage', mediaId: null }], explanation: [{ kind: 'text', text: 'Originalerklärung', mediaId: null }], answers: [{ isCorrect: true, blocks: [{ kind: 'text', text: 'Richtig', mediaId: null }] }, { isCorrect: false, blocks: [{ kind: 'text', text: 'Falsch', mediaId: null }] }] } } });
    if (path.endsWith('/revise')) { revision = route.request().postDataJSON(); version++; return route.fulfill({ status: 201, json: { data: {} } }); }
    if (path.endsWith('/delete')) { deletion = route.request().postDataJSON(); return route.fulfill({ status: 204 }); }
    return route.fulfill({ status: 403 });
  });
  await page.goto('/administration');
  const moderation = page.locator('app-question-moderation');
  await expect(moderation.getByRole('button', { name: 'Fragenübersicht prüfen' })).toBeDisabled();
  await moderation.getByLabel('Konkreter Prüfzweck (10–500 Zeichen)').fill('Fachlichen Fehler prüfen');
  await moderation.getByRole('button', { name: 'Fragenübersicht prüfen' }).click();
  await moderation.getByRole('button', { name: 'Inhalt mit Prüfzweck öffnen' }).click();
  await expect(moderation.getByText('Herkunft: Originalquelle', { exact: false })).toContainText('Originalautor');
  const accessibility = await new AxeBuilder({ page }).withTags(['wcag2a','wcag2aa','wcag21aa']).analyze();
  expect(accessibility.violations).toEqual([]);
  await moderation.getByLabel('Fragetext', { exact: true }).fill('Korrigierte Frage');
  await moderation.getByRole('button', { name: 'Neue private Fassung speichern' }).click();
  await expect(moderation.getByRole('status')).toContainText('Neue private Fassung gespeichert');
  expect(revision).toMatchObject({ expectedVersion: 1, reason: 'Fachlichen Fehler prüfen', content: { source: 'Originalquelle', license: 'CC BY-SA 4.0', prompt: [{ text: 'Korrigierte Frage' }] } });
  await moderation.getByRole('button', { name: 'Inhalt mit Prüfzweck öffnen' }).click();
  await expect(moderation.getByRole('button', { name: 'Frage löschen', exact: true })).toBeDisabled();
  await moderation.getByLabel('Löschung dieser Frage ausdrücklich bestätigen').check();
  await moderation.getByRole('button', { name: 'Frage löschen', exact: true }).click();
  await expect(moderation.getByRole('status')).toContainText('Fragen gelöscht');
  expect(deletion).toMatchObject({ questionIds: ['foreign-question'], confirmed: true, reason: 'Fachlichen Fehler prüfen' });
});

test('ordinary users cannot enter administration and restricted own edits disable saving', async ({ page }) => {
  await api(page);
  await page.route('**/api/v1/questions/permissions', route => route.fulfill({ json: { data: { ...defaults('user'), editOwn: false } } }));
  await page.goto('/administration');
  await expect(page).toHaveURL(/overview/);
  await expect(page.locator('app-question-permissions')).toHaveCount(0);
  await page.goto('/questions');
  await page.locator('.draft-list button').first().click();
  await expect(page.getByRole('button', { name: 'Privat speichern', exact: true })).toBeDisabled();
  await expect(page.getByRole('button', { name: 'Neue Fassung veröffentlichen', exact: true })).toBeDisabled();
});
