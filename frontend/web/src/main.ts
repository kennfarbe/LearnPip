import { isDevMode } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

bootstrapApplication(App, appConfig).catch((err) => console.error(err));

if (!isDevMode() && 'serviceWorker' in navigator) {
  const register = () => {
    // The hashed entry bundle gives each production build a new worker URL.
    const entry = document.querySelector<HTMLScriptElement>('script[type="module"][src]')?.src;
    const workerUrl = new URL('service-worker.js', document.baseURI);
    workerUrl.searchParams.set(
      'build',
      entry ? new URL(entry).pathname.split('/').pop() || 'app' : 'app',
    );
    navigator.serviceWorker
      .register(workerUrl.href, { scope: document.baseURI })
      .catch(console.error);
  };

  if (document.readyState === 'complete') {
    register();
  } else {
    window.addEventListener('load', register, { once: true });
  }
}
