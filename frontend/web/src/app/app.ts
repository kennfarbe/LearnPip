import { Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { ApplicationAccess } from './application-access';
import { LanguageService } from './language';
import { workspaces } from './navigation';

@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly access = inject(ApplicationAccess);
  protected readonly language = inject(LanguageService);
  protected readonly workspaces = workspaces;
  protected readonly current = signal<(typeof workspaces)[number]>(workspaces[0]);
  protected readonly navigationOpen = signal(false);
  private readonly router = inject(Router);

  constructor() {
    this.language.set(this.language.current());
    void this.access.refresh();
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(inject(DestroyRef)),
      )
      .subscribe((event) => {
        const path = event.urlAfterRedirects.split(/[?#]/)[0];
        this.current.set(workspaces.find((item) => item.path === path) ?? workspaces[0]);
        this.navigationOpen.set(false);
        requestAnimationFrame(() => document.getElementById('page-title')?.focus());
      });
    effect(() => {
      document.title = `${this.language.t(this.current().label)} · LearnPip`;
    });
  }

  protected closeNavigation(): void {
    this.navigationOpen.set(false);
    document.getElementById('navigation-toggle')?.focus();
  }
}
