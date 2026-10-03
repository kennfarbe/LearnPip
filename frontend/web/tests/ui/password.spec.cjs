const { test, expect } = require('@playwright/test');
const AxeBuilder = require('@axe-core/playwright').default;
const { api } = require('./fixtures.cjs');

test('password login, failure and change are accessible and clear secrets', async ({ page }) => {
  await api(page);
  let changeBody;
  await page.route('**/api/v1/auth/password', async route => {
    const body = route.request().postDataJSON();
    await route.fulfill({ status: body.password === 'valid test password' ? 200 : 401, json: { data: {} } });
  });
  await page.route('**/api/v1/auth/password/change', async route => {
    changeBody = route.request().postDataJSON();
    await route.fulfill({ status: 204 });
  });
  await page.goto('/settings');
  await page.getByLabel('Benutzername', { exact: true }).fill('admin');
  await page.getByLabel('Passwort', { exact: true }).fill('wrong test password');
  await page.getByRole('button', { name: 'Anmelden', exact: true }).click();
  await expect(page.getByRole('status').filter({ hasText: 'fehlgeschlagen' })).toBeVisible();
  await expect(page.getByLabel('Passwort', { exact: true })).toHaveValue('');
  await page.getByLabel('Passwort', { exact: true }).fill('valid test password');
  await page.getByRole('button', { name: 'Anmelden', exact: true }).click();
  await expect(page.getByRole('status').filter({ hasText: 'Angemeldet.' })).toBeVisible();
  await page.getByText('Lokales Passwort ändern', { exact: true }).click();
  await page.getByLabel('Bisheriges Passwort', { exact: true }).fill('valid test password');
  await page.getByLabel('Neues Passwort', { exact: true }).fill('new test password');
  await page.getByLabel('Neues Passwort wiederholen', { exact: true }).fill('different password');
  await page.getByRole('button', { name: 'Passwort ändern und abmelden' }).click();
  await expect(page.getByText('Die neuen Passwörter stimmen nicht überein.')).toBeVisible();
  expect(changeBody).toBeUndefined();
  await page.getByLabel('Neues Passwort wiederholen', { exact: true }).fill('new test password');
  await page.getByRole('button', { name: 'Passwort ändern und abmelden' }).click();
  await expect(page.getByText('Passwort geändert. Bitte erneut anmelden.')).toBeVisible();
  expect(changeBody).toEqual({ currentPassword: 'valid test password', newPassword: 'new test password' });
  await expect(page.getByLabel('Neues Passwort', { exact: true })).toHaveValue('');
  for (const theme of ['light', 'dark', 'system']) {
    await page.getByLabel('Design', { exact: true }).selectOption(theme);
    await page.setViewportSize({ width: 390, height: 844 });
    expect((await new AxeBuilder({ page }).include('app-password-access').analyze()).violations).toEqual([]);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  }
});
