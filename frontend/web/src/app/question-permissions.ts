import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface RoleRights {
  role: string;
  value: string | null;
  rights: Record<string, boolean>;
  defaults: Record<string, boolean>;
}

@Component({
  selector: 'app-question-permissions',
  imports: [FormsModule],
  template: `
    <section class="workspace-card" aria-labelledby="question-permissions-title">
      <h2 id="question-permissions-title">Benutzer und Rollen: Fragenberechtigungen</h2>
      <p>
        Änderungen gelten sofort, auch für aktive Sitzungen. Administratorrechte bleiben erhalten.
      </p>
      <p>
        Fragenrechte gewähren keine Rechte an Konten, Passwörtern, Gruppencodes oder Lernständen.
        Lizenzen und Freigaben gelten weiterhin; Inhaltszugriff erlaubt keine Weiterverbreitung.
      </p>
      <p>
        Private Moderation benötigt sowohl „Fremde Fragen lesen“ als auch „Private und Gruppenfragen
        lesen“. Import benötigt das Erstellen eigener Fragen; Export benötigt das Lesen eigener
        Fragen. Für Community-Einreichungen muss auch das Bearbeiten eigener Fragen erlaubt sein.
      </p>
      <button type="button" (click)="load()" [disabled]="busy()">Rechte neu laden</button>
      @for (item of roles(); track item.role) {
        <fieldset [disabled]="busy()">
          <legend>{{ item.role === 'user' ? 'Benutzer' : 'Moderator' }}</legend>
          @for (action of actions; track action.key) {
            <label class="permission">
              <input
                type="checkbox"
                [(ngModel)]="item.rights[action.key]"
                (ngModelChange)="invalidate()"
              />
              {{ action.label }}
            </label>
          }
          <button type="button" (click)="defaults(item)">Standardwerte wiederherstellen</button>
        </fieldset>
      }
      <label
        >Optionaler Änderungsgrund
        <textarea [(ngModel)]="reason" maxlength="500" (ngModelChange)="invalidate()"></textarea>
      </label>
      <button type="button" (click)="preview()" [disabled]="busy() || roles().length === 0">
        Änderungen prüfen
      </button>
      @if (reviewing()) {
        <h3>Vorschau der Auswirkungen</h3>
        <ul>
          @for (change of changes(); track change) {
            <li>{{ change }}</li>
          }
        </ul>
        @if (sensitive()) {
          <p role="alert">
            Diese Änderungen erlauben weitreichende Eingriffe in fremde Inhalte. Fremdes Löschen und
            private Inhaltszugriffe sind besonders weitreichend.
          </p>
          <label
            ><input type="checkbox" [(ngModel)]="foreignConfirmed" /> Ich bestätige die Erweiterung
            der Inhalts- und Moderationsrechte separat.</label
          >
        }
        @if (privateAccess()) {
          <p>
            Betreiberhinweis: Moderatoren oder berechtigte Benutzer können private Fragen der
            eigenen Instanz einsehen. Prüfe Zweck, Datenminimierung, Datenschutzhinweise und
            Rollenvergabe. Jeder private Moderationszugriff benötigt einen konkreten Zweck und wird
            protokolliert.
          </p>
          <label
            ><input type="checkbox" [(ngModel)]="privacyConfirmed" /> Ich habe den Betreiberhinweis
            geprüft und bestätige den Zugriff auf private Fragen.</label
          >
        }
        <button
          type="button"
          (click)="save()"
          [disabled]="
            busy() ||
            changes().length === 0 ||
            (sensitive() && !foreignConfirmed) ||
            (privateAccess() && !privacyConfirmed)
          "
        >
          Geprüfte Rechte speichern
        </button>
      }
      <p role="status">{{ message() }}</p>
    </section>
  `,
  styles: `
    fieldset {
      margin: 1rem 0;
      padding: 1rem;
      border: 1px solid var(--border);
    }
    .permission {
      display: flex;
      gap: 0.5rem;
      align-items: center;
      min-height: 2.75rem;
    }
    textarea {
      display: block;
      width: min(100%, 36rem);
      min-height: 5rem;
    }
    button {
      margin: 0.5rem 0;
      min-height: 2.75rem;
    }
    input:focus-visible,
    button:focus-visible,
    textarea:focus-visible {
      outline: 3px solid var(--warning);
    }
  `,
})
export class QuestionPermissions implements OnInit {
  readonly roles = signal<RoleRights[]>([]);
  readonly busy = signal(false);
  readonly message = signal('');
  readonly reviewing = signal(false);
  readonly changes = signal<string[]>([]);
  readonly sensitive = signal(false);
  readonly privateAccess = signal(false);
  readonly actions = [
    { key: 'create', label: 'Eigene Fragen erstellen' },
    { key: 'readOwn', label: 'Eigene Fragen lesen' },
    { key: 'editOwn', label: 'Eigene Fragen bearbeiten und Fassungen erstellen' },
    { key: 'deleteOwn', label: 'Eigene Fragen löschen' },
    {
      key: 'readShared',
      label: 'Öffentlich oder für eigene Gruppen freigegebene fremde Fragen lesen',
    },
    { key: 'readForeign', label: 'Fremde Fragen im Moderationsbereich lesen' },
    { key: 'readPrivate', label: 'Fremde private und Gruppenfragen im Moderationsbereich lesen' },
    { key: 'editForeign', label: 'Fremde Fragen als neue private Fassung bearbeiten' },
    { key: 'deleteForeign', label: 'Fremde Fragen löschen' },
    { key: 'approve', label: 'Öffentliche Einreichungen prüfen und freigeben' },
    { key: 'withdraw', label: 'Fragen sperren und öffentliche Freigaben zurückziehen' },
    { key: 'reports', label: 'Meldungen und Kommentare moderieren' },
    { key: 'batch', label: 'Mehrere berechtigte Fragen gemeinsam löschen' },
    { key: 'import', label: 'Fragenpakete privat importieren' },
    { key: 'export', label: 'Eigene und lizenzrechtlich zulässige importierte Fragen exportieren' },
    { key: 'community', label: 'Eigene Fragen zur Community-Freigabe einreichen' },
  ];
  reason = '';
  foreignConfirmed = false;
  privacyConfirmed = false;

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.busy.set(true);
    this.invalidate();
    try {
      const response = await fetch('/api/v1/admin/question-permissions/', {
        credentials: 'same-origin',
        cache: 'no-store',
      });
      if (!response.ok) throw new Error();
      const result = (await response.json()) as { data: { roles: RoleRights[] } };
      this.roles.set(result.data.roles);
    } catch {
      this.roles.set([]);
      this.message.set(
        'Rechte konnten nicht geladen werden. Eine frische Administrator-Anmeldung ist erforderlich.',
      );
    } finally {
      this.busy.set(false);
    }
  }

  invalidate(): void {
    this.reviewing.set(false);
    this.foreignConfirmed = this.privacyConfirmed = false;
  }

  defaults(item: RoleRights): void {
    item.rights = { ...item.defaults };
    this.invalidate();
  }

  preview(): void {
    const changes: string[] = [];
    let sensitive = false;
    let privateAccess = false;
    for (const role of this.roles()) {
      const before = role.value ? (JSON.parse(role.value) as Record<string, boolean>) : {};
      for (const action of this.actions) {
        const enabled = role.rights[action.key] === true;
        if (enabled === (before[action.key] === true)) continue;
        changes.push(
          `${role.role === 'user' ? 'Benutzer' : 'Moderator'}: ${action.label} – ${enabled ? 'erlauben' : 'verweigern'}`,
        );
        if (
          enabled &&
          [
            'readForeign',
            'readPrivate',
            'editForeign',
            'deleteForeign',
            'approve',
            'withdraw',
            'reports',
            'batch',
          ].includes(action.key)
        )
          sensitive = true;
        if (enabled && action.key === 'readPrivate') privateAccess = true;
      }
    }
    this.changes.set(changes);
    this.sensitive.set(sensitive);
    this.privateAccess.set(privateAccess);
    this.foreignConfirmed = this.privacyConfirmed = false;
    this.reviewing.set(true);
  }

  async save(): Promise<void> {
    if (
      !this.reviewing() ||
      (this.sensitive() && !this.foreignConfirmed) ||
      (this.privateAccess() && !this.privacyConfirmed)
    )
      return;
    this.busy.set(true);
    try {
      for (const role of this.roles()) {
        const before = role.value ? (JSON.parse(role.value) as Record<string, boolean>) : {};
        if (
          this.actions.every((action) => (before[action.key] === true) === role.rights[action.key])
        )
          continue;
        const response = await fetch(`/api/v1/admin/question-permissions/${role.role}`, {
          method: 'PUT',
          credentials: 'same-origin',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            rights: role.rights,
            expectedValue: role.value,
            reason: this.reason,
            foreignAccessConfirmed: this.foreignConfirmed,
            privacyConfirmed: this.privacyConfirmed,
          }),
        });
        if (!response.ok) throw new Error();
        role.value = JSON.stringify(role.rights);
      }
      this.message.set('Rechte gespeichert. Sie gelten sofort für alle Sitzungen.');
      this.invalidate();
    } catch {
      this.message.set(
        'Speichern fehlgeschlagen. Bereits gespeicherte Rollen bleiben wirksam; bitte Rechte neu laden und prüfen.',
      );
      this.invalidate();
    } finally {
      this.busy.set(false);
    }
  }
}
