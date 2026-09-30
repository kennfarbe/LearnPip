import { Component, inject, OnInit, signal } from '@angular/core';
import { LanguageService } from './language';

interface AccountInfo {
  lastActivityAtUtc: string;
  disabledAtUtc: string | null;
}

@Component({
  selector: 'app-account-activity',
  template: `
    @if (account(); as info) {
      <section class="account-activity" aria-labelledby="account-activity-title">
        <h2 id="account-activity-title">{{ language.t('Dein Konto') }}</h2>
        <p>
          {{ language.t('Letzte Aktivität vor diesem Seitenaufruf:')
          }}<strong>{{ dateLabel(info.lastActivityAtUtc) }}</strong>
        </p>
        <p>
          {{
            language.t(
              'Erfolgreiche angemeldete API-Aufrufe halten dein Konto aktiv. Nach 60, 76 und 87 Tagen ohne Aktivität erhältst du eine E-Mail-Erinnerung, wenn eine Adresse verknüpft und der Mailversand eingerichtet ist. Nach 90 Tagen wird das Konto deaktiviert. Weitere 90 Tage später wird es gelöscht.'
            )
          }}
        </p>
        <p>
          {{
            language.t(
              'Bis zur Löschung kannst du dich mit deinem Wiederherstellungsgeheimnis, deiner verknüpften E-Mail oder OIDC erneut anmelden und das Konto reaktivieren.'
            )
          }}
        </p>
        <p>
          {{
            language.current() === 'en'
              ? 'Publicly licensed copies already obtained by others cannot be recalled. Private account content is deleted immediately; backups expire after 30 days.'
              : 'Bereits von anderen übernommene öffentlich lizenzierte Kopien lassen sich nicht zurückholen. Private Kontodaten werden sofort gelöscht; Backups laufen nach 30 Tagen ab.'
          }}
        </p>
        <a href="/api/v1/account/export" download="learnpip-export.json">
          {{
            language.current() === 'en'
              ? 'Download my data (JSON)'
              : 'Meine Daten herunterladen (JSON)'
          }}
        </a>
        <p>
          {{
            language.current() === 'en'
              ? 'Images can be downloaded separately from the private media view.'
              : 'Bilder kannst du separat aus der privaten Medienansicht herunterladen.'
          }}
        </p>
        <label
          >{{
            language.current() === 'en'
              ? 'Recovery secret for deletion'
              : 'Wiederherstellungsgeheimnis zur Löschung'
          }}
          <input
            type="password"
            [value]="recoverySecret"
            (input)="recoverySecret = $any($event.target).value"
            autocomplete="off"
          />
        </label>
        <button type="button" (click)="deleteAccount()">
          {{
            language.current() === 'en' ? 'Delete account permanently' : 'Konto endgültig löschen'
          }}
        </button>
        @if (message()) {
          <p role="status">{{ message() }}</p>
        }
        <button type="button" (click)="refresh()">
          {{ language.t('Aktivität aktualisieren') }}
        </button>
      </section>
    }
  `,
  styles: `
    :host {
      display: block;
    }
    .account-activity {
      margin-top: 3rem;
      padding: clamp(1rem, 3vw, 2rem);
      border: 1px solid #dfe9df;
      border-radius: 1rem;
      background: #f8fbf7;
      color: #1d3a32;
    }
    h2 {
      margin: 0 0 0.6rem;
      font-size: clamp(1.5rem, 4vw, 1.9rem);
    }
    p {
      max-width: 56rem;
      line-height: 1.6;
    }
    input {
      display: block;
      max-width: 30rem;
      width: 100%;
      padding: 0.65rem;
      margin: 0.5rem 0 1rem;
    }
    button {
      min-height: 2.75rem;
      padding: 0.6rem 0.8rem;
      border: 1px solid #205d45;
      border-radius: 0.5rem;
      background: white;
      color: #205d45;
      font: inherit;
      font-weight: 600;
      cursor: pointer;
    }
    button:focus-visible {
      outline: 3px solid #e5a44c;
      outline-offset: 2px;
    }
  `,
})
export class AccountActivity implements OnInit {
  readonly language = inject(LanguageService);
  readonly account = signal<AccountInfo | null>(null);
  readonly message = signal('');
  recoverySecret = '';

  ngOnInit(): void {
    void this.refresh();
  }

  async refresh(): Promise<void> {
    try {
      const response = await fetch('/api/v1/auth/me', { credentials: 'same-origin' });
      if (response.ok) this.account.set(((await response.json()) as { data: AccountInfo }).data);
      else this.account.set(null);
    } catch {
      this.account.set(null);
    }
  }

  async deleteAccount(): Promise<void> {
    const confirmed = window.prompt(
      this.language.current() === 'en'
        ? 'This deletes your private content and ends all sessions. Type DELETE to confirm.'
        : 'Dies löscht deine privaten Daten und beendet alle Sitzungen. Tippe zur Bestätigung DELETE.',
    );
    if (confirmed !== 'DELETE') return;
    try {
      const response = await fetch('/api/v1/account/delete', {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ confirmation: confirmed, recoverySecret: this.recoverySecret }),
      });
      this.recoverySecret = '';
      if (!response.ok) throw new Error(`${response.status}`);
      this.account.set(null);
      window.location.assign('/');
    } catch {
      this.recoverySecret = '';
      this.message.set(
        this.language.current() === 'en'
          ? 'Deletion failed. Check your recovery secret.'
          : 'Löschung fehlgeschlagen. Prüfe dein Wiederherstellungsgeheimnis.',
      );
    }
  }

  dateLabel(value: string): string {
    return new Date(value).toLocaleString('de-DE');
  }
}
