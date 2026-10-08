import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { QuestionRights } from './question-rights';
import { LanguageService } from './language';
import { CatalogPackageImport } from './catalog-package-import';
import { InstanceCatalogLibrary } from './instance-catalog-library';

interface Catalog {
  id: string;
  name: string;
  description?: string | null;
  questionCount: number;
}

interface CatalogQuestion {
  id: string;
  prompt: string;
  catalogId: string | null;
  catalogIds?: string[];
}

@Component({
  selector: 'app-catalog-library',
  imports: [FormsModule, RouterLink, CatalogPackageImport, InstanceCatalogLibrary],
  styles: [
    `
      .workspace-card label.membership-choice {
        display: flex;
        align-items: center;
        gap: 0.6rem;
        min-height: 44px;
        font-weight: normal;
      }
      .workspace-card .membership-choice input {
        width: 1.25rem;
        height: 1.25rem;
        flex: 0 0 auto;
      }
    `,
  ],
  template: `
    <app-instance-catalog-library (chosen)="packageImport.useFile($event)" />
    <app-catalog-package-import #packageImport (completed)="reload()" />
    <section class="workspace-card" aria-labelledby="catalog-library-title">
      <h2 id="catalog-library-title">{{ language.t('Private Kataloge') }}</h2>
      <p>{{ language.t('Deine Fragen bleiben beim Löschen eines Katalogs erhalten.') }}</p>
      <form class="settings-grid" (ngSubmit)="create()">
        <label
          >{{ language.t('Neuer Katalog')
          }}<input name="catalog-name" [(ngModel)]="name" maxlength="120" required
        /></label>
        <label
          >{{ language.t('Beschreibung (optional)') }}
          <textarea
            name="catalog-description"
            [(ngModel)]="description"
            maxlength="2048"
            rows="2"
          ></textarea>
        </label>
        <button type="submit" class="primary-action" [disabled]="busy() || !name.trim()">
          {{ language.t('Katalog anlegen') }}
        </button>
      </form>
      <ul class="catalog-list">
        @for (catalog of catalogs(); track catalog.id) {
          <li class="workspace-card">
            <h3>{{ catalog.name }}</h3>
            @if (catalog.description) {
              <p>{{ catalog.description }}</p>
            }
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
              <label
                >{{ language.t('Beschreibung ändern') }}
                <textarea [(ngModel)]="catalog.description" maxlength="2048" rows="2"></textarea>
              </label>
              <button
                type="button"
                class="secondary-action"
                [disabled]="busy()"
                (click)="rename(catalog)"
              >
                {{ language.t('Katalog speichern') }}
              </button>
              <button
                type="button"
                class="secondary-action"
                [disabled]="busy()"
                (click)="remove(catalog)"
              >
                {{ language.t('Katalog löschen') }}
              </button>
              <fieldset>
                <legend>{{ language.t('Fragen zuordnen') }}</legend>
                <p>
                  {{
                    language.t(
                      'Hinzufügen oder Entfernen ändert keine anderen Kataloge und keine Lernstände.'
                    )
                  }}
                </p>
                @if (questionError()) {
                  <p role="status">{{ questionError() }}</p>
                }
                @for (question of questions(); track question.id) {
                  <label class="membership-choice"
                    ><input
                      type="checkbox"
                      [disabled]="busy() || !rights.allows('editOwn')"
                      [checked]="belongs(question, catalog.id)"
                      (change)="changeMembership(catalog, question, $event)"
                    />{{ question.prompt }}</label
                  >
                } @empty {
                  <p>{{ language.t('Noch keine eigenen Fragen vorhanden.') }}</p>
                }
              </fieldset>
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
  readonly rights = inject(QuestionRights);
  readonly catalogs = signal<Catalog[]>([]);
  readonly busy = signal(false);
  readonly message = signal('');
  name = '';
  description = '';
  readonly questions = signal<CatalogQuestion[]>([]);
  readonly questionError = signal('');

  ngOnInit(): void {
    void this.rights.refresh();
    void this.reload();
    void this.loadQuestions();
  }

  async reload(): Promise<void> {
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
    if (await this.mutate('/api/v1/catalogs/', 'POST', this.name, this.description)) {
      this.name = '';
      this.description = '';
    }
  }

  async rename(catalog: Catalog): Promise<void> {
    if (catalog.name.trim())
      await this.mutate(
        `/api/v1/catalogs/${catalog.id}`,
        'PUT',
        catalog.name,
        catalog.description ?? '',
      );
  }

  async remove(catalog: Catalog): Promise<void> {
    if (!window.confirm(this.language.t('Katalog löschen? Die Fragen bleiben erhalten.'))) return;
    await this.mutate(`/api/v1/catalogs/${catalog.id}`, 'DELETE');
  }

  belongs(question: CatalogQuestion, id: string): boolean {
    return (question.catalogIds ?? (question.catalogId ? [question.catalogId] : [])).includes(id);
  }

  async loadQuestions(): Promise<void> {
    try {
      const response = await fetch('/api/v1/catalogs/questions', {
        credentials: 'same-origin',
      });
      if (!response.ok) throw new Error();
      this.questions.set(((await response.json()) as { data: CatalogQuestion[] }).data);
      this.questionError.set('');
    } catch {
      this.questionError.set(this.language.t('Fragen konnten nicht geladen werden.'));
    }
  }

  async changeMembership(catalog: Catalog, question: CatalogQuestion, event: Event): Promise<void> {
    (event.target as HTMLInputElement).checked = this.belongs(question, catalog.id);
    await this.mutate(
      `/api/v1/catalogs/${catalog.id}/questions/${question.id}`,
      this.belongs(question, catalog.id) ? 'DELETE' : 'PUT',
    );
  }

  private async mutate(
    path: string,
    method: string,
    name?: string,
    description?: string,
  ): Promise<boolean> {
    if (this.busy()) return false;
    this.busy.set(true);
    try {
      const response = await fetch(path, {
        method,
        credentials: 'same-origin',
        headers: name === undefined ? {} : { 'Content-Type': 'application/json' },
        body:
          name === undefined
            ? undefined
            : JSON.stringify({ name: name.trim(), description: description?.trim() || null }),
      });
      if (!response.ok) throw new Error();
      this.message.set(this.language.t('Katalog gespeichert. Die Fragen bleiben erhalten.'));
      await this.reload();
      await this.loadQuestions();
      return true;
    } catch {
      this.message.set(this.language.t('Katalog konnte nicht geändert werden.'));
      return false;
    } finally {
      this.busy.set(false);
    }
  }
}
