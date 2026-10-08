const { test, expect } = require('@playwright/test');
const AxeBuilder = require('@axe-core/playwright').default;
const { api, draft } = require('./fixtures.cjs');

const catalogs = () => [
  { id: 'catalog-1', name: 'Mathematik', description: 'Grundlagen wiederholen', questionCount: 1 },
  { id: 'catalog-2', name: 'Prüfung', description: null, questionCount: 1 },
];

test('editor loads multiple memberships, filters the secondary catalog and saves the whole selection', async ({ page }) => {
  await api(page);
  const question = { ...draft('q-1', 'Was ergibt 2 + 2?', 'Mathematik', 1, 'catalog-1'), catalogIds: ['catalog-1', 'catalog-2'] };
  await page.route('**/api/v1/catalogs/', route => route.fulfill({ json: { data: catalogs() } }));
  await page.route('**/api/v1/questions/drafts', route => route.fulfill({ json: { data: [question, draft('q-2', 'Andere Frage', 'Biologie')] } }));
  let saved;
  await page.route('**/api/v1/questions/q-1/draft', route => {
    saved = route.request().postDataJSON();
    return route.fulfill({ status: 204 });
  });
  await page.goto('/questions?catalog=catalog-2');
  await expect(page.locator('.draft-list button')).toHaveCount(1);
  await page.locator('.draft-list button').click();
  await page.getByText('Sprache und Katalogzuordnung', { exact: true }).click();
  const membership = page.locator('app-question-editor fieldset').filter({ has: page.getByRole('checkbox', { name: 'Prüfung', exact: true }) });
  await expect(membership.getByRole('checkbox', { name: 'Mathematik', exact: true })).toBeChecked();
  await expect(membership.getByRole('checkbox', { name: 'Prüfung', exact: true })).toBeChecked();
  await membership.getByRole('checkbox', { name: 'Mathematik', exact: true }).uncheck();
  await page.getByRole('button', { name: 'Privat speichern', exact: true }).click();
  await expect.poll(() => saved?.catalogIds).toEqual(['catalog-2']);
  expect(saved.catalogId).toBeNull();
  expect((await new AxeBuilder({ page }).include('app-question-editor').analyze()).violations).toEqual([]);
});

test('catalog administration edits descriptions and changes only the chosen membership', async ({ page }) => {
  await api(page);
  const current = catalogs();
  let question = { id: 'q-1', prompt: 'Was ergibt 2 + 2?', catalogId: 'catalog-1', catalogIds: ['catalog-1', 'catalog-2'] };
  const changes = [];
  await page.route('**/api/v1/catalogs/**', route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    if (path.endsWith('/questions')) return route.fulfill({ json: { data: [question] } });
    if (path.endsWith('/catalogs/')) {
      if (request.method() === 'POST') current.push({ id: 'catalog-3', questionCount: 0, ...request.postDataJSON() });
      return route.fulfill({ json: { data: current } });
    }
    const match = path.match(/\/catalogs\/([^/]+)\/questions\/q-1$/);
    if (match) {
      changes.push({ catalog: match[1], method: request.method() });
      question = { ...question, catalogIds: request.method() === 'DELETE' ? question.catalogIds.filter(id => id !== match[1]) : [...question.catalogIds, match[1]] };
    } else if (request.method() === 'PUT') Object.assign(current.find(item => item.id === path.split('/').at(-1)), request.postDataJSON());
    return route.fulfill({ status: 204 });
  });
  await page.goto('/catalogs');
  await page.getByLabel('Neuer Katalog', { exact: true }).fill('Eigene Auswahl');
  await page.getByLabel('Beschreibung (optional)', { exact: true }).fill('Für das nächste Kapitel');
  await page.getByRole('button', { name: 'Katalog anlegen', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Eigene Auswahl', exact: true })).toBeVisible();
  expect(current.at(-1).description).toBe('Für das nächste Kapitel');
  const card = page.locator('li.workspace-card').filter({ has: page.getByRole('heading', { name: 'Mathematik', exact: true }) });
  await card.getByText('Katalog bearbeiten', { exact: true }).click();
  await card.getByLabel('Beschreibung ändern', { exact: true }).fill('Neu beschrieben');
  await card.getByRole('button', { name: 'Katalog speichern', exact: true }).click();
  await expect(card.getByText('Neu beschrieben', { exact: true })).toBeVisible();
  await card.getByRole('checkbox', { name: 'Was ergibt 2 + 2?', exact: true }).click();
  await expect.poll(() => changes.length).toBe(1);
  expect(changes).toEqual([{ catalog: 'catalog-1', method: 'DELETE' }]);
  await expect(card.getByRole('checkbox', { name: 'Was ergibt 2 + 2?', exact: true })).not.toBeChecked();
  expect(question.catalogIds).toEqual(['catalog-2']);
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  expect((await new AxeBuilder({ page }).include('app-catalog-library').analyze()).violations).toEqual([]);
});

test('a refused membership update preserves the selection and shows an error', async ({ page }) => {
  await api(page);
  await page.route('**/api/v1/catalogs/questions', route => route.fulfill({ json: { data: [{ id: 'q-1', prompt: 'Was ergibt 2 + 2?', catalogId: 'catalog-1', catalogIds: ['catalog-1'] }] } }));
  await page.route('**/api/v1/catalogs/catalog-1/questions/q-1', route => route.fulfill({ status: 403 }));
  await page.goto('/catalogs');
  await page.getByText('Katalog bearbeiten', { exact: true }).click();
  const checkbox = page.getByRole('checkbox', { name: 'Was ergibt 2 + 2?', exact: true });
  await checkbox.click();
  await expect(page.getByText('Katalog konnte nicht geändert werden.', { exact: true })).toBeVisible();
  await expect(checkbox).toBeChecked();
});

test('membership controls respect a revoked own-edit permission', async ({ page }) => {
  await api(page);
  await page.route('**/api/v1/questions/permissions', route => route.fulfill({ json: { data: { readOwn: true, editOwn: false } } }));
  await page.goto('/catalogs');
  await page.getByText('Katalog bearbeiten', { exact: true }).click();
  await expect(page.getByRole('checkbox', { name: 'Was ergibt 2 + 2?', exact: true })).toBeDisabled();
});
