import { Component, inject, OnInit, signal } from '@angular/core';
import { JsonPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LanguageService } from './language';

interface Rights {
  license: { id: string; holder: string; attribution: string; license_url?: string };
  provenance: {
    kind: 'original' | 'adapted' | 'verbatim';
    source_url?: string;
    source_revision?: string;
    modification_note?: string;
  };
}
interface ContentRights extends Rights {
  media: Record<string, Rights>;
  metadata?: { age_band: string; difficulty: string; topics: string[] };
}
const empty = (): Rights => ({
  license: { id: 'LicenseRef-Private', holder: '', attribution: '' },
  provenance: { kind: 'original' },
});

@Component({
  selector: 'app-catalog-content-rights',
  imports: [JsonPipe, FormsModule],
  template: `
    <section class="workspace-card" aria-labelledby="rights-title" [attr.aria-busy]="busy()">
      <h2 id="rights-title">{{ language.t('Quellen und Rechte je Frage und Bild') }}</h2>
      <p>
        {{
          language.t(
            'Erfasse tatsächliche Rechteinhaber, Lizenzen und Quellen. Die Angaben gelten für den gespeicherten Inhalt. Änderungen erfordern eine erneute Prüfung. Eine technische Prüfung bestätigt keine Urheberrechte.'
          )
        }}
      </p>
      <button type="button" class="secondary-action" [disabled]="busy()" (click)="reload()">
        {{ language.t('Auswahl aktualisieren') }}
      </button>
      <label
        >{{ language.t('Frage') }}
        <select [(ngModel)]="selected" [disabled]="busy()" (ngModelChange)="load()">
          <option value="">{{ language.t('Bitte auswählen') }}</option>
          @for (question of questions(); track question.id) {
            <option [value]="question.id">{{ question.prompt }}</option>
          }
        </select>
      </label>
      @if (loaded()) {
        @if (stale()) {
          <p role="alert">
            {{
              language.t(
                'Inhalt oder Bild wurde geändert. Die bisherigen Rechteangaben müssen erneut geprüft werden.'
              )
            }}
          </p>
        }
        <details>
          <summary>{{ language.t('Frühere bestätigte Einzelnachweise') }}</summary>
          <pre>{{ history | json }}</pre>
        </details>
        <p>{{ language.t('Gespeicherte Quelle / Lizenz') }}: {{ source }} · {{ license }}</p>
        <fieldset [disabled]="busy()">
          <legend>{{ language.t('Einzelnachweise') }}</legend>
          <label
            >{{ language.t('Klasse / Zielgruppe des Inhalts')
            }}<input [(ngModel)]="audience" maxlength="80" (ngModelChange)="confirmed = false"
          /></label>
          <label
            >{{ language.t('Schwierigkeitsgrad')
            }}<select [(ngModel)]="difficulty" (ngModelChange)="confirmed = false">
              <option value="unknown">{{ language.t('Unbekannt') }}</option>
              <option value="easy">{{ language.t('Leicht') }}</option>
              <option value="medium">{{ language.t('Mittel') }}</option>
              <option value="hard">{{ language.t('Schwer') }}</option>
            </select></label
          >
          <label
            >{{ language.t('Themenhierarchie / Schlagworte (eine Angabe je Zeile)')
            }}<textarea
              [(ngModel)]="tags"
              maxlength="12800"
              (ngModelChange)="confirmed = false"
            ></textarea>
          </label>
          @for (entry of entries; track entry.id) {
            <fieldset>
              <legend>{{ entry.name }}</legend>
              <label
                >{{ language.t('Lizenz')
                }}<input
                  [(ngModel)]="entry.rights.license.id"
                  (ngModelChange)="confirmed = false"
                  maxlength="128"
              /></label>
              <label
                >{{ language.t('Lizenztext-Link')
                }}<input
                  type="url"
                  [(ngModel)]="entry.rights.license.license_url"
                  (ngModelChange)="confirmed = false"
                  maxlength="2048"
              /></label>
              <label
                >{{ language.t('Rechteinhaber')
                }}<input
                  [(ngModel)]="entry.rights.license.holder"
                  (ngModelChange)="confirmed = false"
                  maxlength="256"
              /></label>
              <label
                >{{ language.t('Attribution / Autorenangabe')
                }}<textarea
                  [(ngModel)]="entry.rights.license.attribution"
                  (ngModelChange)="confirmed = false"
                  maxlength="2048"
                ></textarea>
              </label>
              <label
                >{{ language.t('Herkunft') }}
                <select
                  [(ngModel)]="entry.rights.provenance.kind"
                  (ngModelChange)="confirmed = false"
                >
                  <option value="original">{{ language.t('Eigenes Original') }}</option>
                  <option value="adapted">{{ language.t('Bearbeitung einer Quelle') }}</option>
                  <option value="verbatim">{{ language.t('Unveränderte Übernahme') }}</option>
                </select>
              </label>
              <label
                >{{ language.t('Quellenlink / Versionsgeschichte')
                }}<input
                  type="url"
                  [(ngModel)]="entry.rights.provenance.source_url"
                  (ngModelChange)="confirmed = false"
                  maxlength="2048"
              /></label>
              <label
                >{{ language.t('Quellenrevision / Abrufstand')
                }}<input
                  [(ngModel)]="entry.rights.provenance.source_revision"
                  (ngModelChange)="confirmed = false"
                  maxlength="256"
              /></label>
              <label
                >{{ language.t('Änderungsvermerk')
                }}<textarea
                  [(ngModel)]="entry.rights.provenance.modification_note"
                  (ngModelChange)="confirmed = false"
                  maxlength="2048"
                ></textarea>
              </label>
            </fieldset>
          }
          <p>
            {{
              language.t(
                'Private Nutzung benötigt keine offene Lizenz. Fremde Inhalte behalten ihre tatsächliche Lizenz; Wikipedia-Bearbeitungen benötigen passende Share-Alike-Nachweise. Bilder werden einzeln ausgezeichnet.'
              )
            }}
          </p>
          <label
            ><input type="checkbox" [(ngModel)]="confirmed" />{{
              language.t('Ich habe Inhalt, Quellen und Rechteangaben für diese Fassung geprüft.')
            }}</label
          >
          <button type="button" class="secondary-action" [disabled]="!confirmed" (click)="save()">
            {{ language.t('Rechteangaben speichern') }}
          </button>
        </fieldset>
      }
      @if (message()) {
        <p role="status">{{ message() }}</p>
      }
    </section>
  `,
  styles: `
    pre {
      white-space: pre-wrap;
      overflow-wrap: anywhere;
    }
    label {
      display: block;
      margin-block: 0.75rem;
      overflow-wrap: anywhere;
    }
    input:not([type='checkbox']),
    select,
    textarea {
      width: 100%;
      min-width: 0;
    }
    fieldset {
      min-width: 0;
      margin-block: 1rem;
    }
  `,
})
export class CatalogContentRights implements OnInit {
  readonly language = inject(LanguageService);
  readonly questions = signal<{ id: string; prompt: string }[]>([]);
  readonly busy = signal(false);
  readonly message = signal('');
  readonly loaded = signal(false);
  readonly stale = signal(false);
  selected = '';
  confirmed = false;
  source = '';
  license = '';
  hash = '';
  history: unknown[] = [];
  audience = '';
  difficulty = 'unknown';
  tags = '';
  entries: { id: string; name: string; rights: Rights }[] = [];

  ngOnInit(): void {
    void this.reload();
  }

  async reload(): Promise<void> {
    await this.perform(async () => {
      this.questions.set((await this.json('/api/v1/catalog-exports/questions')).data);
    });
  }

  async load(): Promise<void> {
    this.loaded.set(false);
    this.confirmed = false;
    if (!this.selected) return;
    await this.perform(async () => {
      const response = await fetch('/api/v1/catalog-rights/' + this.selected, {
        credentials: 'same-origin',
      });
      if (!response.ok) throw new Error(this.language.t('Frage konnte nicht geladen werden.'));
      const item = (await response.json()) as {
        data: {
          history?: unknown[];
          contentSha256: string;
          stale: boolean;
          source: string;
          license: string;
          rights: ContentRights | null;
          media: { id: string; altText: string }[];
        };
      };
      this.history = item.data.history ?? [];
      this.hash = item.data.contentSha256;
      this.stale.set(item.data.stale);
      this.source = item.data.source;
      this.license = item.data.license;
      this.audience = item.data.rights?.metadata?.age_band ?? '';
      this.difficulty = item.data.rights?.metadata?.difficulty ?? 'unknown';
      this.tags = item.data.rights?.metadata?.topics.join('\n') ?? '';
      const text = item.data.rights ?? empty();
      if (!item.data.rights && item.data.license) text.license.id = item.data.license;
      this.entries = [
        {
          id: 'question',
          name: this.language.t('Fragetext, Antworten und Erklärung'),
          rights: text,
        },
        ...item.data.media.map((image) => ({
          id: image.id,
          name: this.language.t('Bild') + ': ' + image.altText,
          rights: item.data.rights?.media[image.id] ?? empty(),
        })),
      ];
      this.loaded.set(true);
    });
  }

  async save(): Promise<void> {
    if (!this.confirmed || !this.loaded()) return;
    await this.perform(async () => {
      const text = this.clean(this.entries[0].rights);
      const rights: ContentRights = {
        ...text,
        metadata: {
          age_band: this.audience.trim(),
          difficulty: this.difficulty,
          topics: [
            ...new Set(
              this.tags
                .split('\n')
                .map((tag) => tag.trim())
                .filter(Boolean),
            ),
          ],
        },
        media: Object.fromEntries(
          this.entries.slice(1).map((entry) => [entry.id, this.clean(entry.rights)]),
        ),
      };
      await this.json('/api/v1/catalog-rights/' + this.selected, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ contentSha256: this.hash, rights }),
      });
      this.stale.set(false);
      this.confirmed = false;
      this.message.set(this.language.t('Rechteangaben für diese Fassung gespeichert.'));
    });
  }

  private clean(rights: Rights): Rights {
    const license = { ...rights.license };
    const provenance = { ...rights.provenance };
    if (!license.license_url?.trim()) delete license.license_url;
    if (!provenance.source_url?.trim()) delete provenance.source_url;
    if (!provenance.source_revision?.trim()) delete provenance.source_revision;
    if (!provenance.modification_note?.trim()) delete provenance.modification_note;
    return { license, provenance };
  }

  private async json(
    url: string,
    options: RequestInit = {},
  ): Promise<{ data: { id: string; prompt: string }[] }> {
    const response = await fetch(url, { credentials: 'same-origin', ...options });
    if (!response.ok) {
      const error = (await response.json().catch(() => null)) as {
        errors?: Record<string, string[]>;
        message?: string;
      } | null;
      throw new Error(
        error?.errors?.['rights']?.[0] ??
          error?.message ??
          this.language.t('Rechteangaben konnten nicht verarbeitet werden.'),
      );
    }
    return response.json() as Promise<{ data: { id: string; prompt: string }[] }>;
  }

  private async perform(action: () => Promise<void>): Promise<void> {
    if (this.busy()) return;
    this.busy.set(true);
    this.message.set('');
    try {
      await action();
    } catch (error) {
      this.message.set(
        error instanceof Error
          ? error.message
          : this.language.t('Rechteangaben konnten nicht verarbeitet werden.'),
      );
    } finally {
      this.busy.set(false);
    }
  }
}
