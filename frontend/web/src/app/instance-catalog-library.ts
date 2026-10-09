import { Component, inject, input, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LanguageService } from './language';

interface Package {
  id: string;
  packageId: string;
  catalogVersion: string;
  archiveSha256: string;
  publisher?: string;
  audiences?: string[];
  sourceUrls?: string[];
  title: string;
  description: string;
  language: string;
  sourceRevision: string;
  questionCount: number;
  archiveBytes: number;
  expandedBytes: number;
  available: boolean;
  notices: Record<string, string>;
}
interface Preview {
  package: Package;
  previousId: string | null;
  previousVersion: string | null;
  previousSourceRevision: string | null;
  previousSha256: string | null;
  newQuestions: number;
  removedQuestions: number;
  sharedQuestions: number;
  changedQuestions: number;
  unchangedQuestions: number;
}

@Component({
  selector: 'app-instance-catalog-library',
  imports: [FormsModule],
  template: `
    <section
      class="workspace-card"
      aria-labelledby="instance-package-title"
      [attr.aria-busy]="busy()"
    >
      <h2 id="instance-package-title">
        {{
          language.t(
            admin()
              ? 'Optionale Lernpakete der Instanz verwalten'
              : 'Verfügbare optionale Lernpakete'
          )
        }}
      </h2>
      <p>
        {{
          language.t(
            'Die Anwendung funktioniert auch ohne Lernpakete. Es wird nichts automatisch ausgewählt oder heruntergeladen. Private Fragen und Lernstände bleiben unabhängig von der Paketfreigabe erhalten.'
          )
        }}
      </p>
      <button type="button" class="secondary-action" [disabled]="busy()" (click)="reload()">
        {{ language.t('Paketliste aktualisieren') }}
      </button>
      @if (admin()) {
        @if (sources().length) {
          <p>
            {{
              language.t(
                'Konfigurierte Paketquellen. Ein Download verbindet diese Instanz mit dem externen Anbieter. Erst die anschließende Vorschau und Bestätigung stellt Inhalte bereit.'
              )
            }}
          </p>
          @for (source of sources(); track source.id) {
            <button
              type="button"
              class="secondary-action"
              [disabled]="busy()"
              (click)="downloadSource(source.id)"
            >
              {{ source.title }} — {{ language.t('Paket von Quelle herunterladen und prüfen') }}
            </button>
          }
        }
        <label
          >{{ language.t('Lokales Lernpaket auswählen (.zip, maximal 25 MiB)') }}
          <input
            type="file"
            accept=".zip,application/zip"
            [disabled]="busy()"
            (change)="select($event)"
          />
        </label>
        <button
          type="button"
          class="secondary-action"
          [disabled]="busy() || !file"
          (click)="inspect()"
        >
          {{ language.t('Paketfassung und Änderungen prüfen') }}
        </button>
        @if (preview(); as item) {
          <h3>{{ item.package.title }} · {{ item.package.catalogVersion }}</h3>
          <p>{{ language.t('Bisherige Fassung') }}: {{ item.previousVersion ?? '—' }}</p>
          <p>
            {{ language.t('Neue / geänderte / entfallene / unveränderte Fragen') }}:
            {{ item.newQuestions }} / {{ item.changedQuestions }} / {{ item.removedQuestions }} /
            {{ item.unchangedQuestions }}
          </p>
          <p>
            {{
              language.t(
                'Änderungen umfassen auch Antworten, Bilder, Quellen und Rechteangaben. Alle Nachweise der neuen Fassung müssen geprüft werden; persönliche Kopien werden nicht überschrieben.'
              )
            }}
          </p>
          <p>
            {{ language.t('Quellenstand bisher / neu') }}:
            {{ item.previousSourceRevision ?? '—' }} / {{ item.package.sourceRevision }}
          </p>
          <p>
            {{ item.package.publisher }} · Zielgruppe:
            {{ item.package.audiences?.join(', ') || 'Keine Angabe' }} ·
            {{ item.package.description }} · {{ item.package.language }} ·
            {{ item.package.sourceRevision }}
          </p>
          <p>
            {{ item.package.questionCount }} {{ language.t('Fragen') }} ·
            {{ size(item.package.archiveBytes) }} / {{ size(item.package.expandedBytes) }}
          </p>
          <details>
            <summary>{{ language.t('Lizenz- und Quellenangaben') }}</summary>
            @for (name of noticeNames; track name) {
              <h4>{{ name }}</h4>
              <pre>{{ item.package.notices[name] }}</pre>
            }
          </details>
          <label
            ><input type="checkbox" [(ngModel)]="confirmed" [disabled]="busy()" />{{
              language.t(
                'Ich habe Inhalte, Lizenz- und Weitergaberechte geprüft. Das Paket darf für Lernende dieser Instanz bereitgestellt werden und enthält keine vertraulichen Kontodaten. Die neue Fassung ersetzt ausschließlich die Paketfreigabe.'
              )
            }}</label
          >
          <button
            type="button"
            class="secondary-action"
            [disabled]="busy() || !confirmed"
            (click)="install()"
          >
            {{ language.t('Paketfassung bereitstellen') }}
          </button>
        }
      }
      <ul>
        @for (item of packages(); track item.id) {
          <li>
            <h3>{{ item.title }} · {{ item.catalogVersion }}</h3>
            <p>{{ item.description }} · {{ item.language }} · {{ item.sourceRevision }}</p>
            <p>
              {{ item.publisher }} · Zielgruppe: {{ item.audiences?.join(', ') || 'Keine Angabe' }}
            </p>
            @for (source of item.sourceUrls || []; track source) {
              <p>{{ source }}</p>
            }
            <p>
              {{ item.questionCount }} {{ language.t('Fragen') }} · {{ size(item.archiveBytes) }} /
              {{ size(item.expandedBytes) }}
            </p>
            <details>
              <summary>{{ language.t('Lizenz- und Quellenangaben') }}</summary>
              @for (name of noticeNames; track name) {
                <h4>{{ name }}</h4>
                <pre>{{ item.notices[name] }}</pre>
              }
            </details>
            @if (admin()) {
              <p>
                {{
                  language.t(
                    item.available ? 'Für Lernende verfügbar' : 'Deaktivierte Paketfassung'
                  )
                }}
              </p>
              <label
                ><input type="checkbox" [(ngModel)]="changes[item.id]" [disabled]="busy()" />{{
                  language.t(
                    'Ich bestätige den Freigabewechsel. Private Kopien und Lernstände bleiben erhalten. Andere Fassungen dieses Pakets werden bei Aktivierung deaktiviert.'
                  )
                }}</label
              >
              <button
                type="button"
                [disabled]="busy() || !changes[item.id]"
                (click)="availability(item)"
              >
                {{
                  language.t(
                    item.available
                      ? 'Paket deaktivieren / aus Angebot entfernen'
                      : 'Diese Fassung aktivieren'
                  )
                }}
              </button>
              <label
                ><input type="checkbox" [(ngModel)]="removals[item.id]" [disabled]="busy()" />{{
                  language.t(
                    'Ich bestätige die endgültige Entfernung dieser Instanzdatei. Private Kopien und Lernstände bleiben erhalten. Diese Inhaltsversion kann danach nicht erneut bereitgestellt werden.'
                  )
                }}</label
              >
              <button
                type="button"
                [disabled]="busy() || !removals[item.id]"
                (click)="remove(item)"
              >
                {{ language.t('Paketfassung endgültig entfernen') }}
              </button>
            } @else {
              <button
                type="button"
                class="secondary-action"
                [disabled]="busy()"
                (click)="choose(item)"
              >
                {{ language.t('Für privaten Import auswählen') }}
              </button>
            }
          </li>
        } @empty {
          <li>
            {{
              language.t(
                'Keine optionalen Paketfassungen vorhanden. Die Nutzung ohne Pakete bleibt möglich.'
              )
            }}
          </li>
        }
      </ul>
      @if (message()) {
        <p role="status">{{ message() }}</p>
      }
    </section>
  `,
  styles: `
    label {
      display: block;
      margin-block: 0.75rem;
      overflow-wrap: anywhere;
    }
    pre {
      white-space: pre-wrap;
      overflow-wrap: anywhere;
    }
    li {
      margin-block: 1.5rem;
      overflow-wrap: anywhere;
    }
  `,
})
export class InstanceCatalogLibrary implements OnInit {
  readonly language = inject(LanguageService);
  readonly admin = input(false);
  readonly chosen = output<File>();
  readonly packages = signal<Package[]>([]);
  readonly sources = signal<{ id: string; title: string; sha256: string }[]>([]);
  readonly preview = signal<Preview | null>(null);
  readonly busy = signal(false);
  readonly message = signal('');
  readonly noticeNames = ['LICENSES.md', 'NOTICE', 'ATTRIBUTION'];
  file: File | null = null;
  confirmed = false;
  changes: Record<string, boolean> = {};
  removals: Record<string, boolean> = {};

  ngOnInit(): void {
    void this.reload();
  }
  size(bytes: number): string {
    return (
      new Intl.NumberFormat(this.language.current(), { maximumFractionDigits: 2 }).format(
        bytes / 1024 / 1024,
      ) + ' MiB'
    );
  }
  select(event: Event): void {
    if (this.busy()) return;
    this.file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.preview.set(null);
    this.confirmed = false;
    this.message.set('');
  }
  async reload(): Promise<void> {
    await this.perform(async () => {
      await this.list();
      this.message.set('');
    });
  }
  async inspect(): Promise<void> {
    this.preview.set(null);
    this.confirmed = false;
    if (!this.file || !this.file.size || this.file.size > 25 * 1024 * 1024) {
      this.message.set(this.language.t('Bitte eine ZIP-Datei von maximal 25 MiB auswählen.'));
      return;
    }
    await this.perform(async () => {
      this.preview.set(
        (
          (await this.request('/admin/preview', { method: 'POST', body: this.form() })) as {
            data: Preview;
          }
        ).data,
      );
    });
  }
  async install(): Promise<void> {
    const preview = this.preview();
    if (!preview || !this.confirmed || !this.file) return;
    await this.perform(async () => {
      const form = this.form();
      form.append('archiveSha256', preview.package.archiveSha256);
      if (preview.previousSha256) form.append('previousSha256', preview.previousSha256);
      form.append('rightsConfirmed', 'true');
      await this.request('/admin/install', { method: 'POST', body: form });
      this.preview.set(null);
      this.confirmed = false;
      await this.list();
      this.message.set(
        this.language.t('Paketfassung bereitgestellt. Private Fragen wurden nicht geändert.'),
      );
    });
  }
  async availability(item: Package): Promise<void> {
    if (!this.changes[item.id]) return;
    await this.perform(async () => {
      await this.request('/admin/' + item.id + '/availability', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          available: !item.available,
          confirmed: true,
          archiveSha256: item.archiveSha256,
        }),
      });
      this.changes = {};
      await this.list();
      this.message.set(
        this.language.t('Paketfreigabe geändert. Private Kopien und Lernstände bleiben erhalten.'),
      );
    });
  }
  async remove(item: Package): Promise<void> {
    if (!this.removals[item.id]) return;
    await this.perform(async () => {
      await this.request('/admin/' + item.id, {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          available: false,
          confirmed: true,
          archiveSha256: item.archiveSha256,
        }),
      });
      this.removals = {};
      await this.list();
      this.message.set(
        this.language.t('Instanzdatei entfernt. Private Kopien und Lernstände bleiben erhalten.'),
      );
    });
  }
  async choose(item: Package): Promise<void> {
    await this.perform(async () => {
      const response = await fetch('/api/v1/instance-catalogs/' + item.id + '/archive', {
        credentials: 'same-origin',
      });
      if (!response.ok)
        throw new Error(
          this.language.t('Paket ist nicht mehr verfügbar. Bitte Liste aktualisieren.'),
        );
      const bytes = await response.arrayBuffer();
      const hash = [...new Uint8Array(await crypto.subtle.digest('SHA-256', bytes))]
        .map((byte) => byte.toString(16).padStart(2, '0'))
        .join('');
      if (hash !== item.archiveSha256)
        throw new Error(this.language.t('Prüfsumme stimmt nicht überein. Kein Import.'));
      this.chosen.emit(new File([bytes], 'learnpip-package.zip', { type: 'application/zip' }));
      this.message.set(
        this.language.t(
          'Paket ausgewählt. Bitte die private Importvorschau und Nutzungsrechte bestätigen.',
        ),
      );
    });
  }
  async downloadSource(id: string): Promise<void> {
    await this.perform(async () => {
      this.preview.set(null);
      this.confirmed = false;
      const response = await fetch(
        '/api/v1/instance-catalogs/admin/sources/' + encodeURIComponent(id) + '/download',
        { method: 'POST', credentials: 'same-origin' },
      );
      if (!response.ok) {
        const body = (await response.json().catch(() => null)) as {
          errors?: Record<string, string[]>;
        } | null;
        throw new Error(
          body?.errors?.['package']?.[0] ??
            this.language.t('Paket konnte nicht heruntergeladen werden.'),
        );
      }
      this.file = new File([await response.blob()], 'learnpip-source-package.zip', {
        type: 'application/zip',
      });
      this.preview.set(
        (
          (await this.request('/admin/preview', { method: 'POST', body: this.form() })) as {
            data: Preview;
          }
        ).data,
      );
      this.message.set(
        this.language.t(
          'Download geprüft. Bitte Inhalte und Rechte vor der Bereitstellung kontrollieren.',
        ),
      );
    });
  }
  private form(): FormData {
    const form = new FormData();
    form.append('file', this.file!);
    return form;
  }
  private async list(): Promise<void> {
    if (this.admin())
      this.sources.set(
        (
          (await this.request('/admin/sources/')) as {
            data: { id: string; title: string; sha256: string }[];
          }
        ).data,
      );
    this.packages.set(
      ((await this.request(this.admin() ? '/admin/' : '/')) as { data: Package[] }).data,
    );
  }
  private async request(path: string, options: RequestInit = {}): Promise<unknown> {
    const response = await fetch('/api/v1/instance-catalogs' + path, {
      credentials: 'same-origin',
      ...options,
    });
    if (response.status === 204) return null;
    const body = (await response.json().catch(() => null)) as {
      errors?: Record<string, string[]>;
      message?: string;
      detail?: string;
    } | null;
    if (!response.ok)
      throw new Error(
        body?.errors?.['package']?.[0] ??
          body?.message ??
          body?.detail ??
          this.language.t('Paket konnte nicht verarbeitet werden.'),
      );
    return body;
  }
  private async perform(action: () => Promise<void>): Promise<void> {
    if (this.busy()) return;
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
