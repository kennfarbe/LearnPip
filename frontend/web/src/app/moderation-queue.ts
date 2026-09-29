import { Component, OnInit, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface Submission {
  questionVersionId: string;
  status: string;
  authorAttribution: string;
  licenseChoice: string;
  source: string;
  prompt: string;
}
interface Block {
  kind: string;
  text: string | null;
  mediaId: string | null;
  altText: string | null;
}
interface Version {
  prompt: Block[];
  explanation: Block[];
  answers: { blocks: Block[]; isCorrect: boolean }[];
}
interface Api<T> {
  data: T;
}

@Component({
  selector: 'app-moderation-queue',
  imports: [FormsModule, NgTemplateOutlet],
  template: `
    @if (available()) {
      <section class="queue" aria-labelledby="moderation-title">
        <h2 id="moderation-title">Öffentliche Einreichungen prüfen</h2>
        <p>
          Jede Fassung bleibt bis zur vollständigen Prüfung privat. Einreichungen Minderjähriger
          können ohne gesondertes Verfahren nicht freigegeben werden.
        </p>
        @if (message()) {
          <p role="status">{{ message() }}</p>
        }
        @for (item of items(); track item.questionVersionId) {
          <article>
            <h3>{{ item.prompt }}</h3>
            <p>
              Herkunft: {{ item.source }} · Urheber: {{ item.authorAttribution }} · Lizenz:
              {{ item.licenseChoice }} · Status: {{ item.status }}
            </p>
            <button type="button" (click)="open(item)">Private Vorschau prüfen</button>
            @if (selected() === item.questionVersionId && version(); as view) {
              <h4>Frage</h4>
              @for (block of view.prompt; track $index) {
                <ng-container
                  [ngTemplateOutlet]="blockTemplate"
                  [ngTemplateOutletContext]="{ $implicit: block }"
                />
              }
              <h4>Antworten und Korrektheit</h4>
              <ol>
                @for (answer of view.answers; track $index) {
                  <li>
                    @for (block of answer.blocks; track $index) {
                      <ng-container
                        [ngTemplateOutlet]="blockTemplate"
                        [ngTemplateOutletContext]="{ $implicit: block }"
                      />
                    }
                    <strong>{{ answer.isCorrect ? 'Richtig' : 'Falsch' }}</strong>
                  </li>
                }
              </ol>
              <h4>Erklärung</h4>
              @for (block of view.explanation; track $index) {
                <ng-container
                  [ngTemplateOutlet]="blockTemplate"
                  [ngTemplateOutletContext]="{ $implicit: block }"
                />
              }
              <label><input type="checkbox" [(ngModel)]="correctness" /> Korrektheit geprüft</label>
              <label><input type="checkbox" [(ngModel)]="imageRights" /> Bildrechte geprüft</label>
              <label
                ><input type="checkbox" [(ngModel)]="personalData" /> Persönliche Daten
                geprüft</label
              >
              <label><input type="checkbox" [(ngModel)]="duplicates" /> Dubletten geprüft</label>
              <label
                >Begründung/Notiz <textarea [(ngModel)]="note" maxlength="1000"></textarea>
              </label>
              <div class="actions">
                <button
                  type="button"
                  (click)="decide(item, 'approve')"
                  [disabled]="item.status === 'minor_hold'"
                >
                  Freigeben
                </button>
                <button type="button" (click)="decide(item, 'changes_requested')">
                  Überarbeitung anfordern
                </button>
                <button type="button" (click)="decide(item, 'reject')">Ablehnen</button>
              </div>
            }
          </article>
        }
        <ng-template #blockTemplate let-block>
          @if (block.kind === 'text') {
            <p>{{ block.text }}</p>
          }
          @if (block.kind === 'image' && block.mediaId) {
            <img [src]="mediaUrl(block.mediaId)" [alt]="block.altText || ''" />
          }
        </ng-template>
      </section>
    }
  `,
  styles: `
    :host {
      display: block;
    }
    .queue {
      margin-top: 3rem;
      padding: 1.5rem;
      border: 1px solid #dfe9df;
      border-radius: 1rem;
      background: #f8fbf7;
      color: #1d3a32;
    }
    article {
      border-top: 1px solid #dfe9df;
      padding: 1rem 0;
    }
    label {
      display: block;
      margin: 0.75rem 0;
    }
    input {
      margin-right: 0.4rem;
    }
    textarea {
      display: block;
      width: min(100%, 36rem);
      min-height: 4rem;
    }
    img {
      display: block;
      max-width: min(100%, 26rem);
    }
    button {
      min-height: 2.5rem;
      padding: 0.5rem;
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
    .actions {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem;
    }
  `,
})
export class ModerationQueue implements OnInit {
  readonly available = signal(false);
  readonly items = signal<Submission[]>([]);
  readonly selected = signal<string | null>(null);
  readonly version = signal<Version | null>(null);
  readonly message = signal('');
  correctness = false;
  imageRights = false;
  personalData = false;
  duplicates = false;
  note = '';

  ngOnInit(): void {
    void this.reload();
  }

  async reload(): Promise<void> {
    try {
      const response = await fetch('/api/v1/moderation/submissions/', {
        credentials: 'same-origin',
      });
      if (!response.ok) return;
      this.available.set(true);
      this.items.set(((await response.json()) as Api<Submission[]>).data);
    } catch {
      this.available.set(false);
    }
  }

  async open(item: Submission): Promise<void> {
    try {
      const response = await fetch(`/api/v1/moderation/submissions/${item.questionVersionId}`, {
        credentials: 'same-origin',
      });
      if (!response.ok) throw new Error();
      this.version.set(((await response.json()) as Api<Version>).data);
      this.selected.set(item.questionVersionId);
      this.correctness = this.imageRights = this.personalData = this.duplicates = false;
      this.note = '';
    } catch {
      this.message.set('Vorschau konnte nicht geladen werden.');
    }
  }

  mediaUrl(id: string): string {
    return `/api/v1/moderation/submissions/${this.selected()}/media/${id}`;
  }

  async decide(
    item: Submission,
    decision: 'approve' | 'reject' | 'changes_requested',
  ): Promise<void> {
    if (
      decision === 'approve' &&
      !(this.correctness && this.imageRights && this.personalData && this.duplicates)
    ) {
      this.message.set('Für eine Freigabe müssen alle vier Prüfschritte bestätigt werden.');
      return;
    }
    if (decision !== 'approve' && !this.note.trim()) {
      this.message.set('Bitte eine Begründung angeben.');
      return;
    }
    try {
      const response = await fetch(
        `/api/v1/moderation/submissions/${item.questionVersionId}/decision`,
        {
          method: 'POST',
          credentials: 'same-origin',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            decision,
            correctnessChecked: this.correctness,
            imageRightsChecked: this.imageRights,
            personalDataChecked: this.personalData,
            duplicateChecked: this.duplicates,
            note: this.note.trim(),
          }),
        },
      );
      if (!response.ok) throw new Error();
      this.selected.set(null);
      this.version.set(null);
      this.message.set('Entscheidung gespeichert.');
      await this.reload();
    } catch {
      this.message.set('Entscheidung konnte nicht gespeichert werden.');
    }
  }
}
