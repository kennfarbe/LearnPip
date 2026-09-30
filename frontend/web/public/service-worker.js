// Offline support covers the public app shell only. API responses and private media stay on the network.
const scope = new URL(self.registration.scope);
const prefix = 'learnpip-shell-';
const build = new URL(self.location.href).searchParams.get('build') || 'initial';
const cacheName = prefix + encodeURIComponent(build);
const coreAssets = [
  new URL('manifest.webmanifest', scope),
  new URL('favicon.svg', scope),
  new URL('icons/icon-192.png', scope),
  new URL('icons/icon-512.png', scope),
];

self.addEventListener('install', (event) => {
  event.waitUntil(
    (async () => {
      const page = await fetch(scope.href, { cache: 'reload' });
      if (!page.ok) throw new Error('Could not cache the LearnPip app shell');
      const html = await page.clone().text();
      const bundles = [...html.matchAll(/<(?:script|link)\\b[^>]*\\b(?:src|href)=["']([^"']+)["']/gi)]
        .map((match) => new URL(match[1], scope))
        .filter(
          (url) =>
            url.origin === scope.origin &&
            url.pathname.startsWith(scope.pathname) &&
            /\\.(?:js|css)$/.test(url.pathname),
        );
      const cache = await caches.open(cacheName);
      await cache.addAll([...coreAssets, ...bundles].map((url) => url.href));
      await cache.put(scope.href, page);
      await self.skipWaiting();
    })(),
  );
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    (async () => {
      const names = await caches.keys();
      await Promise.all(
        names.filter((name) => name.startsWith(prefix) && name !== cacheName).map((name) => caches.delete(name)),
      );
      await self.clients.claim();
    })(),
  );
});

self.addEventListener('fetch', (event) => {
  const request = event.request;
  if (request.method !== 'GET') return;
  const url = new URL(request.url);
  if (url.origin !== scope.origin || !url.pathname.startsWith(scope.pathname)) return;
  if (url.pathname.startsWith(new URL('api/', scope).pathname)) return;

  if (request.mode === 'navigate') {
    event.respondWith(
      fetch(request).catch(async () => {
        const cache = await caches.open(cacheName);
        return (await cache.match(scope.href)) || Response.error();
      }),
    );
    return;
  }

  if (!['script', 'style', 'font'].includes(request.destination)) return;
  event.respondWith(
    (async () => {
      const cache = await caches.open(cacheName);
      const stored = await cache.match(request);
      if (stored) return stored;
      const response = await fetch(request);
      if (response.ok && response.type === 'basic') {
        await cache.put(request, response.clone());
      }
      return response;
    })(),
  );
});
