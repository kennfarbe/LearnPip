import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LanguageService } from './language';

interface Candidate {
  id: string;
  catalogId: string | null;
  catalogIds?: string[];
  prompt: string;
  subject: string;
  topic: string;
  language: string;
  hasDraft: boolean;
  audience?: string;
  difficulty?: string;
  tags?: string[];
}
interface License {
  id: string;
  holder: string;
  attribution: string;
}
interface Preview {
  rightsReport?: string[];
  communityEnabled?: boolean;
  communityEligible?: boolean;
  questionCount: number;
  mediaCount: number;
  archiveBytes: number;
  previewSha256: string;
  schemaVersion: string;
  topics: string[];
  licenses: License[];
  notices: Record<string, string>;
}

@Component({
  selector: 'app-catalog-package-export',
  imports: [FormsModule],
  template: `
    <section class="workspace-card" aria-labelledby="export-title" [attr.aria-busy]="busy()">
      <h2 id="export-title">{{ language.t('Eigene Fragen als Paket exportieren') }}</h2>
      <p>
        {{
          language.t(
            'Der lokale ZIP-Download enthält nur ausgewählte Frageinhalte. Konten, Lernstände und Gruppencodes werden nicht exportiert. Es wird nichts veröffentlicht.'
          )
        }}
      </p>
      <p>
        {{
          language.t(
            'Gespeicherte Entwürfe haben Vorrang vor veröffentlichten Fassungen. Speichere Änderungen im Editor und aktualisiere anschließend die Auswahl.'
          )
        }}
      </p>
      <button type="button" class="secondary-action" [disabled]="busy()" (click)="reload()">
        {{ language.t('Auswahl aktualisieren') }}
      </button>
      <fieldset [disabled]="busy()">
        <legend>{{ language.t('Fragen auswählen') }}</legend>
        <div class="settings-grid">
          <label
            >{{ language.t('Katalog') }}
            <select [(ngModel)]="catalog" (ngModelChange)="invalidate()">
              <option value="all">{{ language.t('Alle eigenen Fragen') }}</option>
              <option value="none">{{ language.t('Ohne Katalog') }}</option>
              @for (item of catalogs(); track item.id) {
                <option [value]="item.id">{{ item.name }}</option>
              }
            </select>
          </label>
          <label
            >{{ language.t('Fach') }}<input [(ngModel)]="subject" (ngModelChange)="invalidate()"
          /></label>
          <label
            >{{ language.t('Thema') }}<input [(ngModel)]="topic" (ngModelChange)="invalidate()"
          /></label>
          <label
            >{{ language.t('Klasse / Zielgruppe')
            }}<input [(ngModel)]="audience" (ngModelChange)="invalidate()"
          /></label>
          <label
            >{{ language.t('Schlagwort / Themenhierarchie')
            }}<input [(ngModel)]="tags" (ngModelChange)="invalidate()"
          /></label>
          <label
            >{{ language.t('Schwierigkeitsgrad')
            }}<select [(ngModel)]="difficulty" (ngModelChange)="invalidate()">
              <option value="">{{ language.t('Alle') }}</option>
              <option value="unknown">{{ language.t('Unbekannt') }}</option>
              <option value="easy">{{ language.t('Leicht') }}</option>
              <option value="medium">{{ language.t('Mittel') }}</option>
              <option value="hard">{{ language.t('Schwer') }}</option>
            </select></label
          >
          <label
            >{{ language.t('Sprache')
            }}<input [(ngModel)]="questionLanguage" (ngModelChange)="invalidate()"
          /></label>
          <label
            >{{ language.t('Fragen suchen')
            }}<input type="search" [(ngModel)]="search" (ngModelChange)="invalidate()"
          /></label>
        </div>
        <div class="actions">
          <button type="button" class="secondary-action" (click)="choose(questions())">
            {{ language.t('Alle eigenen Fragen auswählen') }}
          </button>
          <button type="button" class="secondary-action" (click)="choose(filtered())">
            {{ language.t('Gefilterte Fragen auswählen') }}
          </button>
          <button type="button" class="secondary-action" (click)="choose([])">
            {{ language.t('Auswahl leeren') }}
          </button>
        </div>
        <p role="status">
          {{ selected.size }} {{ language.t('Fragen ausgewählt (maximal 10000)') }}
        </p>
        <ul class="choices">
          @for (question of filtered(); track question.id) {
            <li>
              <label
                ><input
                  type="checkbox"
                  [checked]="selected.has(question.id)"
                  (change)="toggle(question.id)"
                />
                {{ question.prompt }}
                <small
                  >{{ question.subject }} · {{ question.topic }} · {{ question.language }} ·
                  {{ language.t(question.hasDraft ? 'Entwurf' : 'Gespeicherte Fassung') }}</small
                >
              </label>
            </li>
          }
        </ul>
        <label
          >{{ language.t('Pakettitel')
          }}<input [(ngModel)]="title" maxlength="256" (ngModelChange)="invalidate()"
        /></label>
        <label
          >{{ language.t('Herausgeber / Attributionsname')
          }}<input [(ngModel)]="publisher" maxlength="256" (ngModelChange)="invalidate()"
        /></label>
        <p>
          {{
            language.t(
              'Diese Metadaten stehen im ZIP. Verwende keine E-Mail-Adresse oder andere vertrauliche Angaben als Attributionsnamen.'
            )
          }}
        </p>
        <details class="workspace-disclosure">
          <summary>{{ language.t('Rechte eigener Originaltexte und Originalbilder') }}</summary>
          <p>
            {{
              language.t(
                'Die folgenden Angaben gelten ausschließlich für eigene Originalinhalte. Importierte Originalfragen behalten ihre tatsächlichen Einzellizenzen und Quellen. Bearbeitete Importfragen und Drittquellen benötigen gespeicherte Einzelnachweise unter Quellen und Rechte je Frage und Bild.'
              )
            }}
          </p>
          <label
            >{{ language.t('Lizenz eigener Texte')
            }}<input [(ngModel)]="questionLicense" maxlength="120" (ngModelChange)="invalidate()"
          /></label>
          <label
            >{{ language.t('Rechteinhaber der Texte')
            }}<input [(ngModel)]="questionHolder" maxlength="256" (ngModelChange)="invalidate()"
          /></label>
          <label
            >{{ language.t('Attribution der Texte')
            }}<textarea
              [(ngModel)]="questionAttribution"
              maxlength="2048"
              (ngModelChange)="invalidate()"
            ></textarea>
          </label>
          <label
            >{{ language.t('Lizenz eigener Bilder')
            }}<input [(ngModel)]="imageLicense" maxlength="120" (ngModelChange)="invalidate()"
          /></label>
          <label
            >{{ language.t('Rechteinhaber der Bilder')
            }}<input [(ngModel)]="imageHolder" maxlength="256" (ngModelChange)="invalidate()"
          /></label>
          <label
            >{{ language.t('Attribution der Bilder')
            }}<textarea
              [(ngModel)]="imageAttribution"
              maxlength="2048"
              (ngModelChange)="invalidate()"
            ></textarea>
          </label>
          <label
            >{{ language.t('Lizenztexte, Lizenzverweise und Nutzungsbedingungen')
            }}<textarea
              [(ngModel)]="licenseNotice"
              maxlength="32000"
              rows="5"
              (ngModelChange)="invalidate()"
            ></textarea>
          </label>
          <p>
            {{
              language.t(
                'LicenseRef-Private ist keine offene Lizenz. Der private Export verlangt keine Community-Freigabe. Eine andere Lizenz darf nur bewusst und mit den nötigen Rechten gewählt werden; die technische Prüfung bestätigt keine rechtliche Zulässigkeit.'
              )
            }}
          </p>
        </details>
        <button
          type="button"
          class="secondary-action"
          [disabled]="!selected.size || selected.size > 10000"
          (click)="inspect()"
        >
          {{ language.t('Exportvorschau prüfen') }}
        </button>
      </fieldset>
      @if (preview(); as item) {
        <h3>{{ language.t('Exportvorschau') }}</h3>
        <h4>{{ language.t('Rechtebericht für offene Weitergabe') }}</h4>
        @if (item.rightsReport?.length) {
          <ul>
            @for (issue of item.rightsReport; track $index) {
              <li>{{ issue }}</li>
            }
          </ul>
        } @else {
          <p>
            {{
              language.t(
                'Keine technischen Nachweislücken erkannt. Die rechtliche Zulässigkeit muss trotzdem geprüft werden.'
              )
            }}
          </p>
        }
        @if (!item.communityEnabled) {
          <p>
            {{
              language.t(
                'Community-Export ist auf dieser Instanz administrativ gesperrt. Private Weitergabe bleibt verfügbar.'
              )
            }}
          </p>
        }
        <label
          >{{ language.t('Exportzweck') }}
          <select
            [(ngModel)]="purpose"
            [disabled]="busy()"
            (ngModelChange)="confirmed = false; publicationConfirmed = false"
          >
            <option value="private">{{ language.t('Privater Austausch') }}</option>
            <option value="community" [disabled]="!item.communityEligible">
              {{ language.t('Paket für einen Community-Vorschlag') }}
            </option>
          </select>
        </label>
        @if (purpose === 'community') {
          <p>
            {{
              language.t(
                'Eigene Originaltexte können ausdrücklich unter CC BY-SA 4.0 stehen. Andere dürfen offene Inhalte unter den Lizenzbedingungen kopieren und bearbeiten; eine bereits erteilte offene Lizenz ist nicht widerrufbar. Fremde Einzellizenzen bleiben erhalten. Dieser Download veröffentlicht oder versendet nichts.'
              )
            }}
          </p>
          <label
            ><input type="checkbox" [(ngModel)]="publicationConfirmed" [disabled]="busy()" />{{
              language.t(
                'Ich bestätige die Nutzungsrechte, die offene Weitergabe und ihre Folgen für diese geprüfte Auswahl.'
              )
            }}</label
          >
        }
        <p>
          {{ item.questionCount }} {{ language.t('Fragen') }} · {{ item.mediaCount }}
          {{ language.t('Medien') }} · {{ size(item.archiveBytes) }} ·
          {{ language.t('Formatversion') }} {{ item.schemaVersion }}
        </p>
        <p>{{ item.topics.join(', ') }}</p>
        <details class="workspace-disclosure">
          <summary>{{ language.t('Lizenzen, Quellen und Attribution prüfen') }}</summary>
          <ul>
            @for (license of item.licenses; track $index) {
              <li>{{ license.id }} · {{ license.holder }} · {{ license.attribution }}</li>
            }
          </ul>
          @for (name of noticeNames; track name) {
            <h4>{{ name }}</h4>
            <pre>{{ item.notices[name] }}</pre>
          }
        </details>
        <label
          ><input type="checkbox" [(ngModel)]="confirmed" [disabled]="busy()" />{{
            language.t(
              'Ich bestätige die Exportrechte. Die als eigene Originale ausgezeichneten Texte und Bilder stammen von mir und dürfen mit diesen Angaben exportiert werden.'
            )
          }}</label
        >
        <button
          type="button"
          class="primary-action"
          [disabled]="
            busy() ||
            !confirmed ||
            (purpose === 'community' && (!publicationConfirmed || !item.communityEligible))
          "
          (click)="download()"
        >
          {{ language.t('LearnPip-Paket (.zip) herunterladen') }}
        </button>
      }
      @if (message()) {
        <p role="status">{{ message() }}</p>
      }
    </section>
  `,
  styles: `
    label {
      display: block;
      margin-block: 0.8rem;
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
    .actions {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
    }
    .choices {
      max-height: 24rem;
      overflow: auto;
      padding: 0;
      list-style: none;
    }
    small {
      display: block;
    }
    pre {
      white-space: pre-wrap;
      overflow-wrap: anywhere;
    }
  `,
})
export class CatalogPackageExport implements OnInit {
  readonly language = inject(LanguageService);
  readonly busy = signal(false);
  readonly message = signal('');
  readonly questions = signal<Candidate[]>([]);
  readonly catalogs = signal<{ id: string; name: string }[]>([]);
  readonly preview = signal<Preview | null>(null);
  readonly noticeNames = ['LICENSES.md', 'NOTICE', 'ATTRIBUTION'];
  selected = new Set<string>();
  catalog = 'all';
  subject = '';
  audience = '';
  tags = '';
  difficulty = '';
  topic = '';
  questionLanguage = '';
  search = '';
  title = '';
  publisher = '';
  questionLicense = 'LicenseRef-Private';
  questionHolder = '';
  questionAttribution = '';
  imageLicense = 'LicenseRef-Private';
  imageHolder = '';
  imageAttribution = '';
  licenseNotice =
    'LicenseRef-Private: ausschließlich private Nutzung durch berechtigte Empfänger. Keine öffentliche Weiterverbreitung oder offene Lizenz.';
  confirmed = false;
  publicationConfirmed = false;
  purpose = 'private';

  ngOnInit(): void {
    void this.reload();
  }
  invalidate(): void {
    this.preview.set(null);
    this.confirmed = false;
    this.publicationConfirmed = false;
    this.purpose = 'private';
    this.message.set('');
  }
  filtered(): Candidate[] {
    const matches = (value: string, filter: string): boolean =>
      value.toLocaleLowerCase().includes(filter.trim().toLocaleLowerCase());
    return this.questions().filter(
      (item) =>
        (this.catalog === 'all' ||
          (this.catalog === 'none'
            ? (item.catalogIds ?? (item.catalogId ? [item.catalogId] : [])).length === 0
            : (item.catalogIds ?? (item.catalogId ? [item.catalogId] : [])).includes(
                this.catalog,
              ))) &&
        matches(item.audience ?? '', this.audience) &&
        matches((item.tags ?? []).join(' '), this.tags) &&
        (!this.difficulty || item.difficulty === this.difficulty) &&
        matches(item.subject, this.subject) &&
        matches(item.topic, this.topic) &&
        matches(item.language, this.questionLanguage) &&
        matches(item.prompt, this.search),
    );
  }
  choose(items: Candidate[]): void {
    this.selected = new Set(items.map((item) => item.id));
    this.invalidate();
  }
  toggle(id: string): void {
    if (this.selected.has(id)) this.selected.delete(id);
    else this.selected.add(id);
    this.invalidate();
  }
  size(bytes: number): string {
    return (
      new Intl.NumberFormat(this.language.current(), { maximumFractionDigits: 2 }).format(
        bytes / (1024 * 1024),
      ) + ' MiB'
    );
  }
  async reload(): Promise<void> {
    if (this.busy()) return;
    this.invalidate();
    await this.perform(async () => {
      const [questions, catalogs] = await Promise.all([
        fetch('/api/v1/catalog-exports/questions', { credentials: 'same-origin' }),
        fetch('/api/v1/catalogs/', { credentials: 'same-origin' }),
      ]);
      if (!questions.ok || !catalogs.ok)
        throw new Error(
          this.language.t('Auswahl konnte nicht geladen werden. Bitte Anmeldung prüfen.'),
        );
      this.questions.set(((await questions.json()) as { data: Candidate[] }).data);
      this.catalogs.set(((await catalogs.json()) as { data: { id: string; name: string }[] }).data);
      this.selected = new Set(
        [...this.selected].filter((id) => this.questions().some((item) => item.id === id)),
      );
    });
  }
  async inspect(): Promise<void> {
    if (this.busy() || !this.selected.size || this.selected.size > 10000) return;
    this.invalidate();
    await this.perform(async () => {
      this.preview.set(((await (await this.request('preview')).json()) as { data: Preview }).data);
      this.message.set(
        this.language.t(
          'Export geprüft. Bitte Inhalte, Lizenzen und Attribution vor dem Download kontrollieren.',
        ),
      );
    });
  }
  async download(): Promise<void> {
    if (
      this.busy() ||
      !this.confirmed ||
      !this.preview() ||
      (this.purpose === 'community' &&
        (!this.publicationConfirmed || !this.preview()?.communityEligible))
    )
      return;
    await this.perform(async () => {
      const response = await this.request('download');
      const url = URL.createObjectURL(await response.blob());
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = 'learnpip-selection.zip';
      anchor.click();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
      this.message.set(
        this.language.t(
          'ZIP heruntergeladen. Auf der anderen Instanz unter Fragen importieren oder Kataloge und Inhalte auswählen.',
        ),
      );
    });
  }
  private async request(action: 'preview' | 'download'): Promise<Response> {
    const response = await fetch('/api/v1/catalog-exports/' + action, {
      method: 'POST',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        purpose: this.purpose,
        publicationConfirmed: action === 'download' && this.publicationConfirmed,
        questionIds: [...this.selected].sort(),
        title: this.title,
        publisher: this.publisher,
        questionLicense: {
          id: this.questionLicense,
          holder: this.questionHolder,
          attribution: this.questionAttribution,
        },
        imageLicense: {
          id: this.imageLicense,
          holder: this.imageHolder,
          attribution: this.imageAttribution,
        },
        licenseNotice: this.licenseNotice,
        rightsConfirmed: action === 'download' && this.confirmed,
        previewSha256: action === 'download' ? this.preview()?.previewSha256 : null,
      }),
    });
    if (!response.ok) {
      const error = (await response.json().catch(() => null)) as {
        errors?: Record<string, string[]>;
        message?: string;
        detail?: string;
      } | null;
      throw new Error(
        error?.errors?.['export']?.[0] ??
          error?.message ??
          error?.detail ??
          this.language.t('Export fehlgeschlagen. Bitte Auswahl und Rechteangaben prüfen.'),
      );
    }
    return response;
  }
  private async perform(action: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    this.message.set(this.language.t('Paket wird verarbeitet …'));
    try {
      await action();
    } catch (error) {
      this.preview.set(null);
      this.confirmed = false;
      this.message.set(
        error instanceof Error
          ? error.message
          : this.language.t('Export fehlgeschlagen. Bitte Auswahl und Rechteangaben prüfen.'),
      );
    } finally {
      this.busy.set(false);
    }
  }
}
