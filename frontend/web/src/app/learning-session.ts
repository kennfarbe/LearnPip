import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface Block {
  kind: string;
  text: string | null;
  mediaId: string | null;
  altText: string | null;
}
interface Option {
  id: string;
  blocks: Block[];
}
interface LearningQuestion {
  questionId: string;
  versionId: string;
  selectionMode: 'single' | 'multiple';
  prompt: Block[];
  answers: Option[];
}
interface Session {
  id: string;
  total: number;
  answered: number;
  skipped: number;
  completed: boolean;
  current: LearningQuestion | null;
}
interface Feedback {
  attemptId: string;
  contentId: string;
  isCorrect: boolean;
  correctOptionIds: string[];
  explanation: Block[];
  shortExplanation: string | null;
}
interface Catalog {
  id: string;
  name: string;
}
interface LearningContent {
  id: string;
  title: string;
  questionIds: string[];
  oftenForMe: boolean;
  confidentStreak: number;
  dueAtUtc: string | null;
  mastered: boolean;
  answers: number;
  guesses: number;
  explanationsViewed: number;
}
interface ReviewOverview {
  totalContents: number;
  masteredContents: number;
  oftenForMeCount: number;
  contents: LearningContent[];
}
interface Api<T> {
  data: T;
}

const sessionKey = 'learnpip-learning-session';

@Component({
  selector: 'app-learning-session',
  imports: [FormsModule],
  template: `
    <section class="learning" aria-labelledby="learning-title">
      <h2 id="learning-title">Kurz lernen</h2>
      <p>
        Übe mit deinen veröffentlichten privaten Fragen. Jede Frage erscheint in dieser Sitzung nur
        einmal.
      </p>
      @if (overview(); as review) {
        <div class="review-summary">
          <strong
            >Lernstand: {{ review.masteredContents }} von {{ review.totalContents }} Lerninhalten
            sicher</strong
          >
          <span>{{ review.oftenForMeCount }} auf deiner persönlichen Merkliste</span>
        </div>
      }
      @if (!session()) {
        <div class="settings">
          <label
            >Katalog
            <select [(ngModel)]="catalogId">
              <option value="">Alle eigenen Fragen</option>
              @for (catalog of catalogs(); track catalog.id) {
                <option [value]="catalog.id">{{ catalog.name }}</option>
              }
            </select>
          </label>
          <label
            >Anzahl der Fragen
            <select [(ngModel)]="count">
              <option [ngValue]="3">Bis zu 3</option>
              <option [ngValue]="5">Bis zu 5</option>
              <option [ngValue]="10">Bis zu 10</option>
            </select>
          </label>
          <button type="button" [disabled]="busy()" (click)="start()">Sitzung starten</button>
        </div>
      } @else {
        <p class="progress">
          {{ session()!.answered + session()!.skipped }} von {{ session()!.total }} bearbeitet ·
          {{ session()!.answered }} beantwortet · {{ session()!.skipped }} übersprungen
        </p>
        @if (feedback(); as result) {
          <div class="feedback" role="status">
            <h3>{{ result.isCorrect ? 'Richtig beantwortet' : 'Noch nicht richtig' }}</h3>
            @if (contentFor(result.contentId); as content) {
              <p class="review-state">
                {{ content.mastered ? 'Sicher beherrscht' : 'Weiter üben' }} ·
                {{
                  content.dueAtUtc
                    ? 'Nächste Wiederholung: ' + dateLabel(content.dueAtUtc)
                    : 'Noch offen'
                }}
              </p>
              <button
                type="button"
                class="secondary"
                [disabled]="busy()"
                (click)="toggleFrequent(content)"
              >
                {{
                  content.oftenForMe ? 'Von „öfter für mich“ entfernen' : 'Öfter für mich merken'
                }}
              </button>
            }
            <p>
              Richtige Antwort:
              @for (option of session()!.current?.answers ?? []; track option.id) {
                @if (result.correctOptionIds.includes(option.id)) {
                  <span class="correct-option">{{ optionLabel(option) }}</span>
                }
              }
            </p>
            @if (result.explanation.length) {
              @if (result.shortExplanation) {
                <p>{{ result.shortExplanation }}</p>
              }
              <button type="button" class="secondary" (click)="toggleExplanation(result)">
                {{ showFull() ? 'Erklärung einklappen' : 'Ausführliche Erklärung' }}
              </button>
              @if (showFull()) {
                <div class="blocks">
                  @for (block of result.explanation; track $index) {
                    @if (block.kind === 'text') {
                      <p>{{ block.text }}</p>
                    }
                    @if (block.kind === 'image' && block.mediaId) {
                      <img [src]="imageUrl(block.mediaId)" [alt]="block.altText ?? ''" />
                    }
                  }
                </div>
              }
            } @else {
              <p>Zu dieser Frage ist keine Erklärung hinterlegt.</p>
            }
            <button type="button" [disabled]="busy()" (click)="next()">Weiter</button>
          </div>
        } @else if (session()!.current; as question) {
          <div class="question">
            <div class="blocks">
              @for (block of question.prompt; track $index) {
                @if (block.kind === 'text') {
                  <p>{{ block.text }}</p>
                }
                @if (block.kind === 'image' && block.mediaId) {
                  <img [src]="imageUrl(block.mediaId)" [alt]="block.altText ?? ''" />
                }
              }
            </div>
            <p class="hint">
              {{
                question.selectionMode === 'single'
                  ? 'Wähle eine Antwort.'
                  : 'Wähle alle richtigen Antworten.'
              }}
            </p>
            <div class="options" role="group" aria-label="Antwortmöglichkeiten">
              @for (option of question.answers; track option.id) {
                <label class="option">
                  <input
                    [type]="question.selectionMode === 'single' ? 'radio' : 'checkbox'"
                    name="learning-answer"
                    [checked]="selected().includes(option.id)"
                    (change)="toggle(option.id, question.selectionMode)"
                  />
                  <span class="blocks">
                    @for (block of option.blocks; track $index) {
                      @if (block.kind === 'text') {
                        <span>{{ block.text }}</span>
                      }
                      @if (block.kind === 'image' && block.mediaId) {
                        <img [src]="imageUrl(block.mediaId)" [alt]="block.altText ?? ''" />
                      }
                    }
                  </span>
                </label>
              }
            </div>
            <div class="actions">
              <button type="button" [disabled]="busy() || !selected().length" (click)="answer()">
                Prüfen
              </button>
              <button type="button" class="secondary" [disabled]="busy()" (click)="skip()">
                Ohne Wertung überspringen
              </button>
            </div>
            <label class="guess"
              ><input type="checkbox" [(ngModel)]="wasGuessed" /> Ich habe geraten</label
            >
          </div>
        } @else {
          <div class="feedback" role="status">
            <h3>Sitzung abgeschlossen</h3>
            <p>
              {{ session()!.answered }} beantwortet, {{ session()!.skipped }} ohne Wertung
              übersprungen.
            </p>
            <button type="button" (click)="reset()">Neue Sitzung</button>
          </div>
        }
      }
      @if (overview(); as review) {
        <div class="content-panel">
          <h3>Lerninhalte und Varianten</h3>
          <p>
            Mehrere Fragevarianten können zu einem Lerninhalt gehören. Die Zielquote zählt jeden
            Inhalt einmal.
          </p>
          <ul>
            @for (content of review.contents; track content.id) {
              <li>
                <span
                  ><strong>{{ content.title }}</strong> · {{ content.questionIds.length }} Frage(n)
                  ·
                  {{ content.mastered ? 'sicher' : 'in Übung' }}
                  @if (content.oftenForMe) {
                    · öfter für mich
                  }
                </span>
                <button
                  type="button"
                  class="secondary"
                  [disabled]="busy()"
                  (click)="toggleFrequent(content)"
                >
                  {{ content.oftenForMe ? 'Merkliste entfernen' : 'Öfter für mich' }}
                </button>
              </li>
            }
          </ul>
          @if (review.contents.length > 1) {
            <div class="settings">
              <label
                >Fragevariante
                <select [(ngModel)]="variantId">
                  <option value="">Frage auswählen</option>
                  @for (content of review.contents; track content.id) {
                    @for (id of content.questionIds; track id) {
                      <option [value]="id">{{ content.title }} · {{ id.slice(0, 8) }}</option>
                    }
                  }
                </select>
              </label>
              <label
                >Gehört zum Lerninhalt
                <select [(ngModel)]="contentTargetId">
                  <option value="">Lerninhalt auswählen</option>
                  @for (content of review.contents; track content.id) {
                    <option [value]="content.id">{{ content.title }}</option>
                  }
                </select>
              </label>
              <button
                type="button"
                [disabled]="busy() || !variantId || !contentTargetId"
                (click)="assignVariant()"
              >
                Variante zuordnen
              </button>
            </div>
          }
        </div>
      }
      @if (message()) {
        <p class="message" role="alert">{{ message() }}</p>
      }
    </section>
  `,
  styleUrl: './learning-session.css',
})
export class LearningSession implements OnInit {
  readonly catalogs = signal<Catalog[]>([]);
  readonly session = signal<Session | null>(null);
  readonly selected = signal<string[]>([]);
  readonly feedback = signal<Feedback | null>(null);
  readonly showFull = signal(false);
  readonly busy = signal(false);
  readonly message = signal('');
  readonly overview = signal<ReviewOverview | null>(null);
  catalogId = '';
  count = 5;
  variantId = '';
  contentTargetId = '';
  wasGuessed = false;

  ngOnInit(): void {
    void this.loadCatalogs();
    void this.loadReview();
    const id = sessionStorage.getItem(sessionKey);
    if (id) void this.load(id);
  }

  private async loadCatalogs(): Promise<void> {
    try {
      const response = await fetch('/api/v1/catalogs/', { credentials: 'same-origin' });
      if (response.ok) this.catalogs.set(((await response.json()) as Api<Catalog[]>).data);
    } catch {
      /* Sign in and retry when starting a session. */
    }
  }

  private async loadReview(): Promise<void> {
    try {
      const response = await fetch('/api/v1/learning/review', { credentials: 'same-origin' });
      if (response.ok) this.overview.set(((await response.json()) as Api<ReviewOverview>).data);
    } catch {
      /* The learning flow shows its own connection error. */
    }
  }

  contentFor(id: string): LearningContent | undefined {
    return this.overview()?.contents.find((item) => item.id === id);
  }

  dateLabel(value: string): string {
    return new Date(value).toLocaleDateString('de-DE');
  }

  async start(): Promise<void> {
    this.busy.set(true);
    this.message.set('');
    try {
      const response = await fetch('/api/v1/learning/sessions/', {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ catalogId: this.catalogId || null, count: this.count }),
      });
      if (!response.ok) {
        this.message.set(
          response.status === 409
            ? 'Veröffentliche zuerst eine Frage in diesem Katalog.'
            : 'Sitzung konnte nicht gestartet werden. Bitte melde dich an.',
        );
        return;
      }
      const session = ((await response.json()) as Api<Session>).data;
      this.session.set(session);
      sessionStorage.setItem(sessionKey, session.id);
    } catch {
      this.message.set('Verbindung zum Server fehlgeschlagen.');
    } finally {
      this.busy.set(false);
    }
  }

  async load(id: string): Promise<void> {
    try {
      const response = await fetch(`/api/v1/learning/sessions/${id}`, {
        credentials: 'same-origin',
      });
      if (!response.ok) {
        this.reset();
        return;
      }
      this.session.set(((await response.json()) as Api<Session>).data);
    } catch {
      this.message.set('Sitzung konnte nicht geladen werden.');
    }
  }

  toggle(id: string, mode: string): void {
    if (mode === 'single') this.selected.set([id]);
    else
      this.selected.update((ids) =>
        ids.includes(id) ? ids.filter((value) => value !== id) : [...ids, id],
      );
  }

  async answer(): Promise<void> {
    const session = this.session();
    if (!session || !this.selected().length || this.busy()) return;
    this.busy.set(true);
    try {
      const response = await fetch(`/api/v1/learning/sessions/${session.id}/answer`, {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ selectedOptionIds: this.selected(), wasGuessed: this.wasGuessed }),
      });
      if (!response.ok) {
        this.message.set('Antwort konnte nicht geprüft werden. Lade die Sitzung erneut.');
        return;
      }
      this.feedback.set(((await response.json()) as Api<Feedback>).data);
      await this.loadReview();
      window.dispatchEvent(new Event('learnpip:progress-changed'));
      this.message.set('');
    } catch {
      this.message.set('Verbindung zum Server fehlgeschlagen.');
    } finally {
      this.busy.set(false);
    }
  }

  async skip(): Promise<void> {
    const session = this.session();
    if (!session || this.busy()) return;
    this.busy.set(true);
    try {
      const response = await fetch(`/api/v1/learning/sessions/${session.id}/skip`, {
        method: 'POST',
        credentials: 'same-origin',
      });
      if (!response.ok) {
        this.message.set('Frage konnte nicht übersprungen werden.');
        return;
      }
      this.selected.set([]);
      await this.load(session.id);
      window.dispatchEvent(new Event('learnpip:progress-changed'));
      this.message.set('Frage ohne Wertung übersprungen.');
    } catch {
      this.message.set('Verbindung zum Server fehlgeschlagen.');
    } finally {
      this.busy.set(false);
    }
  }

  async next(): Promise<void> {
    const id = this.session()?.id;
    if (!id) return;
    this.feedback.set(null);
    this.showFull.set(false);
    this.selected.set([]);
    this.wasGuessed = false;
    await this.load(id);
  }

  reset(): void {
    sessionStorage.removeItem(sessionKey);
    this.session.set(null);
    this.feedback.set(null);
    this.selected.set([]);
    this.wasGuessed = false;
  }

  async toggleExplanation(result: Feedback): Promise<void> {
    if (!this.showFull()) {
      const id = this.session()?.id;
      if (!id) return;
      try {
        const response = await fetch(`/api/v1/learning/sessions/${id}/explanation`, {
          method: 'POST',
          credentials: 'same-origin',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ attemptId: result.attemptId }),
        });
        if (!response.ok) {
          this.message.set('Erklärungsabruf konnte nicht gespeichert werden.');
          return;
        }
        await this.loadReview();
        window.dispatchEvent(new Event('learnpip:progress-changed'));
      } catch {
        this.message.set('Verbindung zum Server fehlgeschlagen.');
        return;
      }
    }
    this.showFull.set(!this.showFull());
  }

  async toggleFrequent(content: LearningContent): Promise<void> {
    this.busy.set(true);
    try {
      const response = await fetch(`/api/v1/learning/contents/${content.id}/often-for-me`, {
        method: content.oftenForMe ? 'DELETE' : 'PUT',
        credentials: 'same-origin',
      });
      if (!response.ok) {
        this.message.set('Merkliste konnte nicht geändert werden.');
        return;
      }
      await this.loadReview();
      window.dispatchEvent(new Event('learnpip:progress-changed'));
    } catch {
      this.message.set('Verbindung zum Server fehlgeschlagen.');
    } finally {
      this.busy.set(false);
    }
  }

  async assignVariant(): Promise<void> {
    if (!this.variantId || !this.contentTargetId) return;
    this.busy.set(true);
    try {
      const response = await fetch(`/api/v1/learning/questions/${this.variantId}/content`, {
        method: 'PUT',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ contentId: this.contentTargetId }),
      });
      if (!response.ok) {
        this.message.set('Variante konnte nicht zugeordnet werden.');
        return;
      }
      this.variantId = '';
      this.contentTargetId = '';
      await this.loadReview();
      this.message.set('Fragevariante zugeordnet. Die Lernprognose wurde neu berechnet.');
    } catch {
      this.message.set('Verbindung zum Server fehlgeschlagen.');
    } finally {
      this.busy.set(false);
    }
  }

  optionLabel(option: Option): string {
    return option.blocks
      .map((block) => (block.kind === 'text' ? block.text : (block.altText ?? 'Bild')))
      .join(' ');
  }

  imageUrl(id: string): string {
    return `/api/v1/media/${id}/content`;
  }
}
