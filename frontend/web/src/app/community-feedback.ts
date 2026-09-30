import { Component, inject, OnInit, signal } from '@angular/core';
import { LanguageService } from './language';
import { FormsModule } from '@angular/forms';

interface Api<T> {
  data: T;
}
interface Question {
  id: string;
  version: number | null;
  prompt: string | null;
}
interface Feedback {
  comments: { id: string; text: string }[];
  helpful: number;
  unhelpful: number;
  myVote: boolean | null;
}
interface InboxItem {
  questionVersionId: string;
  versionNumber: number;
  prompt: string;
  openReports: number;
}
interface Detail {
  reports: { reason: string; details: string; status: string }[];
  comments: { id: string; text: string; removedAtUtc: string | null }[];
  helpful: number;
  unhelpful: number;
  events: { action: string; note: string }[];
}

@Component({
  selector: 'app-community-feedback',
  imports: [FormsModule],
  template: `
    <section class="feedback">
      <h2>{{ language.t('Fragen besprechen') }}</h2>
      <label
        >{{ language.t('Frage auswählen')
        }}<select [(ngModel)]="selected" (ngModelChange)="loadFeedback()">
          <option value="">{{ language.t('Bitte auswählen') }}</option>
          @for (item of questions(); track item.id) {
            <option [value]="item.id">{{ item.prompt }} (Version {{ item.version }})</option>
          }
        </select></label
      >
      @if (selected && feedback(); as data) {
        <p>Hilfreich: {{ data.helpful }} · Nicht hilfreich: {{ data.unhelpful }}</p>
        <button type="button" (click)="vote(true)" [attr.aria-pressed]="data.myVote === true">
          {{ language.t('Hilfreich') }}
        </button>
        <button type="button" (click)="vote(false)" [attr.aria-pressed]="data.myVote === false">
          {{ language.t('Nicht hilfreich') }}
        </button>
        <button type="button" (click)="removeVote()">
          {{ language.t('Bewertung entfernen') }}
        </button>
        <h3>{{ language.t('Kommentare') }}</h3>
        @for (entry of data.comments; track entry.id) {
          <p>{{ entry.text }}</p>
        }
        <label
          >{{ language.t('Kommentar') }}<textarea [(ngModel)]="comment" maxlength="2000"></textarea>
        </label>
        <button type="button" (click)="postComment()" [disabled]="!comment.trim()">
          {{ language.t('Kommentieren') }}
        </button>
        <h3>{{ language.t('Problem melden') }}</h3>
        <label
          >{{ language.t('Grund')
          }}<select [(ngModel)]="reason">
            <option value="incorrect">{{ language.t('Inhaltlich falsch') }}</option>
            <option value="unclear">{{ language.t('Unklar') }}</option>
            <option value="rights">{{ language.t('Rechte') }}</option>
            <option value="privacy">{{ language.t('Persönliche Daten') }}</option>
            <option value="other">{{ language.t('Sonstiges') }}</option>
          </select></label
        >
        <label
          >{{ language.t('Beschreibung')
          }}<textarea [(ngModel)]="details" maxlength="2000"></textarea>
        </label>
        <button type="button" (click)="report()" [disabled]="!details.trim()">
          {{ language.t('Meldung senden') }}
        </button>
      }
      @if (message()) {
        <p role="status">{{ message() }}</p>
      }
    </section>
    @if (moderator()) {
      <section class="feedback">
        <h2>{{ language.t('Meldungen prüfen') }}</h2>
        <button type="button" (click)="loadInbox()">
          {{ language.t('Posteingang aktualisieren') }}
        </button>
        @for (item of inbox(); track item.questionVersionId) {
          <article>
            <h3>
              {{ item.prompt }} · Version {{ item.versionNumber }} · {{ item.openReports }} offen
            </h3>
            <button type="button" (click)="open(item)">{{ language.t('Details prüfen') }}</button>
            @if (opened() === item.questionVersionId && review(); as data) {
              <p>Hilfreich: {{ data.helpful }} · Nicht hilfreich: {{ data.unhelpful }}</p>
              @for (entry of data.reports; track $index) {
                <p>{{ entry.reason }}: {{ entry.details }} ({{ entry.status }})</p>
              }
              @for (entry of data.events; track $index) {
                <p>{{ entry.action }}: {{ entry.note }}</p>
              }
              @for (entry of data.comments; track entry.id) {
                <p>{{ entry.text }}</p>
                @if (!entry.removedAtUtc) {
                  <button type="button" (click)="act('remove_comment', entry.id)">
                    {{ language.t('Kommentar entfernen') }}
                  </button>
                }
              }
              <label
                >{{ language.t('Begründung')
                }}<textarea [(ngModel)]="note" maxlength="1000"></textarea>
              </label>
              <label
                >{{ language.t('Korrigierter Fragetext')
                }}<textarea [(ngModel)]="correctedPrompt" maxlength="12000"></textarea>
              </label>
              <button type="button" (click)="act('close')">
                {{ language.t('Meldungen schließen') }}
              </button>
              <button type="button" (click)="act('correct')">
                {{ language.t('Neue private Version erstellen') }}
              </button>
              <button type="button" (click)="act('withdraw')">
                {{ language.t('Version zurückziehen') }}
              </button>
              <button type="button" (click)="act('delete')">
                {{ language.t('Frage löschen') }}
              </button>
            }
          </article>
        }
      </section>
    }
  `,
  styles: `
    .feedback {
      margin: 2rem 0;
      padding: 1.5rem;
      border: 1px solid #dfe9df;
      border-radius: 1rem;
    }
    label,
    textarea,
    select {
      display: block;
      margin: 0.75rem 0;
      max-width: 100%;
    }
    textarea {
      min-width: min(100%, 32rem);
      min-height: 4rem;
    }
    button {
      margin: 0.3rem;
      padding: 0.55rem;
      cursor: pointer;
    }
    button:focus-visible {
      outline: 3px solid #e5a44c;
    }
    article {
      border-top: 1px solid #dfe9df;
      padding: 1rem 0;
    }
  `,
})
export class CommunityFeedback implements OnInit {
  readonly language = inject(LanguageService);
  readonly questions = signal<Question[]>([]);
  readonly feedback = signal<Feedback | null>(null);
  readonly moderator = signal(false);
  readonly inbox = signal<InboxItem[]>([]);
  readonly opened = signal<string | null>(null);
  readonly review = signal<Detail | null>(null);
  readonly message = signal('');
  selected = '';
  reason = 'incorrect';
  details = '';
  comment = '';
  note = '';
  correctedPrompt = '';

  ngOnInit(): void {
    void this.loadQuestions();
    void this.loadInbox();
  }
  private current(): Question | undefined {
    return this.questions().find((item) => item.id === this.selected);
  }
  private url(): string {
    const item = this.current()!;
    return `/api/v1/questions/${item.id}/versions/${item.version}/feedback`;
  }
  private async loadQuestions(): Promise<void> {
    const response = await fetch('/api/v1/public/questions?page=1&pageSize=100');
    if (response.ok)
      this.questions.set(
        ((await response.json()) as Api<{ items: Question[] }>).data.items.filter(
          (item) => item.version,
        ),
      );
  }
  async loadFeedback(): Promise<void> {
    this.feedback.set(null);
    if (!this.current()) return;
    const response = await fetch(this.url());
    if (response.ok) this.feedback.set(((await response.json()) as Api<Feedback>).data);
  }
  private async send(path: string, method: string, body?: object): Promise<boolean> {
    const response = await fetch(path, {
      method,
      headers: body ? { 'Content-Type': 'application/json' } : {},
      body: body ? JSON.stringify(body) : undefined,
    });
    if (!response.ok) {
      this.message.set(`Aktion fehlgeschlagen (${response.status}).`);
      return false;
    }
    this.message.set('Änderung gespeichert.');
    return true;
  }
  async vote(helpful: boolean): Promise<void> {
    if (await this.send(`${this.url()}/helpful`, 'PUT', { helpful })) await this.loadFeedback();
  }
  async removeVote(): Promise<void> {
    if (await this.send(`${this.url()}/helpful`, 'DELETE')) await this.loadFeedback();
  }
  async postComment(): Promise<void> {
    if (await this.send(`${this.url()}/comments`, 'POST', { text: this.comment })) {
      this.comment = '';
      await this.loadFeedback();
    }
  }
  async report(): Promise<void> {
    if (
      await this.send(`${this.url()}/reports`, 'POST', {
        reason: this.reason,
        details: this.details,
      })
    ) {
      this.details = '';
      await this.loadInbox();
    }
  }
  async loadInbox(): Promise<void> {
    const response = await fetch('/api/v1/moderation/feedback/');
    if (response.ok) {
      this.moderator.set(true);
      this.inbox.set(((await response.json()) as Api<InboxItem[]>).data);
    }
  }
  async open(item: InboxItem): Promise<void> {
    const response = await fetch(`/api/v1/moderation/feedback/${item.questionVersionId}`);
    if (response.ok) {
      this.opened.set(item.questionVersionId);
      this.review.set(((await response.json()) as Api<Detail>).data);
    }
  }
  async act(action: string, commentId?: string): Promise<void> {
    if (!this.note.trim() || !this.opened()) {
      this.message.set('Bitte eine Begründung angeben.');
      return;
    }
    if (
      await this.send(`/api/v1/moderation/feedback/${this.opened()}/actions`, 'POST', {
        action,
        note: this.note,
        correctedPrompt: commentId || this.correctedPrompt,
      })
    ) {
      this.opened.set(null);
      this.review.set(null);
      this.note = '';
      await this.loadInbox();
      await this.loadQuestions();
    }
  }
}
