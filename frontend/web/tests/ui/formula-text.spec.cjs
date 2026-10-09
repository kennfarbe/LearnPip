const { test, expect } = require('@playwright/test');
const { api } = require('./fixtures.cjs');

async function question(page, prompt, answer = '$2 \\cdot 10^3 \\Omega$') {
  await api(page);
  await page.route('**/api/v1/learning/sessions/', route => route.fulfill({ json: { data: {
    id: 'math-session', total: 1, answered: 0, skipped: 0, completed: false,
    current: {
      questionId: 'math-question', versionId: 'math-version', selectionMode: 'single',
      prompt: [{ kind: 'text', text: prompt }],
      answers: [{ id: 'a', blocks: [{ kind: 'text', text: answer }] },
        { id: 'b', blocks: [{ kind: 'text', text: 'Andere Antwort' }] }],
      language: 'de', requestedLanguage: 'de', translationMissing: false, versionNumber: 1,
    },
  } } }));
  await page.goto('/learn');
  await page.getByRole('button', { name: 'Sitzung starten' }).click();
}

test('renders formulas accessibly in prompts and answers on mobile without changing text', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  const source = 'Welcher Wert ist $\\frac{U}{I}$?';
  await question(page, source);
  await expect(page.locator('.question app-formula-text math')).toHaveCount(2);
  await expect(page.locator('annotation').first()).toHaveText('\\frac{U}{I}');
  await expect(page.getByRole('radio').first()).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: test.info().outputPath('formulas-mobile.png'), fullPage: true });
});

test('keeps ordinary HTML literal and blocks formula URLs and invalid input', async ({ page }) => {
  let externalRequests = 0;
  page.on('request', request => { if (request.url().includes('malicious.example')) externalRequests++; });
  await question(page, '<img src=x onerror=alert(1)> $\\includegraphics{https://malicious.example/a}$', '$\\unknowncommand{x}$');
  await expect(page.locator('app-learning-session img')).toHaveCount(0);
  await expect(page.locator('app-learning-session')).toContainText('<img src=x onerror=alert(1)>');
  await expect(page.locator('app-learning-session')).toContainText('$\\unknowncommand{x}$');
  expect(externalRequests).toBe(0);
});

test('renders doubly escaped TeX commands used by external catalog sources', async ({ page }) => {
  await question(page, '$2 \\\\cdot 10^3 \\\\Omega$');
  await expect(page.locator('annotation').first()).toHaveText('2 \\cdot 10^3 \\Omega');
});
