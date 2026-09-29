import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface Mode {
  info: {
    mode: string;
    available: boolean;
    recipient: string;
    dataShared: string;
    dailyQuota: number;
    maxInputBytes: number;
    needsUserKey: boolean;
  };
  usedToday: number;
}

@Component({
  selector: 'app-ai-assistant',
  imports: [FormsModule],
  template: `
    <section class="ai" aria-labelledby="ai-title">
      <h2 id="ai-title">Optionale KI-Unterstützung</h2>
      <p>Eigene Fragen erstellen und lernen funktioniert auch ohne KI.</p>
      <label
        >Betriebsart
        <select [(ngModel)]="mode" (ngModelChange)="confirmed = false; answer.set('')">
          @for (item of modes(); track item.info.mode) {
            <option [value]="item.info.mode" [disabled]="!item.info.available">
              {{ label(item.info.mode) }}{{ item.info.available ? '' : ' · nicht verfügbar' }}
            </option>
          }
        </select>
      </label>
      @if (selected(); as choice) {
        <p><strong>Empfänger:</strong> {{ choice.info.recipient }}</p>
        <p><strong>Datenweg:</strong> {{ choice.info.dataShared }}</p>
        @if (mode !== 'off') {
          <p>
            {{ choice.usedToday }} / {{ choice.info.dailyQuota }} Anfragen heute · höchstens
            {{ choice.info.maxInputBytes }} UTF-8-Bytes je Text.
          </p>
          <label
            >Text für den gewählten Anbieter
            <textarea [(ngModel)]="prompt" [attr.maxlength]="choice.info.maxInputBytes"></textarea>
          </label>
          <label
            ><input type="checkbox" [(ngModel)]="confirmed" />
            Ich habe Empfänger und Datenübermittlung gelesen und möchte genau diesen Text senden.
          </label>
          <button
            type="button"
            [disabled]="!confirmed || busy() || !prompt.trim()"
            (click)="generate()"
          >
            An gewählten Anbieter senden
          </button>
        }
      }
      @if (userKeyEnabled()) {
        <details>
          <summary>Eigenen API-Schlüssel verwalten</summary>
          <p>
            Schlüssel
            {{ hasUserKey() ? 'gespeichert (Wert nicht abrufbar)' : 'nicht gespeichert' }}.
          </p>
          <label
            >Neuer Schlüssel <input type="password" [(ngModel)]="newKey" autocomplete="off"
          /></label>
          <button type="button" (click)="saveKey()">Schlüssel speichern/ersetzen</button>
          @if (hasUserKey()) {
            <button type="button" (click)="deleteKey()">Schlüssel löschen</button>
          }
        </details>
      }
      @if (answer()) {
        <p role="status">{{ answer() }}</p>
      }
      @if (message()) {
        <p role="alert">{{ message() }}</p>
      }
    </section>
  `,
  styles: `
    .ai {
      margin: 2rem 0;
      padding: 1.5rem;
      border: 1px solid #dfe9df;
      border-radius: 1rem;
    }
    label {
      display: block;
      margin: 0.75rem 0;
    }
    textarea {
      display: block;
      width: min(100%, 45rem);
      min-height: 7rem;
    }
    button {
      padding: 0.6rem 1rem;
      margin: 0.4rem 0;
      cursor: pointer;
    }
    button:focus-visible,
    input:focus-visible,
    select:focus-visible,
    textarea:focus-visible {
      outline: 3px solid #e5a44c;
    }
  `,
})
export class AiAssistant implements OnInit {
  readonly modes = signal<Mode[]>([]);
  readonly hasUserKey = signal(false);
  readonly message = signal('');
  readonly answer = signal('');
  readonly busy = signal(false);
  mode = 'off';
  prompt = '';
  confirmed = false;
  newKey = '';
  disclosureVersion = '';

  ngOnInit(): void {
    void this.reload();
  }
  label(mode: string): string {
    return (
      (
        {
          off: 'Aus',
          'operator-cloud': 'Betreiber-Cloud',
          'operator-local': 'Betreiber-lokal',
          'user-key': 'Eigener API-Schlüssel',
        } as Record<string, string>
      )[mode] ?? mode
    );
  }
  selected(): Mode | undefined {
    return this.modes().find((item) => item.info.mode === this.mode);
  }
  userKeyEnabled(): boolean {
    return this.modes().some(
      (item) =>
        item.info.mode === 'user-key' &&
        (item.info.available ||
          (item.info.needsUserKey && item.info.recipient !== 'Nicht konfiguriert')),
    );
  }
  async reload(): Promise<void> {
    const response = await fetch('/api/v1/ai/modes');
    if (!response.ok) return;
    const data = (
      (await response.json()) as {
        data: {
          modes: Mode[];
          hasUserKey: boolean;
          disclosureVersion: string;
        };
      }
    ).data;
    this.modes.set(data.modes);
    this.hasUserKey.set(data.hasUserKey);
    this.disclosureVersion = data.disclosureVersion;
  }
  async saveKey(): Promise<void> {
    const response = await fetch('/api/v1/ai/user-key', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ key: this.newKey }),
    });
    this.newKey = '';
    this.message.set(
      response.ok ? 'Schlüssel gespeichert.' : `Schlüssel nicht gespeichert (${response.status}).`,
    );
    await this.reload();
  }
  async deleteKey(): Promise<void> {
    const response = await fetch('/api/v1/ai/user-key', { method: 'DELETE' });
    this.message.set(
      response.ok ? 'Schlüssel gelöscht.' : `Löschen fehlgeschlagen (${response.status}).`,
    );
    await this.reload();
  }
  async generate(): Promise<void> {
    if (!this.confirmed || !this.selected()?.info.available || this.mode === 'off') return;
    this.busy.set(true);
    this.message.set('');
    this.answer.set('');
    try {
      const response = await fetch('/api/v1/ai/generate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          mode: this.mode,
          prompt: this.prompt,
          disclosureVersion: this.disclosureVersion,
          confirmed: true,
        }),
      });
      if (!response.ok) throw new Error(`${response.status}`);
      this.answer.set(((await response.json()) as { data: { text: string } }).data.text);
      this.confirmed = false;
      await this.reload();
    } catch (error) {
      this.message.set(
        `Der gewählte Anbieter konnte nicht antworten (${String(error)}). Kein Wechsel zu einem anderen Anbieter.`,
      );
    } finally {
      this.busy.set(false);
    }
  }
}
