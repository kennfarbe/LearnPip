const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const ts = require('typescript');

function theme(saved, systemDark, storageFails = false) {
  const document = { documentElement: { dataset: {} }, querySelector: () => null };
  const listeners = new Map();
  const media = {
    matches: systemDark,
    addEventListener: (k, fn) => listeners.set(k, fn),
    removeEventListener: () => {},
  };
  const storage = {
    getItem: () => {
      if (storageFails) throw Error();
      return saved;
    },
    setItem: () => {
      if (storageFails) throw Error();
    },
  };
  const context = {
    document,
    window: { matchMedia: () => media },
    localStorage: storage,
    exports: {},
    require: () => ({
      Injectable: () => (x) => x,
      inject: () => ({ onDestroy() {} }),
      signal: (value) => {
        const fn = () => value;
        fn.set = (x) => {
          value = x;
        };
        return fn;
      },
    }),
  };
  const source = ts.transpileModule(fs.readFileSync('src/app/theme.ts', 'utf8'), {
    compilerOptions: { module: ts.ModuleKind.CommonJS, experimentalDecorators: true },
  }).outputText;
  vm.runInNewContext(source, context);
  return { service: new context.exports.ThemeService(), document, media, listeners };
}

test('explicit theme persists across initialization and ignores system changes', () => {
  const x = theme('dark', false);
  assert.equal(x.document.documentElement.dataset.theme, 'dark');
  x.service.set('light');
  x.media.matches = true;
  x.listeners.get('change')();
  assert.equal(x.document.documentElement.dataset.theme, 'light');
});
test('system responds live and invalid preferences are ignored', () => {
  const x = theme('invalid', false);
  assert.equal(x.service.preference(), 'system');
  x.media.matches = true;
  x.listeners.get('change')();
  assert.equal(x.document.documentElement.dataset.theme, 'dark');
  x.service.set('invalid');
  assert.equal(x.service.preference(), 'system');
});
test('blocked browser storage still permits a theme choice', () => {
  const x = theme(null, false, true);
  x.service.set('dark');
  assert.equal(x.document.documentElement.dataset.theme, 'dark');
});
test('service worker precaches actual bundles and leaves API/private media alone', async () => {
  const handlers = {};
  const added = [];
  const cache = { addAll: async (urls) => added.push(...urls), put: async () => {} };
  vm.runInNewContext(fs.readFileSync('public/service-worker.js', 'utf8'), {
    URL,
    Response,
    self: {
      registration: { scope: 'https://learnpip.test/' },
      location: { href: 'https://learnpip.test/service-worker.js?build=main-123.js' },
      addEventListener: (k, fn) => (handlers[k] = fn),
    },
    fetch: async (url) =>
      url.endsWith('shell-assets.json')
        ? new Response(JSON.stringify(['chunk-workspaces.js', 'media/KaTeX_Main-Regular-ABC123.woff2']))
        : new Response(
            '<script type="module" src="main-123.js"></script><link rel="stylesheet" href="styles-123.css">',
          ),
    caches: { open: async () => cache },
  });
  let installed;
  handlers.install({ waitUntil: (p) => (installed = p) });
  await installed;
  assert(added.includes('https://learnpip.test/main-123.js'));
  assert(added.includes('https://learnpip.test/styles-123.css'));
  assert(added.includes('https://learnpip.test/theme-init.js'));
  assert(added.includes('https://learnpip.test/chunk-workspaces.js'));
  assert(added.includes('https://learnpip.test/media/KaTeX_Main-Regular-ABC123.woff2'));
  assert.equal(added.length, new Set(added).size);
  for (const [url, destination] of [
    ['/api/questions', ''],
    ['/media/private.jpg', 'image'],
    ['/media/private.mp4', 'video'],
  ]) {
    handlers.fetch({
      request: { url: 'https://learnpip.test' + url, method: 'GET', destination },
      respondWith: () => assert.fail('private request intercepted'),
    });
  }
});
