import { DestroyRef, Injectable, inject, signal } from '@angular/core';

type ThemePreference = 'light' | 'dark' | 'system';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly preference = signal<ThemePreference>('system');
  private readonly system = window.matchMedia('(prefers-color-scheme: dark)');

  constructor() {
    let saved: string | null = null;
    try {
      saved = localStorage.getItem('learnpip-theme');
    } catch {
      // The setting still works when browser storage is disabled.
    }
    this.set(saved === 'light' || saved === 'dark' ? saved : 'system');
    const onChange = () => this.apply();
    this.system.addEventListener('change', onChange);
    inject(DestroyRef).onDestroy(() => this.system.removeEventListener('change', onChange));
  }

  set(value: string): void {
    if (value !== 'light' && value !== 'dark' && value !== 'system') return;
    this.preference.set(value);
    try {
      localStorage.setItem('learnpip-theme', value);
    } catch {
      // Keep the in-memory choice for this visit.
    }
    this.apply();
  }

  private apply(): void {
    const dark =
      this.preference() === 'dark' || (this.preference() === 'system' && this.system.matches);
    document.documentElement.dataset['theme'] = dark ? 'dark' : 'light';
    document
      .querySelector('meta[name="theme-color"]')
      ?.setAttribute('content', dark ? '#101c18' : '#205d45');
  }
}
