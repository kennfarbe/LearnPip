import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LanguageService } from './language';
interface License {
  id: string;
  holder: string;
  attribution: string;
}
interface Preview {
  title: string;
  catalogVersion: string;
  schemaVersion: string;
  sourceRevision: string;
  language: string;
  questionCount: number;
  mediaCount: number;
  license: License;
  questionLicenses: License[];
  notices: Record<string, string>;
  topics: string[];
  archiveSha256: string;
  state: 'new' | 'identical' | 'conflict';
  catalogId: string | null;
}
interface Imported {
  id: string;
  packageId: string;
  catalogVersion: string;
}
@Component({
  selector: 'app-catalog-package-import',
  imports: [FormsModule, RouterLink],
  template: `
    <section class="workspace-card" aria-labelledby="package-title" [attr.aria-busy]="busy()">
      <h2 id="package-title">{{ language.t('Fragenpaket importieren') }}</h2>
      <p>
        {{
          language.t(
            'Wähle eine lokale LearnPip-ZIP-Datei. Der Import bleibt privat und benötigt keinen externen Dienst.'
          )
        }}
      </p>
      <form class="settings-grid" (ngSubmit)="inspect()">
        <label for="package-file">{{ language.t('LearnPip-Paket (.zip, maximal 25 MiB)') }}</label>
        <input
          id="package-file"
          type="file"
          accept=".zip,application/zip"
          [disabled]="busy()"
          (change)="select($event)"
        />
        <button class="secondary-action" type="submit" [disabled]="busy() || !file">
          {{ language.t('Importvorschau prüfen') }}
        </button>
      </form>
      @if (preview(); as item) {
        <article>
          <h3>{{ item.title }}</h3>
          <dl>
            <dt>{{ language.t('Paketversion') }}</dt>
            <dd>{{ item.catalogVersion }}</dd>
            <dt>{{ language.t('Formatversion') }}</dt>
            <dd>{{ item.schemaVersion }}</dd>
            <dt>{{ language.t('Quellenstand') }}</dt>
            <dd>{{ item.sourceRevision }}</dd>
            <dt>{{ language.t('Sprache') }}</dt>
            <dd>{{ item.language }}</dd>
            <dt>{{ language.t('Inhalt') }}</dt>
            <dd>
              {{ item.questionCount }} {{ language.t('Fragen') }}, {{ item.mediaCount }}
              {{ language.t('Medien') }}
            </dd>
            <dt>{{ language.t('Themen') }}</dt>
            <dd>{{ item.topics.join(', ') }}</dd>
          </dl>
          <details class="workspace-disclosure">
            <summary>{{ language.t('Lizenzen, Quellen und Attribution prüfen') }}</summary>
            <p>
              {{ item.license.id }} · {{ item.license.holder }} · {{ item.license.attribution }}
            </p>
            <ul>
              @for (license of item.questionLicenses; track $index) {
                <li>{{ license.id }} · {{ license.holder }} · {{ license.attribution }}</li>
              }
            </ul>
            @for (notice of noticeNames; track notice) {
              <h4>{{ notice }}</h4>
              <pre>{{ item.notices[notice] }}</pre>
            }
          </details>
          @if (item.state === 'conflict') {
            <p role="alert">
              {{
                language.t(
                  'Dieses Paket wurde geändert. Der Import ist gesperrt, damit vorhandene Fragen und Lernstände erhalten bleiben. Kontrollierte Paketupdates sind noch nicht verfügbar.'
                )
              }}
            </p>
          } @else if (item.state === 'identical') {
            <p role="status">
              {{
                language.t(
                  'Dieses Paket wurde bereits importiert. Es werden keine Duplikate angelegt; eigene Änderungen bleiben erhalten.'
                )
              }}
            </p>
          } @else {
            <p>
              {{
                language.t(
                  'Alle Fragen werden in einem neuen privaten Katalog angelegt. Es wird nichts veröffentlicht. Originalpaket und Anzeigebilder belegen Speicherplatz; Bilder werden für die Anzeige bereinigt.'
                )
              }}
            </p>
            <label
              ><input
                type="checkbox"
                name="package-rights"
                [(ngModel)]="confirmed"
                [disabled]="busy()"
              />{{
                language.t(
                  'Ich habe die Lizenzangaben geprüft und darf diese Inhalte privat nutzen.'
                )
              }}</label
            >
            <button
              type="button"
              class="primary-action"
              [disabled]="busy() || !confirmed"
              (click)="importPackage()"
            >
              {{ language.t('Import bestätigen') }}
            </button>
          }
        </article>
      }
      @if (catalogId()) {
        <a
          class="secondary-action"
          routerLink="/questions"
          [queryParams]="{ catalog: catalogId() }"
          >{{ language.t('Importierte Fragen anzeigen') }}</a
        >
      }
      @if (message()) {
        <p role="status">{{ message() }}</p>
      }
      @if (imports().length) {
        <h3>{{ language.t('Importierte Originalpakete') }}</h3>
        <p>
          {{
            language.t(
              'Der Download enthält das unveränderte Originalpaket mit sämtlichen Lizenz- und Quellenangaben. Spätere Änderungen in LearnPip sind darin nicht enthalten.'
            )
          }}
        </p>
        <ul>
          @for (item of imports(); track item.id) {
            <li>
              {{ item.packageId }} · {{ item.catalogVersion }} —
              <a [href]="'/api/v1/catalog-packages/' + item.id + '/original'">{{
                language.t('Originalpaket herunterladen')
              }}</a>
            </li>
          }
        </ul>
      }
    </section>
  `,
  styles: `
    section {
      margin-block: 1.5rem;
    }
    pre {
      white-space: pre-wrap;
      overflow-wrap: anywhere;
      max-width: 100%;
    }
    dd {
      margin: 0 0 0.6rem;
      overflow-wrap: anywhere;
    }
    dt {
      font-weight: 600;
    }
    article {
      margin-block: 1rem;
    }
    label {
      display: block;
      margin-block: 0.75rem;
    }
    li {
      overflow-wrap: anywhere;
    }
  `,
})
export class CatalogPackageImport implements OnInit {
  readonly language = inject(LanguageService);
  readonly busy = signal(false);
  readonly preview = signal<Preview | null>(null);
  readonly message = signal('');
  readonly catalogId = signal<string | null>(null);
  readonly imports = signal<Imported[]>([]);
  readonly noticeNames = ['LICENSES.md', 'NOTICE', 'ATTRIBUTION'];
  file: File | null = null;
  confirmed = false;
  ngOnInit(): void {
    void this.reload();
  }
  select(event: Event): void {
    if (this.busy()) return;
    this.file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.preview.set(null);
    this.message.set('');
    this.catalogId.set(null);
    this.confirmed = false;
  }
  async inspect(): Promise<void> {
    if (!this.file || this.busy()) return;
    this.preview.set(null);
    this.catalogId.set(null);
    this.confirmed = false;
    if (this.file.size > 25 * 1024 * 1024 || !this.file.size) {
      this.message.set(this.language.t('Bitte eine ZIP-Datei von maximal 25 MiB auswählen.'));
      return;
    }
    await this.perform(async () => {
      const response = await this.request('preview');
      const item = ((await response.json()) as { data: Preview }).data;
      this.preview.set(item);
      this.catalogId.set(item.catalogId);
      this.message.set(
        this.language.t('Paket geprüft. Bitte die Vorschau und Lizenzangaben lesen.'),
      );
    });
  }
  async importPackage(): Promise<void> {
    const item = this.preview();
    if (!this.file || !item || !this.confirmed || item.state !== 'new' || this.busy()) return;
    await this.perform(async () => {
      const response = await this.request('import', item.archiveSha256);
      const result = ((await response.json()) as { data: { catalogId: string | null } }).data;
      this.catalogId.set(result.catalogId);
      this.preview.set({ ...item, state: 'identical', catalogId: result.catalogId });
      this.message.set(
        this.language.t('Fragenpaket privat importiert. Es wurde nichts veröffentlicht.'),
      );
      await this.reload();
    });
  }
  private async request(action: string, hash?: string): Promise<Response> {
    const form = new FormData();
    form.append('file', this.file!);
    if (hash) {
      form.append('archiveSha256', hash);
      form.append('rightsConfirmed', 'true');
    }
    const response = await fetch('/api/v1/catalog-packages/' + action, {
      method: 'POST',
      credentials: 'same-origin',
      body: form,
    });
    if (!response.ok) {
      const error = (await response.json().catch(() => null)) as {
        errors?: Record<string, string[]>;
        detail?: string;
        message?: string;
      } | null;
      throw new Error(
        error?.errors?.['package']?.[0] ??
          error?.detail ??
          error?.message ??
          this.language.t(
            'Paket konnte nicht verarbeitet werden. Bitte Anmeldung und Datei prüfen.',
          ),
      );
    }
    return response;
  }
  private async reload(): Promise<void> {
    try {
      const response = await fetch('/api/v1/catalog-packages/', { credentials: 'same-origin' });
      if (response.ok) this.imports.set(((await response.json()) as { data: Imported[] }).data);
    } catch {
      /* An optional list can be retried after importing. */
    }
  }
  private async perform(action: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    this.message.set(this.language.t('Paket wird verarbeitet …'));
    try {
      await action();
    } catch (error) {
      this.message.set(
        error instanceof Error
          ? error.message
          : this.language.t('Paket konnte nicht verarbeitet werden.'),
      );
    } finally {
      this.busy.set(false);
    }
  }
}
