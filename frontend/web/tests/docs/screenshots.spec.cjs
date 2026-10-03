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
  await capture(page, 'question-editor');

  await page.goto('/catalogs');
  await expect(page.getByText('Mathematik', { exact: true })).toBeVisible();
  await capture(page, 'catalogs');

  await page.goto('/settings');
  await page.getByLabel('Design', { exact: true }).selectOption('dark');
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/learn');
  await page.getByRole('button', { name: 'Sitzung starten' }).click();
  await page.getByRole('radio', { name: 'Vier', exact: true }).check();
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
  await capture(page, 'learning-mobile-dark');
});
