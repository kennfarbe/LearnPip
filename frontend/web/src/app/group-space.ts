import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface Group {
  id: string;
  name: string;
  ownerAccountId: string;
}
interface Catalog {
  id: string;
  name: string;
}
interface Member {
  accountId: string;
  role: string;
}
interface Invitation {
  id: string;
  code: string;
  expiresAtUtc: string;
  maxUses: number;
}
interface Api<T> {
  data: T;
}

@Component({
  selector: 'app-group-space',
  imports: [FormsModule],
  template: `
    <section class="groups" aria-labelledby="groups-title">
      <h2 id="groups-title">Geschlossene Lerngruppen</h2>
      <p>
        Teile einen eigenen Katalog mit deiner Gruppe. Ein Einladungscode dient nur zum Beitritt.
      </p>
      <div class="actions">
        <label>Gruppenname <input [(ngModel)]="name" maxlength="160" /></label>
        <button type="button" (click)="create()">Gruppe erstellen</button>
      </div>
      <div class="actions">
        <label>Einladungscode <input [(ngModel)]="code" autocomplete="off" /></label>
        <button type="button" (click)="join()">Beitreten</button>
      </div>
      @if (message()) {
        <p role="status">{{ message() }}</p>
      }
      @for (group of groups(); track group.id) {
        <article>
          <h3>{{ group.name }}</h3>
          <button type="button" (click)="open(group)">Mitglieder und Kataloge anzeigen</button>
          @if (selected() === group.id) {
            <h4>Mitglieder</h4>
            <ul>
              @for (member of members(); track member.accountId) {
                <li>
                  {{ member.accountId }} ({{ member.role }})
                  <button type="button" (click)="remove(group, member.accountId)">Entfernen</button>
                </li>
              }
            </ul>
            <h4>Freigegebene Kataloge</h4>
            <ul>
              @for (catalog of shared(); track catalog.id) {
                <li>
                  {{ catalog.name }}
                  <button type="button" (click)="unshare(group, catalog.id)">
                    Freigabe aufheben
                  </button>
                </li>
              }
            </ul>
            <div class="actions">
              <label
                >Eigener Katalog
                <select [(ngModel)]="catalogId">
                  <option value="">Bitte wählen</option>
                  @for (catalog of catalogs(); track catalog.id) {
                    <option [value]="catalog.id">{{ catalog.name }}</option>
                  }
                </select>
              </label>
              <button type="button" (click)="share(group)">Aktuelle Fassungen freigeben</button>
            </div>
            <div class="actions">
              <label
                >Ablauf in Tagen <input type="number" min="1" max="30" [(ngModel)]="days"
              /></label>
              <label>Nutzungen <input type="number" min="1" max="1000" [(ngModel)]="uses" /></label>
              <button type="button" (click)="invite(group)">Code erstellen</button>
            </div>
            @if (invitation(); as issued) {
              <p role="status">
                Code nur jetzt kopieren: <strong>{{ issued.code }}</strong
                ><br />
                Gültig bis {{ issued.expiresAtUtc }} für {{ issued.maxUses }} Beitritte.
              </p>
              <button type="button" (click)="revoke(group, issued.id)">Code widerrufen</button>
            }
          }
        </article>
      }
    </section>
  `,
  styles: `
    :host {
      display: block;
    }
    .groups {
      margin-top: 3rem;
      padding: 1.5rem;
      border: 1px solid #dfe9df;
      border-radius: 1rem;
      background: #f8fbf7;
      color: #1d3a32;
    }
    .actions {
      display: flex;
      flex-wrap: wrap;
      align-items: end;
      gap: 0.75rem;
      margin: 1rem 0;
    }
    label {
      display: grid;
      gap: 0.25rem;
    }
    input,
    select,
    button {
      min-height: 2.5rem;
      padding: 0.4rem 0.6rem;
      font: inherit;
    }
    input {
      max-width: 18rem;
    }
    button {
      border: 1px solid #205d45;
      border-radius: 0.5rem;
      background: white;
      color: #205d45;
      cursor: pointer;
    }
    button:focus-visible {
      outline: 3px solid #e5a44c;
      outline-offset: 2px;
    }
    article {
      border-top: 1px solid #dfe9df;
      padding: 1rem 0;
    }
    li {
      margin: 0.5rem 0;
      overflow-wrap: anywhere;
    }
    strong {
      overflow-wrap: anywhere;
    }
  `,
})
export class GroupSpace implements OnInit {
  readonly groups = signal<Group[]>([]);
  readonly catalogs = signal<Catalog[]>([]);
  readonly members = signal<Member[]>([]);
  readonly shared = signal<Catalog[]>([]);
  readonly selected = signal<string | null>(null);
  readonly invitation = signal<Invitation | null>(null);
  readonly message = signal('');
  name = '';
  code = '';
  catalogId = '';
  days = 7;
  uses = 10;

  ngOnInit(): void {
    void this.reload();
  }

  private async request<T>(path: string, method = 'GET', body?: object): Promise<T> {
    const response = await fetch(`/api/v1${path}`, {
      method,
      credentials: 'same-origin',
      headers: body ? { 'Content-Type': 'application/json' } : undefined,
      body: body ? JSON.stringify(body) : undefined,
    });
    if (!response.ok) throw new Error(`Anfrage fehlgeschlagen (${response.status}).`);
    return response.status === 204 ? (undefined as T) : ((await response.json()) as Api<T>).data;
  }

  async reload(): Promise<void> {
    try {
      this.groups.set(await this.request<Group[]>('/groups/'));
      this.catalogs.set(await this.request<Catalog[]>('/catalogs/'));
    } catch {
      this.groups.set([]);
    }
  }

  async create(): Promise<void> {
    try {
      await this.request<Group>('/groups/', 'POST', { name: this.name });
      this.name = '';
      this.message.set('Gruppe erstellt.');
      await this.reload();
    } catch (error) {
      this.fail(error);
    }
  }

  async join(): Promise<void> {
    try {
      await this.request<void>('/groups/join', 'POST', { code: this.code.trim() });
      this.code = '';
      this.message.set('Du bist der Gruppe beigetreten.');
      await this.reload();
    } catch (error) {
      this.fail(error);
    }
  }

  async open(group: Group): Promise<void> {
    try {
      this.selected.set(group.id);
      this.invitation.set(null);
      this.members.set(await this.request<Member[]>(`/groups/${group.id}/members`));
      this.shared.set(await this.request<Catalog[]>(`/groups/${group.id}/catalogs`));
    } catch (error) {
      this.fail(error);
    }
  }

  async invite(group: Group): Promise<void> {
    try {
      if (this.days < 1 || this.days > 30 || this.uses < 1 || this.uses > 1000)
        throw new Error('Ungültige Grenzen.');
      const expiresAtUtc = new Date(Date.now() + this.days * 86_400_000).toISOString();
      this.invitation.set(
        await this.request<Invitation>(`/groups/${group.id}/invitations`, 'POST', {
          expiresAtUtc,
          maxUses: this.uses,
        }),
      );
      this.message.set('Den Code jetzt sicher weitergeben; er wird später nicht mehr angezeigt.');
    } catch (error) {
      this.fail(error);
    }
  }

  async revoke(group: Group, id: string): Promise<void> {
    try {
      await this.request<void>(`/groups/${group.id}/invitations/${id}`, 'DELETE');
      this.invitation.set(null);
      this.message.set('Code widerrufen. Bestehende Mitglieder bleiben in der Gruppe.');
    } catch (error) {
      this.fail(error);
    }
  }

  async share(group: Group): Promise<void> {
    if (!this.catalogId) return;
    try {
      await this.request<void>(`/groups/${group.id}/catalogs/${this.catalogId}`, 'PUT');
      await this.open(group);
      this.message.set('Katalog freigegeben.');
    } catch (error) {
      this.fail(error);
    }
  }

  async unshare(group: Group, id: string): Promise<void> {
    try {
      await this.request<void>(`/groups/${group.id}/catalogs/${id}`, 'DELETE');
      await this.open(group);
      this.message.set('Freigabe aufgehoben.');
    } catch (error) {
      this.fail(error);
    }
  }

  async remove(group: Group, accountId: string): Promise<void> {
    try {
      await this.request<void>(`/groups/${group.id}/members/${accountId}`, 'DELETE');
      await this.open(group);
      await this.reload();
      this.message.set('Mitgliedschaft beendet.');
    } catch (error) {
      this.fail(error);
    }
  }

  private fail(error: unknown): void {
    this.message.set(error instanceof Error ? error.message : 'Die Aktion ist fehlgeschlagen.');
  }
}
