import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApplicationAccess } from './application-access';

@Component({
  selector: 'app-password-access',
  imports: [FormsModule],
  template: `
    <section class="workspace-card" aria-labelledby="password-access-title">
      <h2 id="password-access-title">Administrator-Anmeldung</h2>
      <p>
        Lokaler Benutzername und Passwort aus der Installation. Normale Lernzugänge bleiben
        erhalten.
      </p>
      <form (ngSubmit)="signIn()" class="settings-grid">
        <div>
          <label for="admin-username">Benutzername</label>
          <input
            id="admin-username"
            name="username"
            autocomplete="username"
            required
            maxlength="64"
            [(ngModel)]="username"
          />
        </div>
        <div>
          <label for="admin-password">Passwort</label>
          <input
            id="admin-password"
            name="password"
            type="password"
            autocomplete="current-password"
            required
            maxlength="128"
            [(ngModel)]="password"
          />
        </div>
        <button class="primary-action" type="submit" [disabled]="busy()">Anmelden</button>
      </form>
      <details class="workspace-disclosure">
        <summary>Lokales Passwort ändern</summary>
        <p id="password-requirements">
          12–128 Zeichen. Nach der Änderung werden alle Sitzungen beendet. Danach erneut anmelden.
        </p>
        <form (ngSubmit)="change()" class="settings-grid">
          <div>
            <label for="current-password">Bisheriges Passwort</label>
            <input
              id="current-password"
              name="currentPassword"
              type="password"
              autocomplete="current-password"
              required
              maxlength="128"
              [(ngModel)]="currentPassword"
            />
          </div>
          <div>
            <label for="new-password">Neues Passwort</label>
            <input
              id="new-password"
              name="newPassword"
              type="password"
              autocomplete="new-password"
              required
              minlength="12"
              maxlength="128"
              aria-describedby="password-requirements"
              [(ngModel)]="newPassword"
            />
          </div>
          <div>
            <label for="confirm-password">Neues Passwort wiederholen</label>
            <input
              id="confirm-password"
              name="confirmation"
              type="password"
              autocomplete="new-password"
              required
              minlength="12"
              maxlength="128"
              [(ngModel)]="confirmation"
            />
          </div>
          <button class="secondary-action" type="submit" [disabled]="busy()">
            Passwort ändern und abmelden
          </button>
        </form>
      </details>
      <p role="status" aria-live="polite">{{ message() }}</p>
    </section>
  `,
})
export class PasswordAccess {
  username = '';
  password = '';
  currentPassword = '';
  newPassword = '';
  confirmation = '';
  readonly busy = signal(false);
  readonly message = signal('');
  private readonly access = inject(ApplicationAccess);

  async signIn(): Promise<void> {
    await this.submit(
      '/api/v1/auth/password',
      { username: this.username, password: this.password },
      'Angemeldet. Administration ist jetzt verfügbar.',
    );
  }

  async change(): Promise<void> {
    if (this.newPassword !== this.confirmation) {
      this.message.set('Die neuen Passwörter stimmen nicht überein.');
      return;
    }
    await this.submit(
      '/api/v1/auth/password/change',
      { currentPassword: this.currentPassword, newPassword: this.newPassword },
      'Passwort geändert. Bitte erneut anmelden.',
    );
  }

  private async submit(path: string, body: object, success: string): Promise<void> {
    if (this.busy()) return;
    this.busy.set(true);
    this.message.set('Bitte warten …');
    try {
      const response = await fetch(path, {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      if (!response.ok) throw new Error('authentication');
      await this.access.refresh();
      this.message.set(success);
    } catch {
      this.message.set(
        'Anmeldung oder Passwortänderung fehlgeschlagen. Zugangsdaten prüfen; bei zu vielen Versuchen später erneut versuchen.',
      );
    } finally {
      this.password = '';
      this.currentPassword = '';
      this.newPassword = '';
      this.confirmation = '';
      this.busy.set(false);
    }
  }
}
