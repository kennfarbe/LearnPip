import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { QuestionRights } from './question-rights';

type Block = { kind: string; text: string | null; mediaId: string | null; altText: string | null };
type Version = {
  version: number;
  selectionMode: string;
  subject: string;
  topic: string;
  language: string;
  source: string;
  license: string;
  authorAttribution: string;
  prompt: Block[];
  explanation: Block[];
  answers: { isCorrect: boolean; blocks: Block[] }[];
};
type Entry = {
  questionId: string;
  versionId: string;
  versionNumber: number;
  visibility: string;
  subject: string;
  topic: string;
};

@Component({
  selector: 'app-question-moderation',
  imports: [FormsModule],
  template: `
    <section class="workspace-card" aria-labelledby="question-moderation-title">
      <h2 id="question-moderation-title">Alle Fragen im Moderationskontext</h2>
      <p>
        Dieser Bereich kann private und Gruppenfragen der eigenen Instanz enthalten. Verwende ihn
        nur für einen konkreten Prüfzweck. Zugriffe und Änderungen werden protokolliert.
      </p>
      <p>
        Keine Konten, Gruppencodes oder Lernstände werden angezeigt. Inhaltszugriff ersetzt keine
        Lizenzrechte.
      </p>
      <label
        >Konkreter Prüfzweck (10–500 Zeichen)
        <textarea
          [(ngModel)]="reason"
          [disabled]="busy()"
          maxlength="500"
          (ngModelChange)="clear()"
        ></textarea>
      </label>
      <button type="button" (click)="browse(0)" [disabled]="busy() || !validReason()">
        Fragenübersicht prüfen
      </button>
      @for (item of items(); track item.questionId) {
        <article>
          @if (rights.allows('batch')) {
            <label
              ><input
                type="checkbox"
                [checked]="selectedIds().includes(item.questionId)"
                (change)="select(item.questionId)"
              />Für gemeinsame Löschung auswählen</label
            >
          }
          <h3>{{ item.subject }} · {{ item.topic }}</h3>
          <p>{{ item.visibility }} · Fassung {{ item.versionNumber }}</p>
          <button type="button" (click)="inspect(item)" [disabled]="busy()">
            Inhalt mit Prüfzweck öffnen
          </button>
        </article>
      }
      @if (items().length) {
        <button type="button" (click)="browse(page - 1)" [disabled]="busy() || page === 0">
          Vorherige Seite
        </button>
        <button type="button" (click)="browse(page + 1)" [disabled]="busy() || items().length < 50">
          Nächste Seite
        </button>
      }
      @if (current(); as item) {
        @if (version(); as view) {
          <h3>Frage prüfen und als neue Fassung bearbeiten</h3>
          <p>
            Herkunft: {{ view.source }} · Lizenz: {{ view.license }} · Attribution:
            {{ view.authorAttribution }}
          </p>
          <fieldset [disabled]="busy() || !rights.allows('editForeign')">
            <legend>Frageninhalt</legend>
            <label>Fach <input [(ngModel)]="view.subject" maxlength="120" /></label>
            <label>Thema <input [(ngModel)]="view.topic" maxlength="120" /></label>
            <label>Sprache <input [(ngModel)]="view.language" maxlength="35" /></label>
            <label
              >Antwortmodus
              <select [(ngModel)]="view.selectionMode">
                <option value="single">Einfachauswahl</option>
                <option value="multiple">Mehrfachauswahl</option>
              </select></label
            >
            <h4>Frage</h4>
            @for (block of view.prompt; track $index) {
              @if (block.kind === 'text') {
                <label
                  >Fragetext <textarea [(ngModel)]="block.text" maxlength="4000"></textarea>
                </label>
              }
              @if (block.mediaId && images()[block.mediaId]) {
                <img [src]="images()[block.mediaId]" [alt]="block.altText || ''" />
              }
            }
            <h4>Antworten</h4>
            @for (answer of view.answers; track $index; let number = $index) {
              @for (block of answer.blocks; track $index) {
                @if (block.kind === 'text') {
                  <label
                    >Antwort {{ number + 1 }}
                    <textarea [(ngModel)]="block.text" maxlength="4000"></textarea>
                  </label>
                }
                @if (block.mediaId && images()[block.mediaId]) {
                  <img [src]="images()[block.mediaId]" [alt]="block.altText || ''" />
                }
              }
              <label
                ><input type="checkbox" [(ngModel)]="answer.isCorrect" />Antwort
                {{ number + 1 }} ist richtig</label
              >
            }
            <h4>Erklärung</h4>
            @for (block of view.explanation; track $index) {
              @if (block.kind === 'text') {
                <label
                  >Erklärungstext <textarea [(ngModel)]="block.text" maxlength="4000"></textarea>
                </label>
              }
              @if (block.mediaId && images()[block.mediaId]) {
                <img [src]="images()[block.mediaId]" [alt]="block.altText || ''" />
              }
            }
          </fieldset>
          @if (rights.allows('editForeign')) {
            <button
              type="button"
              (click)="revise(item, view)"
              [disabled]="busy() || !validReason()"
            >
              Neue private Fassung speichern
            </button>
          }
          @if (rights.allows('withdraw')) {
            <button type="button" (click)="withdraw(item)" [disabled]="busy() || !validReason()">
              Freigaben sperren und zurückziehen
            </button>
          }
          @if (rights.allows('deleteForeign')) {
            <label
              ><input type="checkbox" [(ngModel)]="deleteConfirmed" />Löschung dieser Frage
              ausdrücklich bestätigen</label
            >
            <button
              type="button"
              (click)="remove([item.questionId])"
              [disabled]="busy() || !deleteConfirmed"
            >
              Frage löschen
            </button>
          }
        }
      }
      @if (rights.allows('batch') && rights.allows('deleteForeign') && selectedIds().length) {
        <label
          ><input type="checkbox" [(ngModel)]="batchConfirmed" />Löschung der
          {{ selectedIds().length }} ausgewählten Fragen ausdrücklich bestätigen</label
        >
        <button
          type="button"
          (click)="remove(selectedIds())"
          [disabled]="busy() || !batchConfirmed"
        >
          Ausgewählte Fragen löschen
        </button>
      }
      <p role="status">{{ message() }}</p>
    </section>
  `,
  styles: `
    label {
      display: block;
      margin: 0.75rem 0;
    }
    input:not([type='checkbox']),
    textarea,
    select {
      display: block;
      width: min(100%, 36rem);
    }
    textarea {
      min-height: 5rem;
    }
    fieldset {
      border: 1px solid var(--border);
      min-width: 0;
    }
    img {
      max-width: 100%;
      max-height: 20rem;
    }
    article {
      border-top: 1px solid var(--border);
      margin-top: 1rem;
    }
    button {
      min-height: 2.75rem;
      margin: 0.5rem;
    }
    input:focus-visible,
    textarea:focus-visible,
    select:focus-visible,
    button:focus-visible {
      outline: 3px solid var(--warning);
    }
  `,
})
export class QuestionModeration implements OnInit, OnDestroy {
  readonly rights = inject(QuestionRights);
  readonly items = signal<Entry[]>([]);
  readonly current = signal<Entry | null>(null);
  readonly version = signal<Version | null>(null);
  readonly selectedIds = signal<string[]>([]);
  readonly images = signal<Record<string, string>>({});
  readonly busy = signal(false);
  readonly message = signal('');
  reason = '';
  page = 0;
  deleteConfirmed = false;
  batchConfirmed = false;

  ngOnInit(): void {
    void this.rights.refresh();
  }
  ngOnDestroy(): void {
    this.clear();
  }
  validReason(): boolean {
    return this.reason.trim().length >= 10 && this.reason.trim().length <= 500;
  }

  clear(): void {
    for (const url of Object.values(this.images())) URL.revokeObjectURL(url);
    this.images.set({});
    this.current.set(null);
    this.version.set(null);
    this.selectedIds.set([]);
    this.deleteConfirmed = this.batchConfirmed = false;
  }

  select(id: string): void {
    this.batchConfirmed = false;
    this.selectedIds.update((ids) =>
      ids.includes(id) ? ids.filter((value) => value !== id) : [...ids, id],
    );
  }

  private async post(path: string, body: unknown): Promise<Response> {
    const response = await fetch(`/api/v1/moderation/questions/${path}`, {
      method: 'POST',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    if (!response.ok) throw new Error();
    return response;
  }

  async browse(page: number): Promise<void> {
    if (!this.validReason()) return;
    this.busy.set(true);
    this.clear();
    try {
      await this.rights.refresh();
      const response = await this.post('browse', { reason: this.reason, page });
      this.items.set(((await response.json()) as { data: Entry[] }).data);
      this.page = page;
    } catch {
      this.items.set([]);
      this.message.set('Übersicht nicht verfügbar. Bitte aktuelle Rechte und Prüfzweck prüfen.');
    } finally {
      this.busy.set(false);
    }
  }

  async inspect(item: Entry): Promise<void> {
    if (!this.validReason()) return;
    this.busy.set(true);
    this.clear();
    try {
      const response = await this.post(`${item.versionId}/inspect`, { reason: this.reason });
      const view = ((await response.json()) as { data: Version }).data;
      const media = [
        ...view.prompt,
        ...view.explanation,
        ...view.answers.flatMap((answer) => answer.blocks),
      ];
      for (const id of new Set(
        media.map((block) => block.mediaId).filter((id): id is string => !!id),
      )) {
        const image = await this.post(`${item.versionId}/media/${id}`, { reason: this.reason });
        const blob = await image.blob();
        this.images.update((images) => ({ ...images, [id]: URL.createObjectURL(blob) }));
      }
      this.current.set(item);
      this.version.set(view);
    } catch {
      this.clear();
      this.message.set('Inhalt nicht verfügbar. Bitte Rechte und Prüfzweck prüfen.');
    } finally {
      this.busy.set(false);
    }
  }

  async revise(item: Entry, view: Version): Promise<void> {
    this.busy.set(true);
    try {
      await this.post(`${item.questionId}/revise`, {
        content: view,
        reason: this.reason,
        expectedVersion: item.versionNumber,
      });
      this.message.set(
        'Neue private Fassung gespeichert. Die vorherige Fassung und Herkunft bleiben erhalten.',
      );
      await this.browse(this.page);
    } catch {
      this.message.set(
        'Fassung nicht gespeichert. Bitte aktuelle Rechte, Inhalt und zwischenzeitliche Änderungen prüfen.',
      );
    } finally {
      this.busy.set(false);
    }
  }

  async withdraw(item: Entry): Promise<void> {
    this.busy.set(true);
    try {
      await this.post(`${item.questionId}/withdraw`, { reason: this.reason });
      this.message.set('Öffentliche und Gruppenfreigaben zurückgezogen.');
      await this.browse(this.page);
    } catch {
      this.message.set('Freigaben konnten nicht zurückgezogen werden.');
    } finally {
      this.busy.set(false);
    }
  }

  async remove(ids: string[]): Promise<void> {
    if (
      (ids.length > 1 && !this.batchConfirmed) ||
      (ids.length === 1 && !this.deleteConfirmed && !this.batchConfirmed)
    )
      return;
    this.busy.set(true);
    try {
      await this.post('delete', { questionIds: ids, reason: this.reason, confirmed: true });
      this.message.set('Fragen gelöscht; Audit und Referenzen bleiben erhalten.');
      await this.browse(this.page);
    } catch {
      this.message.set('Löschung fehlgeschlagen. Bitte Rechte und Auswahl erneut prüfen.');
    } finally {
      this.busy.set(false);
    }
  }
}
