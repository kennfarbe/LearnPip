import { FormsModule } from '@angular/forms';
import { Component, inject, OnInit, signal } from '@angular/core';
import { LanguageService } from './language';

type Reminder = {
  enabled: boolean;
  intervalDays: number;
  quietStartMinute: number;
  quietEndMinute: number;
  timeZoneId: string;
};
type Link = { id: string; status: string; childAccountId: string; parentAccountId: string | null };
type Goal = { id: string; title: string };
type Overview = {
  totalContents: number;
  masteredContents: number;
  completedSessionsLast28Days: number;
  activeDaysLast28Days: number;
  topics: { subject: string; topic: string; totalContents: number; masteredContents: number }[];
  goals: Goal[];
};

@Component({
  selector: 'app-family-space',
  imports: [FormsModule],
  template: `
    <section class="family" aria-labelledby="family-title">
      <h2 id="family-title">{{ t('Familie und Lernziele', 'Family and learning goals') }}</h2>
      <p>
        {{
          t(
            'Eltern sehen nur Themenfortschritt, Lernroutine und Ziele. Einzelne Antworten bleiben privat.',
            'Parents see topic progress, routine and goals. Individual answers stay private.'
          )
        }}
      </p>
      <article>
        <h3>{{ t('Lernerinnerungen per E-Mail', 'Email study reminders') }}</h3>
        <p>
          {{
            t(
              'Freiwillig. Maximal eine Erinnerung bis zu deiner nächsten Lernaktivität. Keine Aufgabeninhalte in der E-Mail.',
              'Optional. At most one reminder until your next study activity. No private task details in the email.'
            )
          }}
        </p>
        <label
          ><input type="checkbox" [(ngModel)]="reminder.enabled" />{{
            t('Erinnerungen aktivieren', 'Enable reminders')
          }}</label
        >
        <label
          >{{ t('Nach Tagen ohne Lernaktivität', 'Days without study activity') }}
          <input type="number" min="1" max="30" [(ngModel)]="reminder.intervalDays" />
        </label>
        <label
          >{{ t('Ruhezeit ab (Stunde)', 'Quiet hours from (hour)') }}
          <input type="number" min="0" max="23" [(ngModel)]="quietFromHour" />
        </label>
        <label
          >{{ t('Ruhezeit bis (Stunde)', 'Quiet hours until (hour)') }}
          <input type="number" min="0" max="23" [(ngModel)]="quietUntilHour" />
        </label>
        <label
          >{{ t('Zeitzone', 'Time zone') }}
          <input [(ngModel)]="reminder.timeZoneId" maxlength="100" />
        </label>
        <button type="button" (click)="saveReminder()">
          {{ t('Erinnerungen speichern', 'Save reminders') }}
        </button>
      </article>
      @if (ageBand() === 'unknown') {
        <label
          >{{ t('Altersgruppe', 'Age group') }}
          <select [(ngModel)]="selectedAge">
            <option value="minor">{{ t('Minderjährig', 'Minor') }}</option>
            <option value="adult">{{ t('Erwachsen', 'Adult') }}</option>
          </select>
        </label>
        <button type="button" (click)="declareAge()">
          {{ t('Einmalig festlegen', 'Set once') }}
        </button>
      }
      @if (ageBand() === 'minor') {
        <p>
          {{
            t(
              'Erstelle eine Einladung. Eine Administration prüft die Beziehung. Erst deine abschließende Bestätigung erlaubt den Zugriff.',
              'Create an invitation. An administrator verifies the relationship. Your final confirmation enables access.'
            )
          }}
        </p>
        <button type="button" (click)="invite()">
          {{ t('Eltern-Einladung erzeugen', 'Create parent invitation') }}
        </button>
        @if (invitation()) {
          <p>
            {{ t('Einmaliger Code (24 Stunden):', 'One-time code (24 hours):') }}
            <code>{{ invitation() }}</code>
          </p>
        }
      }
      @if (ageBand() === 'adult') {
        <label
          >{{ t('Einladungscode vom Kind', 'Invitation code from child')
          }}<input [(ngModel)]="code" autocomplete="off"
        /></label>
        <button type="button" (click)="redeem()">
          {{ t('Verknüpfung beantragen', 'Request link') }}
        </button>
      }
      <button type="button" (click)="reload()">
        {{ t('Status aktualisieren', 'Refresh status') }}
      </button>
      @if (message()) {
        <p role="status">{{ message() }}</p>
      }
      @for (link of links(); track link.id) {
        <article>
          <h3>{{ t('Verknüpfung', 'Link') }} · {{ link.status }}</h3>
          @if (link.status === 'verified' && ageBand() === 'minor') {
            <button type="button" (click)="confirm(link)">
              {{ t('Einsicht bestätigen', 'Confirm access') }}
            </button>
          }
          @if (link.status !== 'revoked') {
            <button type="button" (click)="revoke(link)">
              {{ t('Zugriff widerrufen', 'Revoke access') }}
            </button>
          }
          @if (link.status === 'active') {
            <label
              >{{ t('Gemeinsames Ziel', 'Shared goal')
              }}<input [(ngModel)]="goalTitle" maxlength="160"
            /></label>
            <button type="button" (click)="addGoal(link)">
              {{ t('Ziel vereinbaren', 'Add goal') }}
            </button>
            <button type="button" (click)="loadOverview(link)">
              {{ t('Übersicht öffnen', 'Open overview') }}
            </button>
            @if (ageBand() === 'adult') {
              <label
                >{{
                  t(
                    'Versions-ID einer eingereichten Kinderfrage',
                    'Version ID of a minor submission'
                  )
                }}<input [(ngModel)]="submissionId"
              /></label>
              <button type="button" (click)="approveSubmission(link)">
                {{ t('Diesen Beitrag freigeben', 'Approve this submission') }}
              </button>
            }
            @if (selectedLink() === link.id && overview(); as view) {
              <p>
                {{ view.masteredContents }} / {{ view.totalContents }}
                {{ t('Lerninhalte sicher', 'mastered contents') }} ·
                {{ view.completedSessionsLast28Days }} {{ t('Einheiten', 'sessions') }} ·
                {{ view.activeDaysLast28Days }} {{ t('aktive Tage', 'active days') }}
              </p>
              <ul>
                @for (topic of view.topics; track topic.subject + topic.topic) {
                  <li>
                    {{ topic.subject }} · {{ topic.topic }}: {{ topic.masteredContents }} /
                    {{ topic.totalContents }}
                  </li>
                }
              </ul>
              <ul>
                @for (goal of view.goals; track goal.id) {
                  <li>
                    {{ goal.title }}
                    <button type="button" (click)="removeGoal(link, goal)">
                      {{ t('Entfernen', 'Remove') }}
                    </button>
                  </li>
                }
              </ul>
            }
          }
        </article>
      }
    </section>
  `,
  styles: `
    .family {
      margin-top: 3rem;
      padding: clamp(1rem, 3vw, 2rem);
      border: 1px solid #dfe9df;
      border-radius: 1rem;
      background: #f8fbf7;
      color: #1d3a32;
    }
    .family label,
    .family article {
      display: block;
      margin: 1rem 0;
    }
    .family input,
    .family select {
      display: block;
      max-width: 26rem;
      width: 100%;
      padding: 0.6rem;
    }
    .family button {
      margin: 0.4rem 0.6rem 0.4rem 0;
      min-height: 2.75rem;
    }
    .family code {
      overflow-wrap: anywhere;
    }
  `,
})
export class FamilySpace implements OnInit {
  readonly language = inject(LanguageService);
  readonly ageBand = signal('unknown');
  readonly links = signal<Link[]>([]);
  readonly invitation = signal('');
  readonly message = signal('');
  readonly selectedLink = signal('');
  readonly overview = signal<Overview | null>(null);
  selectedAge = 'minor';
  code = '';
  goalTitle = '';
  submissionId = '';
  reminder: Reminder = {
    enabled: false,
    intervalDays: 7,
    quietStartMinute: 1320,
    quietEndMinute: 480,
    timeZoneId: Intl.DateTimeFormat().resolvedOptions().timeZone || 'Europe/Berlin',
  };
  quietFromHour = 22;
  quietUntilHour = 8;

  ngOnInit(): void {
    void this.reload();
  }
  t(de: string, en: string): string {
    return this.language.current() === 'de' ? de : en;
  }

  private async request<T>(path: string, method = 'GET', body?: object): Promise<T> {
    const response = await fetch(`/api/v1/family${path}`, {
      method,
      credentials: 'same-origin',
      headers: body ? { 'Content-Type': 'application/json' } : undefined,
      body: body ? JSON.stringify(body) : undefined,
    });
    if (!response.ok) throw new Error(`${response.status}`);
    return response.status === 204
      ? (undefined as T)
      : ((await response.json()) as { data: T }).data;
  }

  async reload(): Promise<void> {
    try {
      this.ageBand.set((await this.request<{ ageBand: string }>('/me')).ageBand);
      this.links.set(await this.request<Link[]>('/links'));
      this.reminder = await this.request<Reminder>('/reminders');
      this.quietFromHour = Math.floor(this.reminder.quietStartMinute / 60);
      this.quietUntilHour = Math.floor(this.reminder.quietEndMinute / 60);
    } catch {
      this.links.set([]);
    }
  }
  private async execute(action: () => Promise<void>): Promise<void> {
    try {
      await action();
      this.message.set(this.t('Gespeichert.', 'Saved.'));
      await this.reload();
    } catch (error) {
      this.message.set(`${this.t('Aktion fehlgeschlagen', 'Action failed')} (${error}).`);
    }
  }
  saveReminder(): void {
    void this.execute(() =>
      this.request<void>('/reminders', 'PUT', {
        ...this.reminder,
        quietStartMinute: this.quietFromHour * 60,
        quietEndMinute: this.quietUntilHour * 60,
      }),
    );
  }
  declareAge(): void {
    void this.execute(() => this.request<void>('/age-band', 'PUT', { ageBand: this.selectedAge }));
  }
  invite(): void {
    void this.execute(async () => {
      this.invitation.set((await this.request<{ token: string }>('/invites', 'POST')).token);
    });
  }
  redeem(): void {
    void this.execute(async () => {
      await this.request<void>('/invites/redeem', 'POST', { token: this.code.trim() });
      this.code = '';
    });
  }
  confirm(link: Link): void {
    void this.execute(() => this.request<void>(`/links/${link.id}/confirm`, 'POST'));
  }
  revoke(link: Link): void {
    void this.execute(async () => {
      await this.request<void>(`/links/${link.id}/revoke`, 'POST');
      this.overview.set(null);
      this.selectedLink.set('');
    });
  }
  loadOverview(link: Link): void {
    void this.execute(async () => {
      this.overview.set(await this.request<Overview>(`/links/${link.id}/overview`));
      this.selectedLink.set(link.id);
    });
  }
  addGoal(link: Link): void {
    void this.execute(async () => {
      await this.request<void>(`/links/${link.id}/goals`, 'POST', {
        title: this.goalTitle.trim(),
        targetAtUtc: null,
      });
      this.goalTitle = '';
      if (this.selectedLink() === link.id) await this.loadOverview(link);
    });
  }
  removeGoal(link: Link, goal: Goal): void {
    void this.execute(async () => {
      await this.request<void>(`/links/${link.id}/goals/${goal.id}`, 'DELETE');
      if (this.selectedLink() === link.id) await this.loadOverview(link);
    });
  }
  approveSubmission(link: Link): void {
    void this.execute(() =>
      this.request<void>(
        `/links/${link.id}/submissions/${this.submissionId.trim()}/approve`,
        'POST',
      ),
    );
  }
}
