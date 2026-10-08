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
    if (path.endsWith('/questions/permissions')) data = Object.fromEntries(['create','readOwn','editOwn','deleteOwn','readShared','import','export','community'].map(action => [action, true]));
    else if (path.endsWith('/auth/capabilities')) data = options.capabilities;
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
        catalogs.push({ id: 'catalog-2', name: request.postDataJSON().name, description: request.postDataJSON().description, questionCount: 0 });
      data = catalogs;
    } else if (path.endsWith('/catalogs/questions')) {
      data = drafts.map(item => ({ id: item.questionId, prompt: item.content.prompt[0].text, catalogId: item.catalogId, catalogIds: item.catalogIds }));
    } else if (path.includes('/catalogs/')) {
      const id = path.split('/').at(-1);
      if (method === 'PUT')
        Object.assign(catalogs.find((item) => item.id === id), request.postDataJSON());
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
      return route.fulfill({
        json: {
          installedVersion: 'v1.0.0',
          latestVersion: null,
          interval: 'daily',
          lastCheckedAtUtc: null,
          nextCheckAtUtc: null,
          error: null,
          release: null,
          job: null,
          state: 'idle',
        },
      });
    }
    await route.fulfill({ json: { data } });
  });
  return options;
}

module.exports = { api, draft };
