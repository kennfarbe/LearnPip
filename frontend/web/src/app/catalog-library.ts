import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LanguageService } from './language';
import { CatalogPackageImport } from './catalog-package-import';

interface Catalog {
  id: string;
  name: string;
  questionCount: number;
}

@Component({
  selector: 'app-catalog-library',
  imports: [FormsModule, RouterLink, CatalogPackageImport],
  template: `
    <app-catalog-package-import />
    <section class="workspace-card" aria-labelledby="catalog-library-title">
      <h2 id="catalog-library-title">{{ language.t('Private Kataloge') }}</h2>
      <p>{{ language.t('Deine Fragen bleiben beim Löschen eines Katalogs erhalten.') }}</p>
      <form class="settings-grid" (ngSubmit)="create()">
        <label
          >{{ language.t('Neuer Katalog')
          }}<input name="catalog-name" [(ngModel)]="name" maxlength="120" required
        /></label>
        <button type="submit" class="primary-action" [disabled]="busy() || !name.trim()">
          {{ language.t('Katalog anlegen') }}
        </button>
      </form>
      <ul class="catalog-list">
        @for (catalog of catalogs(); track catalog.id) {
          <li class="workspace-card">
            <h3>{{ catalog.name }}</h3>
            <p>{{ catalog.questionCount }} {{ language.t('Fragen') }}</p>
            <a
              class="secondary-action"
              routerLink="/questions"
              [queryParams]="{ catalog: catalog.id }"
              >{{ language.t('Fragen anzeigen') }}</a
            >
            <details class="workspace-disclosure">
              <summary>{{ language.t('Katalog bearbeiten') }}</summary>
              <label
                >{{ language.t('Katalogname ändern')
                }}<input [(ngModel)]="catalog.name" maxlength="120"
              /></label>
              <button
                type="button"
                class="secondary-action"
                [disabled]="busy()"
                (click)="rename(catalog)"
              >
                {{ language.t('Umbenennen') }}
              </button>
              <button
                type="button"
                class="secondary-action"
                [disabled]="busy()"
                (click)="remove(catalog)"
              >
                {{ language.t('Katalog löschen') }}
              </button>
            </details>
          </li>
        } @empty {
          <li>{{ language.t('Noch keine privaten Kataloge vorhanden.') }}</li>
        }
      </ul>
      @if (message()) {
        <p role="status">{{ message() }}</p>
      }
    </section>
  `,
})
export class CatalogLibrary implements OnInit {
  readonly language = inject(LanguageService);
  readonly catalogs = signal<Catalog[]>([]);
  readonly busy = signal(false);
  readonly message = signal('');
  name = '';

  ngOnInit(): void {
    void this.reload();
  }

  private async reload(): Promise<void> {
    try {
      const response = await fetch('/api/v1/catalogs/', { credentials: 'same-origin' });
      if (!response.ok) throw new Error();
      this.catalogs.set(((await response.json()) as { data: Catalog[] }).data);
    } catch {
      this.message.set(this.language.t('Melde dich an, um private Fragen und Kataloge zu laden.'));
    }
  }

  async create(): Promise<void> {
    if (!this.name.trim()) return;
    if (await this.mutate('/api/v1/catalogs/', 'POST', this.name)) this.name = '';
  }

  async rename(catalog: Catalog): Promise<void> {
    if (catalog.name.trim())
      await this.mutate(`/api/v1/catalogs/${catalog.id}`, 'PUT', catalog.name);
  }

  async remove(catalog: Catalog): Promise<void> {
    if (!window.confirm(this.language.t('Katalog löschen? Die Fragen bleiben erhalten.'))) return;
    await this.mutate(`/api/v1/catalogs/${catalog.id}`, 'DELETE');
  }

  private async mutate(path: string, method: string, name?: string): Promise<boolean> {
    if (this.busy()) return false;
    this.busy.set(true);
    try {
      const response = await fetch(path, {
        method,
        credentials: 'same-origin',
        headers: name === undefined ? {} : { 'Content-Type': 'application/json' },
        body: name === undefined ? undefined : JSON.stringify({ name: name.trim() }),
      });
      if (!response.ok) throw new Error();
      this.message.set(this.language.t('Katalog gespeichert. Die Fragen bleiben erhalten.'));
      await this.reload();
      return true;
    } catch {
      this.message.set(this.language.t('Katalog konnte nicht geändert werden.'));
      return false;
    } finally {
      this.busy.set(false);
    }
  }
}
