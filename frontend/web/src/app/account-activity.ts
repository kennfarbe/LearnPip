import { Component, OnInit, signal } from '@angular/core';

interface AccountInfo {
  lastActivityAtUtc: string;
  disabledAtUtc: string | null;
}

@Component({
  selector: 'app-account-activity',
  template: `
    @if (account(); as info) {
      <section class="account-activity" aria-labelledby="account-activity-title">
        <h2 id="account-activity-title">Dein Konto</h2>
        <p>
          Letzte Aktivität vor diesem Seitenaufruf:
          <strong>{{ dateLabel(info.lastActivityAtUtc) }}</strong>
        </p>
        <p>
          Erfolgreiche angemeldete API-Aufrufe halten dein Konto aktiv. Nach 60, 76 und 87 Tagen
          ohne Aktivität erhältst du eine E-Mail-Erinnerung, wenn eine Adresse verknüpft und der
          Mailversand eingerichtet ist. Nach 90 Tagen wird das Konto deaktiviert. Weitere 90 Tage
          später wird es gelöscht.
        </p>
        <p>
          Bis zur Löschung kannst du dich mit deinem Wiederherstellungsgeheimnis, deiner verknüpften
          E-Mail oder OIDC erneut anmelden und das Konto reaktivieren.
        </p>
        <button type="button" (click)="refresh()">Aktivität aktualisieren</button>
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
  readonly account = signal<AccountInfo | null>(null);

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

  dateLabel(value: string): string {
    return new Date(value).toLocaleString('de-DE');
  }
}
