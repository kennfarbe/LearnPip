import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LanguageService } from './language';

@Component({
  selector: 'app-exam-administration',
  imports: [FormsModule],
  template: `
    <section class="workspace-card" aria-labelledby="exam-administration-title">
      <h2 id="exam-administration-title">
        {{ language.t('Offiziellen Katalog und Profilfassungen verwalten') }}
      </h2>
      <p>
        {{
          language.t(
            'JSON-Import mit Quelle, Lizenz, Urheberangabe, Fassungsdatum und bestätigten Rechten.'
          )
        }}
      </p>
      <label
        >{{ language.t('Katalogimport (JSON)')
        }}<textarea [(ngModel)]="catalogJson" rows="8"></textarea>
      </label>
      <button
        type="button"
        class="primary-action"
        [disabled]="busy()"
        (click)="submit('catalogs/import', catalogJson)"
      >
        {{ language.t('Katalogfassung importieren') }}
      </button>
      <label
        >{{ language.t('Profilfassung (JSON)')
        }}<textarea [(ngModel)]="profileJson" rows="8"></textarea>
      </label>
      <button
        type="button"
        class="primary-action"
        [disabled]="busy()"
        (click)="submit('profiles/versions', profileJson)"
      >
        {{ language.t('Profilfassung speichern') }}
      </button>
      @if (message()) {
        <p role="status">{{ message() }}</p>
      }
    </section>
  `,
})
export class ExamAdministration {
  readonly language = inject(LanguageService);
  readonly busy = signal(false);
  readonly message = signal('');
  catalogJson = '';
  profileJson = '';

  async submit(path: 'catalogs/import' | 'profiles/versions', raw: string): Promise<void> {
    if (this.busy()) return;
    this.busy.set(true);
    try {
      const payload: unknown = JSON.parse(raw);
      const response = await fetch(`/api/v1/exams/admin/${path}`, {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });
      if (!response.ok) throw new Error(`${response.status}`);
      this.message.set(this.language.t('Neue Fassung gespeichert.'));
    } catch {
      this.message.set(
        this.language.t('Import fehlgeschlagen. Bitte JSON und Berechtigungen prüfen.'),
      );
    } finally {
      this.busy.set(false);
    }
  }
}
