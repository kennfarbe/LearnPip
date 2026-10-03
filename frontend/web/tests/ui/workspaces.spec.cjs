const { test, expect } = require('@playwright/test');
const AxeBuilder = require('@axe-core/playwright').default;

function draft(id, prompt, subject, latestVersion = 0, catalogId = null) {
  return {
    questionId: id,
    catalogId,
    latestVersion,
    content: {
      subject,
      topic: 'Grundlagen',
      language: 'de',
      source: 'Eigene Frage',
      license: 'Eigene Inhalte',
      selectionMode: 'single',
      prompt: [{ kind: 'text', text: prompt }],
      explanation: [],
      answers: [
        { isCorrect: true, blocks: [{ kind: 'text', text: 'Vier' }] },
        { isCorrect: false, blocks: [{ kind: 'text', text: 'Fünf' }] },
      ],
    },
  };
}

async function api(page, options = {}) {
  options.capabilities ??= { administration: false, moderation: false };
  let catalogs = [{ id: 'catalog-1', name: 'Mathematik', questionCount: 1 }];
  let drafts = [
    draft('q-1', 'Was ergibt 2 + 2?', 'Mathematik', 1, 'catalog-1'),
    draft('q-2', 'Was ist eine Zelle?', 'Biologie'),
  ];
  const session = { id: 'session-1', total: 2, answered: 0, skipped: 0, completed: false };
  const question = () => ({
    questionId: `q-${session.answered + session.skipped + 1}`,
    versionId: 'version-1',
    selectionMode: 'single',
    prompt: [{ kind: 'text', text: 'Was ergibt 2 + 2?' }],
    answers: [
      { id: 'a-1', blocks: [{ kind: 'text', text: 'Vier' }] },
      { id: 'a-2', blocks: [{ kind: 'text', text: 'Fünf' }] },
    ],
    hint: null,
    nextStep: null,
    language: 'de',
    requestedLanguage: 'de',
    translationMissing: false,
    translationId: null,
    versionNumber: 1,
  });
  const snapshot = () => ({
    ...session,
    completed: session.answered + session.skipped >= session.total,
    current: session.answered + session.skipped >= session.total ? null : question(),
  });
  await page.route('**/api/v1/**', async (route) => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    const method = request.method();
    let data = [];
    if (path.endsWith('/auth/capabilities')) data = options.capabilities;
    else if (path.endsWith('/auth/me'))
      data = { lastActivityAtUtc: '2026-10-03T08:00:00Z', disabledAtUtc: null };
    else if (path.endsWith('/learning/progress'))
      data = {
        totalContents: 2,
        masteredContents: 1,
        improvedContents: 1,
        participationPoints: 6,
        learningDays: 2,
        topics: [],
        recentWeeks: [],
      };
    else if (path.endsWith('/learning/review'))
      data = { totalContents: 2, masteredContents: 1, oftenForMeCount: 0, contents: [] };
    else if (/\/catalogs\/?$/.test(path)) {
      if (method === 'POST')
        catalogs.push({ id: 'catalog-2', name: request.postDataJSON().name, questionCount: 0 });
      data = catalogs;
    } else if (path.includes('/catalogs/')) {
      const id = path.split('/').at(-1);
      if (method === 'PUT')
        catalogs.find((item) => item.id === id).name = request.postDataJSON().name;
      if (method === 'DELETE') catalogs = catalogs.filter((item) => item.id !== id);
    } else if (path.endsWith('/questions/drafts')) {
      if (method === 'POST') {
        const content = request.postDataJSON().content;
        drafts.push({ ...draft('q-3', '', ''), content });
        data = { questionId: 'q-3' };
      } else data = drafts;
    } else if (/\/questions\/[^/]+\/draft$/.test(path)) {
      const id = path.split('/')[4];
      drafts.find((item) => item.questionId === id).content = request.postDataJSON().content;
    } else if (/\/questions\/[^/]+\/versions\/\d+$/.test(path)) data = { visibility: 'private' };
    else if (path.endsWith('/submission')) data = { status: '' };
    else if (path.endsWith('/learning/sessions/')) data = snapshot();
    else if (path.endsWith('/answer')) {
      session.answered++;
      data = {
        attemptId: 'attempt-1',
        contentId: 'content-1',
        isCorrect: true,
        correctOptionIds: ['a-1'],
        explanation: [{ kind: 'text', text: 'Zwei und zwei ergeben vier.' }],
        shortExplanation: 'Vier ist richtig.',
      };
    } else if (path.endsWith('/skip')) session.skipped++;
    else if (path.endsWith('/learning/sessions/session-1')) {
      if (options.sessionRevoked) return route.fulfill({ status: 401, json: {} });
      data = snapshot();
    } else if (path.endsWith('/admin/updates')) {
      if (!options.capabilities.administration) return route.fulfill({ status: 403, json: {} });
      data = {
        installedVersion: 'v1.0.0',
        interval: 'daily',
        latest: null,
        job: null,
        state: 'idle',
      };
    }
    await route.fulfill({ json: { data } });
  });
  return options;
}

async function navigate(page, name) {
  await page.getByRole('navigation').getByRole('link', { name, exact: true }).click();
  await expect(page.getByRole('heading', { level: 1, name, exact: true })).toBeVisible();
}

test('overview contains next actions, and routes separate unrelated controls', async ({ page }) => {
  await api(page);
  await page.goto('/');
  await expect(page).toHaveURL(/\/overview$/);
  await expect(page.locator('main input, main textarea')).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Jetzt lernen' })).toBeVisible();
  await expect(page.getByRole('navigation').getByRole('link', { name: 'Verwaltung' })).toHaveCount(
    0,
  );
  await navigate(page, 'Fragen');
  await expect(page.getByRole('button', { name: 'Neue Frage erstellen' })).toBeVisible();
  await expect(
    page.locator('app-learning-session, app-admin-updates, app-account-activity'),
  ).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Fragen', exact: true })).toHaveAttribute(
    'aria-current',
    'page',
  );
  await expect(page.locator('#page-title')).toBeFocused();
  await navigate(page, 'Lernen');
  await expect(page.getByRole('button', { name: 'Sitzung starten' })).toBeVisible();
  await expect(page.locator('app-question-editor, app-exam-administration')).toHaveCount(0);
  await navigate(page, 'Einstellungen');
  await expect(page.getByLabel('Design', { exact: true })).toBeVisible();
  await expect(page.locator('app-admin-updates, app-moderation-queue')).toHaveCount(0);
  await page.goto('/not-a-route');
  await expect(page).toHaveURL(/\/overview$/);
});

test('question search, filters, editing and unsaved-change guard work together', async ({
  page,
}) => {
  await api(page);
  await page.goto('/questions');
  const list = page.locator('.draft-list');
  await expect(list.getByRole('button')).toHaveCount(2);
  await page.getByLabel('Fragen suchen').fill('Zelle');
  await expect(list.getByRole('button')).toHaveCount(1);
  await page.getByLabel('Fassungsstatus').selectOption('published');
  await expect(list.getByRole('button')).toHaveCount(0);
  await page.getByLabel('Fragen suchen').fill('');
  await expect(list.getByRole('button')).toHaveCount(1);
  await list.getByRole('button').click();
  await expect(page.locator('#question-form-title')).toBeFocused();
  await expect(page.getByLabel('Herkunft', { exact: true })).toBeHidden();
  await page.getByLabel('Frage', { exact: true }).fill('Was ergibt 3 + 3?');
  page.once('dialog', (dialog) => dialog.dismiss());
  await page.getByRole('navigation').getByRole('link', { name: 'Lernen', exact: true }).click();
  await expect(page).toHaveURL(/\/questions$/);
  await expect(page.getByLabel('Frage', { exact: true })).toHaveValue('Was ergibt 3 + 3?');
  await page.getByRole('button', { name: 'Privat speichern' }).click();
  await expect(page.getByText('Entwurf gespeichert. Er bleibt privat.')).toBeVisible();
  await navigate(page, 'Lernen');
});

test('learning feedback and selected answer survive navigation; completion is clear', async ({
  page,
}) => {
  await api(page);
  await page.goto('/learn');
  await page.getByRole('button', { name: 'Sitzung starten' }).click();
  await expect(page.getByText('Eine Prüfung vorbereiten', { exact: true })).toHaveCount(0);
  await page.getByRole('radio', { name: 'Vier', exact: true }).check();
  await navigate(page, 'Übersicht');
  await navigate(page, 'Lernen');
  await expect(page.getByRole('radio', { name: 'Vier', exact: true })).toBeChecked();
  await page.getByRole('button', { name: 'Prüfen', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Richtig beantwortet' })).toBeFocused();
  await expect(page.getByRole('progressbar')).toHaveAttribute('value', '1');
  await navigate(page, 'Übersicht');
  await navigate(page, 'Lernen');
  await expect(page.getByText('Vier ist richtig.')).toBeVisible();
  await page.getByRole('button', { name: 'Nächste Frage' }).click();
  await page.getByRole('button', { name: 'Ohne Wertung überspringen' }).click();
  await expect(page.getByRole('heading', { name: 'Sitzung abgeschlossen' })).toBeFocused();
  await expect(page.getByText('1 beantwortet, 1 ohne Wertung übersprungen.')).toBeVisible();
});

test('returning to learning clears a session after its access was revoked', async ({ page }) => {
  const options = await api(page);
  await page.goto('/learn');
  await page.getByRole('button', { name: 'Sitzung starten' }).click();
  await expect(page.getByRole('radio', { name: 'Vier', exact: true })).toBeVisible();
  await navigate(page, 'Übersicht');
  options.sessionRevoked = true;
  await navigate(page, 'Lernen');
  await expect(page.getByRole('button', { name: 'Sitzung starten' })).toBeVisible();
  await expect(page.getByRole('radio')).toHaveCount(0);
  expect(await page.evaluate(() => sessionStorage.getItem('learnpip-learning-session'))).toBeNull();
});

test('administration fails closed and refreshes permissions on entry', async ({ page }) => {
  const options = await api(page);
  await page.goto('/administration');
  await expect(page).toHaveURL(/\/overview\?access=denied$/);
  await expect(page.getByRole('alert')).toHaveText(
    'Dieser Bereich ist für dein Konto nicht verfügbar.',
  );
  options.capabilities = { administration: false, moderation: true };
  await page.goto('/administration');
  await expect(page.getByRole('heading', { name: 'Verwaltung', exact: true })).toBeVisible();
  await expect(
    page.getByRole('heading', { name: 'Öffentliche Einreichungen prüfen' }),
  ).toBeVisible();
  await expect(page.locator('app-admin-updates, app-exam-administration')).toHaveCount(0);
  await navigate(page, 'Übersicht');
  options.capabilities = { administration: false, moderation: false };
  await page.getByRole('navigation').getByRole('link', { name: 'Verwaltung', exact: true }).click();
  await expect(page).toHaveURL(/access=denied/);
  await expect(page.getByRole('alert')).toHaveText(
    'Dieser Bereich ist für dein Konto nicht verfügbar.',
  );
  await expect(
    page.getByRole('navigation').getByRole('link', { name: 'Verwaltung', exact: true }),
  ).toHaveCount(0);
});

test('catalog links apply filters; deleting a catalog keeps questions', async ({ page }) => {
  await api(page);
  await page.goto('/catalogs');
  await page.getByRole('link', { name: 'Fragen anzeigen' }).click();
  await expect(page.getByLabel('Anzeigen')).toHaveValue('catalog-1');
  await expect(page.locator('.draft-list button')).toHaveCount(1);
  await navigate(page, 'Kataloge und Inhalte');
  await page.getByText('Katalog bearbeiten', { exact: true }).click();
  page.once('dialog', (dialog) => dialog.accept());
  await page.getByRole('button', { name: 'Katalog löschen', exact: true }).click();
  await expect(page.getByText('Noch keine privaten Kataloge vorhanden.')).toBeVisible();
  await navigate(page, 'Fragen');
  await expect(page.locator('.draft-list button')).toHaveCount(2);
});

test('mobile menu and disclosures work with a keyboard, without horizontal overflow', async ({
  page,
}) => {
  await api(page);
  await page.setViewportSize({ width: 320, height: 740 });
  await page.goto('/overview');
  await expect(page.getByRole('navigation')).toBeHidden();
  await page.locator('#navigation-toggle').focus();
  await page.keyboard.press('Enter');
  await expect(page.getByRole('navigation')).toBeVisible();
  await page.keyboard.press('Tab');
  await expect(
    page.getByRole('navigation').getByRole('link', { name: 'Übersicht', exact: true }),
  ).toBeFocused();
  await page.keyboard.press('Escape');
  await expect(page.locator('#navigation-toggle')).toBeFocused();
  await expect(page.getByRole('navigation')).toBeHidden();
  await page.getByRole('button', { name: 'Menü öffnen' }).click();
  await navigate(page, 'Fragen');
  await expect(page.getByRole('navigation')).toBeHidden();
  await page.getByRole('button', { name: 'Neue Frage erstellen' }).click();
  await page.getByText('Erklärung, Herkunft und Lizenz', { exact: true }).focus();
  await page.keyboard.press('Enter');
  await expect(page.getByLabel('Herkunft', { exact: true })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(
    true,
  );
});

test('language and theme persist; core pages pass accessible-name, structure and contrast checks', async ({
  page,
}) => {
  await api(page);
  for (const theme of ['light', 'dark']) {
    await page.goto('/settings');
    await page.getByLabel('Design', { exact: true }).selectOption(theme);
    await expect(page.locator('html')).toHaveAttribute('data-theme', theme);
    for (const path of ['/overview', '/questions', '/learn', '/catalogs', '/settings']) {
      await page.goto(path);
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
      if (path === '/questions')
        await page.getByRole('button', { name: 'Neue Frage erstellen' }).click();
      const result = await new AxeBuilder({ page })
        .withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa'])
        .analyze();
      expect(
        result.violations.map((item) => ({
          id: item.id,
          nodes: item.nodes.map((node) => node.target),
        })),
      ).toEqual([]);
    }
  }
  await page.getByLabel('Sprache', { exact: true }).selectOption('en');
  await expect(page.locator('html')).toHaveAttribute('lang', 'en');
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Settings');
  await page.reload();
  await expect(page.getByLabel('Language', { exact: true })).toHaveValue('en');
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
});

test.describe('offline app shell', () => {
  test.use({ serviceWorkers: 'allow' });
  test('deep links and workspace bundles remain available offline without caching API data', async ({
    page,
    context,
  }) => {
    await page.goto('/overview');
    await page.evaluate(() => navigator.serviceWorker.ready);
    await expect.poll(() => page.evaluate(() => !!navigator.serviceWorker.controller)).toBe(true);
    const cachesBefore = await page.evaluate(async () => {
      const names = await caches.keys();
      const keys = await Promise.all(
        names.map(async (name) =>
          (await (await caches.open(name)).keys()).map((request) => request.url),
        ),
      );
      return keys.flat();
    });
    expect(cachesBefore.some((url) => /chunk-.*\.js$/.test(url))).toBe(true);
    expect(cachesBefore.some((url) => url.includes('/api/'))).toBe(false);
    await context.setOffline(true);
    const fresh = await context.newPage();
    await fresh.goto('/questions');
    await expect(
      fresh.getByRole('heading', { level: 1, name: 'Fragen', exact: true }),
    ).toBeVisible();
    await fresh
      .getByRole('navigation')
      .getByRole('link', { name: 'Einstellungen', exact: true })
      .click();
    await expect(fresh.getByLabel('Design', { exact: true })).toBeVisible();
  });
});
