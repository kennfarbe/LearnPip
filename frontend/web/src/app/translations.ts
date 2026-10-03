import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LanguageService } from './language';

type Block = {
  kind: 'text' | 'image';
  text: string | null;
  mediaId: string | null;
  altText: string | null;
};
type Answer = { id: string; isCorrect: boolean; blocks: Block[] };
type Version = {
  id: string;
  version: number;
  language: string;
  source: string;
  license: string;
  prompt: Block[];
  explanation: Block[];
  answers: Answer[];
};
type Payload = {
  prompt: Block[];
  explanation: Block[];
  answers: { optionId: string; blocks: Block[] }[];
};
type Draft = {
  id: string;
  revision: number;
  status: string;
  source: string;
  license: string;
  provenance: string;
  payload: Payload;
  reports: { details: string; createdAtUtc: string }[];
};
type View = {
  id: string | null;
  revision: number;
  missing: boolean;
  language: string;
  source: string;
  license: string;
  provenance: string;
  payload: Payload;
};
type EditorDraft = {
  questionId: string;
  latestVersion: number;
  content: { subject: string; topic: string };
};
type Mode = {
  info: {
    mode: string;
    available: boolean;
    recipient: string;
    dataShared: string;
    maxInputBytes: number;
  };
};

@Component({
  selector: 'app-translations',
  imports: [FormsModule],
  template: `
    <section class="translations" aria-labelledby="translation-title">
      <h2 id="translation-title">
        {{ language.current() === 'en' ? 'Question translations' : 'Fragen übersetzen' }}
      </h2>
      <p>
        {{
          language.current() === 'en'
            ? 'Translate the question, answers, image descriptions and explanation together. Correct answers remain linked to the original options.'
            : 'Übersetze Frage, Antworten, Bildbeschreibungen und Lösung gemeinsam. Die richtigen Antworten bleiben mit den ursprünglichen Optionen verknüpft.'
        }}
      </p>
      <label
        >{{ language.current() === 'en' ? 'My published question' : 'Meine veröffentlichte Frage' }}
        <select [(ngModel)]="questionId" (ngModelChange)="loadQuestion()">
          <option value="">
            {{ language.current() === 'en' ? 'Choose a question' : 'Frage auswählen' }}
          </option>
          @for (item of drafts(); track item.questionId) {
            @if (item.latestVersion) {
              <option [value]="item.questionId">
                {{ item.content.subject }} · {{ item.content.topic }} · #{{ item.latestVersion }}
              </option>
            }
          }
        </select>
      </label>
      @if (original(); as base) {
        <label
          >{{ language.current() === 'en' ? 'Target language' : 'Zielsprache' }}
          <select [(ngModel)]="target" (ngModelChange)="loadTranslation()">
            <option value="de" [disabled]="base.language === 'de'">Deutsch</option>
            <option value="en" [disabled]="base.language === 'en'">English</option>
          </select>
        </label>
        @if (published(); as existing) {
          <p role="status">
            {{
              existing.missing
                ? language.current() === 'en'
                  ? 'Translation missing. Showing original.'
                  : 'Übersetzung fehlt. Original wird angezeigt.'
                : (language.current() === 'en' ? 'Approved revision ' : 'Freigegebene Revision ') +
                  existing.revision
            }}
          </p>
          @if (existing.id) {
            <label
              >{{
                language.current() === 'en'
                  ? 'Report a translation error'
                  : 'Übersetzungsfehler melden'
              }}
              <textarea [(ngModel)]="reportDetails" maxlength="2000"></textarea>
            </label>
            <button
              type="button"
              [disabled]="busy() || !reportDetails.trim()"
              (click)="report(existing.id)"
            >
              {{ language.t('Übersetzungsfehler melden') }}
            </button>
          }
        }
        <p>
          {{ language.current() === 'en' ? 'Revision history' : 'Revisionsverlauf' }}:
          @for (item of history(); track item.id) {
            <button type="button" (click)="editHistory(item)">
              #{{ item.revision }} · {{ item.status }} · {{ item.provenance }}
            </button>
            @for (report of item.reports; track report.createdAtUtc) {
              <span role="status"
                >{{ language.current() === 'en' ? 'Reported error' : 'Gemeldeter Fehler' }}:
                {{ report.details }}</span
              >
            }
          }
        </p>
        <button type="button" (click)="newDraft()">
          {{
            language.current() === 'en'
              ? 'New translation draft'
              : 'Neuen Übersetzungsentwurf bearbeiten'
          }}
        </button>
        @if (payload(); as data) {
          <div (input)="savedDraftId.set('')">
            <h3>{{ language.t('Frage und Antworten') }}</h3>
            @for (block of data.prompt; track $index; let i = $index) {
              @if (block.kind === 'text') {
                <label
                  >{{ language.current() === 'en' ? 'Question block' : 'Frageblock' }} {{ i + 1 }}
                  <textarea [(ngModel)]="block.text" maxlength="4000" rows="3"></textarea>
                </label>
              } @else {
                <label
                  >{{ language.current() === 'en' ? 'Question block' : 'Frageblock' }} {{ i + 1 }}
                  <textarea
                    [(ngModel)]="block.altText"
                    maxlength="300"
                    rows="2"
                    [attr.aria-label]="language.t('Bildbeschreibung')"
                  ></textarea>
                </label>
                <img
                  [src]="'/api/v1/media/' + block.mediaId + '/content'"
                  [alt]="block.altText ?? ''"
                />
              }
            }
            @for (answer of data.answers; track answer.optionId; let i = $index) {
              <fieldset>
                <legend>
                  {{ language.current() === 'en' ? 'Answer' : 'Antwort' }} {{ i + 1 }}
                  {{ isCorrect(answer.optionId) ? '✓' : '' }}
                </legend>
                @for (block of answer.blocks; track $index) {
                  @if (block.kind === 'text') {
                    <textarea [(ngModel)]="block.text" maxlength="4000" rows="2"></textarea>
                  } @else {
                    <label
                      >{{ language.t('Bildbeschreibung') }}
                      <textarea [(ngModel)]="block.altText" maxlength="300"></textarea>
                    </label>
                    <img
                      [src]="'/api/v1/media/' + block.mediaId + '/content'"
                      [alt]="block.altText ?? ''"
                    />
                  }
                }
              </fieldset>
            }
            <h3>{{ language.t('Lösung') }}</h3>
            @for (block of data.explanation; track $index) {
              @if (block.kind === 'text') {
                <textarea [(ngModel)]="block.text" maxlength="4000" rows="3"></textarea>
              } @else {
                <label
                  >{{ language.t('Bildbeschreibung') }}
                  <textarea [(ngModel)]="block.altText" maxlength="300"></textarea>
                </label>
                <img
                  [src]="'/api/v1/media/' + block.mediaId + '/content'"
                  [alt]="block.altText ?? ''"
                />
              }
            }
            <label
              >{{ language.current() === 'en' ? 'Translation source' : 'Herkunft der Übersetzung' }}
              <input [(ngModel)]="source" maxlength="500" />
            </label>
            <label
              >{{ language.current() === 'en' ? 'Translation license' : 'Lizenz der Übersetzung' }}
              <input [(ngModel)]="license" maxlength="120" />
            </label>
            <p>{{ language.current() === 'en' ? 'Origin' : 'Entstehung' }}: {{ provenance }}</p>
          </div>
          <button type="button" [disabled]="busy()" (click)="save()">
            {{ language.t('Entwurf speichern') }}
          </button>
          @if (savedDraftId()) {
            <button
              type="button"
              [disabled]="busy() || !source.trim() || !license.trim()"
              (click)="approve()"
            >
              {{ language.t('Übersetzung freigeben') }}
            </button>
          }
          <details>
            <summary>
              {{
                language.current() === 'en' ? 'Optional AI suggestion' : 'Optionaler KI-Vorschlag'
              }}
            </summary>
            <p>
              {{
                language.current() === 'en'
                  ? 'Only the original text and image descriptions are sent. No image files or account details are sent.'
                  : 'Nur Originaltext und Bildbeschreibungen werden übermittelt. Keine Bilddateien oder Kontodaten.'
              }}
            </p>
            <label
              >{{ language.current() === 'en' ? 'Provider' : 'Anbieter' }}
              <select [(ngModel)]="mode" (ngModelChange)="confirmed = false">
                <option value="off">Aus / Off</option>
                @for (entry of modes(); track entry.info.mode) {
                  @if (entry.info.mode !== 'off') {
                    <option [value]="entry.info.mode" [disabled]="!entry.info.available">
                      {{ entry.info.mode }} · {{ entry.info.recipient }}
                    </option>
                  }
                }
              </select>
            </label>
            @if (selectedMode(); as selected) {
              <p>{{ selected.info.dataShared }} · {{ selected.info.maxInputBytes }} UTF-8 bytes</p>
            }
            <label
              ><input type="checkbox" [(ngModel)]="confirmed" />
              {{
                language.current() === 'en'
                  ? 'I confirm sending this original text and image descriptions to the selected provider.'
                  : 'Ich bestätige die Übermittlung dieses Originaltexts und der Bildbeschreibungen an den gewählten Anbieter.'
              }}
            </label>
            <button
              type="button"
              [disabled]="busy() || mode === 'off' || !confirmed"
              (click)="suggest()"
            >
              {{
                language.current() === 'en'
                  ? 'Get editable suggestion'
                  : 'Bearbeitbaren Vorschlag abrufen'
              }}
            </button>
          </details>
        }
      }
      @if (message()) {
        <p role="status">{{ message() }}</p>
      }
    </section>
  `,
  styles: `
    .translations {
      margin: 2rem 0;
      padding: 1.5rem;
      border: 1px solid var(--border);
      border-radius: 1rem;
    }
    label {
      display: block;
      margin: 0.8rem 0;
    }
    textarea {
      display: block;
      width: min(100%, 45rem);
      min-height: 4rem;
    }
    img {
      display: block;
      max-width: min(100%, 24rem);
      max-height: 15rem;
    }
    button {
      margin: 0.3rem;
      padding: 0.55rem 0.8rem;
    }
    button:focus-visible,
    input:focus-visible,
    textarea:focus-visible,
    select:focus-visible {
      outline: 3px solid var(--warning);
    }
  `,
})
export class Translations implements OnInit {
  readonly language = inject(LanguageService);
  readonly drafts = signal<EditorDraft[]>([]);
  readonly original = signal<Version | null>(null);
  readonly published = signal<View | null>(null);
  readonly history = signal<Draft[]>([]);
  readonly payload = signal<Payload | null>(null);
  readonly modes = signal<Mode[]>([]);
  readonly savedDraftId = signal('');
  readonly busy = signal(false);
  readonly message = signal('');
  questionId = '';
  target = 'en';
  mode = 'off';
  confirmed = false;
  disclosureVersion = '';
  source = '';
  license = '';
  provenance: 'manual' | 'ai-assisted' = 'manual';
  reportDetails = '';

  ngOnInit(): void {
    void this.refresh();
  }
  private path(): string {
    const version = this.original();
    return `/api/v1/questions/${this.questionId}/versions/${version?.version}/translations`;
  }
  async refresh(): Promise<void> {
    try {
      const [drafts, modes] = await Promise.all([
        fetch('/api/v1/questions/drafts'),
        fetch('/api/v1/ai/modes'),
      ]);
      if (drafts.ok) this.drafts.set(((await drafts.json()) as { data: EditorDraft[] }).data);
      if (modes.ok) {
        const info = (
          (await modes.json()) as { data: { modes: Mode[]; disclosureVersion: string } }
        ).data;
        this.modes.set(info.modes);
        this.disclosureVersion = info.disclosureVersion;
      }
    } catch {
      this.message.set('Verbindung fehlgeschlagen / Connection failed.');
    }
  }
  async loadQuestion(): Promise<void> {
    this.original.set(null);
    this.payload.set(null);
    this.published.set(null);
    if (!this.questionId) return;
    const entry = this.drafts().find((item) => item.questionId === this.questionId);
    if (!entry?.latestVersion) return;
    try {
      const response = await fetch(
        `/api/v1/questions/${this.questionId}/versions/${entry.latestVersion}`,
      );
      if (!response.ok) throw new Error();
      const version = ((await response.json()) as { data: Version }).data;
      this.original.set(version);
      this.target = version.language === 'de' ? 'en' : 'de';
      await this.loadTranslation();
    } catch {
      this.message.set('Frage konnte nicht geladen werden / Could not load question.');
    }
  }
  async loadTranslation(): Promise<void> {
    if (!this.original()) return;
    this.payload.set(null);
    this.savedDraftId.set('');
    const [viewResponse, historyResponse] = await Promise.all([
      fetch(`${this.path()}/${this.target}`),
      fetch(`${this.path()}/history/${this.target}`),
    ]);
    if (viewResponse.ok) this.published.set(((await viewResponse.json()) as { data: View }).data);
    if (historyResponse.ok)
      this.history.set(((await historyResponse.json()) as { data: Draft[] }).data);
    this.newDraft();
  }
  newDraft(): void {
    const original = this.original();
    if (!original) return;
    const convert = (block: Block) => ({ ...block });
    this.payload.set({
      prompt: original.prompt.map(convert),
      explanation: original.explanation.map(convert),
      answers: original.answers.map((answer) => ({
        optionId: answer.id,
        blocks: answer.blocks.map(convert),
      })),
    });
    this.source = '';
    this.license = '';
    this.provenance = 'manual';
    this.savedDraftId.set('');
  }
  editHistory(item: Draft): void {
    this.payload.set(structuredClone(item.payload));
    this.source = item.source;
    this.license = item.license;
    this.provenance = item.provenance === 'ai-assisted' ? 'ai-assisted' : 'manual';
    this.savedDraftId.set(item.status === 'draft' ? item.id : '');
  }
  isCorrect(id: string): boolean {
    return this.original()?.answers.some((item) => item.id === id && item.isCorrect) ?? false;
  }
  selectedMode(): Mode | undefined {
    return this.modes().find((item) => item.info.mode === this.mode);
  }
  async suggest(): Promise<void> {
    if (!this.confirmed || this.mode === 'off') return;
    this.busy.set(true);
    try {
      const response = await fetch(`${this.path()}/suggest`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          language: this.target,
          mode: this.mode,
          disclosureVersion: this.disclosureVersion,
          confirmed: true,
        }),
      });
      if (!response.ok) throw new Error();
      this.payload.set(((await response.json()) as { data: Payload }).data);
      this.provenance = 'ai-assisted';
      this.savedDraftId.set('');
      this.confirmed = false;
      this.message.set('Ungeprüfter Vorschlag – alle Felder korrigieren / Review every field.');
    } catch {
      this.message.set('KI-Vorschlag fehlgeschlagen / AI suggestion failed.');
    } finally {
      this.busy.set(false);
    }
  }
  async save(): Promise<void> {
    if (!this.payload() || this.busy()) return;
    this.busy.set(true);
    try {
      const response = await fetch(`${this.path()}/drafts`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          language: this.target,
          payload: this.payload(),
          source: this.source,
          license: this.license,
          provenance: this.provenance,
        }),
      });
      if (!response.ok) throw new Error();
      this.savedDraftId.set(((await response.json()) as { data: { id: string } }).data.id);
      this.message.set('Privater Übersetzungsentwurf gespeichert / Private draft saved.');
      await this.loadHistory();
    } catch {
      this.message.set('Alle Text- und Bildfelder müssen übersetzt sein / Complete all fields.');
    } finally {
      this.busy.set(false);
    }
  }
  private async loadHistory(): Promise<void> {
    const response = await fetch(`${this.path()}/history/${this.target}`);
    if (response.ok) this.history.set(((await response.json()) as { data: Draft[] }).data);
  }
  async approve(): Promise<void> {
    if (!this.savedDraftId() || !this.source.trim() || !this.license.trim()) return;
    this.busy.set(true);
    try {
      const response = await fetch(`${this.path()}/${this.savedDraftId()}/approve`, {
        method: 'POST',
      });
      if (!response.ok) throw new Error();
      this.savedDraftId.set('');
      await this.loadTranslation();
      this.message.set('Übersetzung freigegeben / Translation approved.');
    } catch {
      this.message.set(
        'Zuerst Entwurf mit Herkunft und Lizenz speichern / Save source and license first.',
      );
    } finally {
      this.busy.set(false);
    }
  }
  async report(id: string): Promise<void> {
    if (!this.reportDetails.trim()) return;
    const response = await fetch(`${this.path()}/${id}/reports`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ details: this.reportDetails.trim() }),
    });
    this.message.set(
      response.ok
        ? 'Meldung erfasst / Report submitted.'
        : 'Meldung fehlgeschlagen / Report failed.',
    );
    if (response.ok) this.reportDetails = '';
  }
}
