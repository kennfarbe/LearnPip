import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type Block = { kind: 'text' | 'image'; text?: string; mediaId?: string };
type Answer = { text: string; imageId: string; imageAlt: string; isCorrect: boolean };
type DraftContent = {
  selectionMode: 'single' | 'multiple';
  subject: string;
  topic: string;
  language: string;
  source: string;
  license: string;
  prompt: Block[];
  explanation: Block[];
  answers: { isCorrect: boolean; blocks: Block[] }[];
};
type Draft = {
  questionId: string;
  catalogId: string | null;
  latestVersion: number;
  content: DraftContent;
};
type Catalog = { id: string; name: string; questionCount: number };
type Api<T> = { data: T };

@Component({
  selector: 'app-question-editor',
  imports: [FormsModule],
  template: `
    <section class="editor" aria-labelledby="editor-title">
      <header>
        <p class="eyebrow">Mein Lernstoff</p>
        <h2 id="editor-title">Fragen sammeln</h2>
        <p>
          Erstelle Fragen mit Text oder Foto, speichere sie privat und veröffentliche eine Fassung
          erst, wenn du bereit bist.
        </p>
      </header>

      <div class="workspace">
        <aside aria-label="Private Kataloge und Entwürfe">
          <h3>Private Kataloge</h3>
          <div class="row">
            <input
              aria-label="Neuer Katalog"
              placeholder="Neuer Katalog"
              maxlength="120"
              [(ngModel)]="newCatalog"
            />
            <button type="button" (click)="createCatalog()">Anlegen</button>
          </div>
          <label for="catalog-filter">Anzeigen</label>
          <select id="catalog-filter" [(ngModel)]="filterCatalog">
            <option value="all">Alle Entwürfe</option>
            <option value="none">Ohne Katalog</option>
            @for (catalog of catalogs(); track catalog.id) {
              <option [value]="catalog.id">{{ catalog.name }} ({{ catalog.questionCount }})</option>
            }
          </select>
          @if (activeCatalog()) {
            <label for="catalog-rename">Katalogname ändern</label>
            <input id="catalog-rename" maxlength="120" [(ngModel)]="renamedCatalog" />
            <div class="row">
              <button type="button" class="secondary" (click)="renameCatalog()">Umbenennen</button>
              <button type="button" class="secondary" (click)="deleteCatalog()">
                Katalog löschen
              </button>
            </div>
          }
          <button type="button" class="secondary" (click)="newDraft()">Neue Frage</button>
          <ul class="draft-list">
            @for (draft of visibleDrafts(); track draft.questionId) {
              <li>
                <button
                  type="button"
                  [attr.aria-current]="questionId() === draft.questionId ? 'true' : null"
                  (click)="editDraft(draft)"
                >
                  {{ draft.content.subject || 'Unbenannte Frage' }}
                  <small>{{
                    draft.latestVersion ? 'Fassung ' + draft.latestVersion : 'Nur Entwurf'
                  }}</small>
                </button>
              </li>
            }
          </ul>
        </aside>

        <div class="form-panel">
          <h3>{{ questionId() ? 'Frage bearbeiten' : 'Neue Frage' }}</h3>
          <p class="privacy">
            {{
              latestVersion()
                ? 'Änderungen werden erst mit einer neuen Fassung wirksam.'
                : 'Dieser Entwurf bleibt privat, bis du ihn ausdrücklich veröffentlichst.'
            }}
          </p>
          <div class="two-column">
            <label
              >Fach <input [(ngModel)]="subject" maxlength="120" placeholder="z. B. Biologie"
            /></label>
            <label
              >Thema <input [(ngModel)]="topic" maxlength="120" placeholder="z. B. Pflanzen"
            /></label>
            <label>Sprache <input [(ngModel)]="language" maxlength="35" placeholder="de" /></label>
            <label
              >Katalog
              <select [(ngModel)]="catalogId">
                <option value="">Ohne Katalog</option>
                @for (catalog of catalogs(); track catalog.id) {
                  <option [value]="catalog.id">{{ catalog.name }}</option>
                }
              </select>
            </label>
          </div>
          <label
            >Frage
            <textarea
              [(ngModel)]="promptText"
              maxlength="4000"
              rows="4"
              placeholder="Formuliere deine Frage"
            ></textarea>
          </label>
          <div class="image-field">
            <label for="prompt-image">Foto oder Bilddatei zur Frage</label>
            <input
              id="prompt-image"
              type="file"
              accept="image/jpeg,image/png"
              (change)="uploadImage($event, -1)"
            />
            <label
              >Bildbeschreibung
              <input
                [(ngModel)]="promptImageAlt"
                maxlength="300"
                placeholder="Was ist auf dem Bild zu sehen?"
            /></label>
            @if (promptImageId) {
              <img [src]="imageUrl(promptImageId)" [alt]="promptImageAlt" />
              <button type="button" class="secondary" (click)="promptImageId = ''">
                Bild aus Frage entfernen
              </button>
            }
          </div>
          <label
            >Auswahlart
            <select [(ngModel)]="selectionMode" (change)="normalizeChoice()">
              <option value="single">Eine richtige Antwort</option>
              <option value="multiple">Mehrere richtige Antworten</option>
            </select>
          </label>
          <fieldset>
            <legend>Antworten</legend>
            @for (answer of answers; track $index; let i = $index) {
              <div class="answer">
                <div class="answer-heading">
                  <strong>Antwort {{ i + 1 }}</strong>
                  @if (answers.length > 2) {
                    <button type="button" class="secondary" (click)="removeAnswer(i)">
                      Entfernen
                    </button>
                  }
                </div>
                <label
                  >Antworttext
                  <textarea [(ngModel)]="answer.text" maxlength="4000" rows="2"></textarea>
                </label>
                <label
                  >Bilddatei (optional)
                  <input
                    type="file"
                    accept="image/jpeg,image/png"
                    (change)="uploadImage($event, i)"
                /></label>
                <label
                  >Bildbeschreibung
                  <input
                    [(ngModel)]="answer.imageAlt"
                    maxlength="300"
                    placeholder="Bild beschreiben"
                /></label>
                @if (answer.imageId) {
                  <img [src]="imageUrl(answer.imageId)" [alt]="answer.imageAlt" />
                  <button type="button" class="secondary" (click)="answer.imageId = ''">
                    Bild entfernen
                  </button>
                }
                @if (selectionMode === 'single') {
                  <label class="choice"
                    ><input
                      type="radio"
                      name="correct-answer"
                      [checked]="answer.isCorrect"
                      (change)="selectCorrect(i)"
                    />
                    Richtig</label
                  >
                } @else {
                  <label class="choice"
                    ><input type="checkbox" [(ngModel)]="answer.isCorrect" /> Richtig</label
                  >
                }
              </div>
            }
            <button
              type="button"
              class="secondary"
              [disabled]="answers.length >= 8"
              (click)="addAnswer()"
            >
              Antwort hinzufügen
            </button>
          </fieldset>
          <label
            >Lösungsweg und Erklärung
            <textarea
              [(ngModel)]="explanation"
              maxlength="4000"
              rows="4"
              placeholder="Warum ist die Lösung richtig?"
            ></textarea>
          </label>
          <div class="two-column">
            <label
              >Herkunft
              <input [(ngModel)]="source" maxlength="500" placeholder="z. B. Eigene Frage"
            /></label>
            <label
              >Lizenz
              <input [(ngModel)]="license" maxlength="120" placeholder="z. B. eigene Inhalte"
            /></label>
          </div>
          <div class="actions">
            <button type="button" [disabled]="busy()" (click)="saveDraft()">
              Privat speichern
            </button>
            <button type="button" class="publish" [disabled]="busy()" (click)="publish()">
              {{ latestVersion() ? 'Neue Fassung veröffentlichen' : 'Fassung veröffentlichen' }}
            </button>
          </div>
          @if (status()) {
            <p role="status" class="status">{{ status() }}</p>
          }
        </div>
      </div>
    </section>
  `,
  styleUrl: './question-editor.css',
})
export class QuestionEditor implements OnInit {
  readonly catalogs = signal<Catalog[]>([]);
  readonly drafts = signal<Draft[]>([]);
  readonly questionId = signal('');
  readonly latestVersion = signal(0);
  readonly status = signal('');
  readonly busy = signal(false);
  newCatalog = '';
  renamedCatalog = '';
  filterCatalog = 'all';
  catalogId = '';
  subject = '';
  topic = '';
  language = 'de';
  source = '';
  license = '';
  selectionMode: 'single' | 'multiple' = 'single';
  promptText = '';
  promptImageId = '';
  promptImageAlt = '';
  explanation = '';
  answers: Answer[] = [this.emptyAnswer(true), this.emptyAnswer(false)];

  ngOnInit(): void {
    void this.refresh();
  }

  activeCatalog(): Catalog | undefined {
    return this.catalogs().find((item) => item.id === this.filterCatalog);
  }

  visibleDrafts(): Draft[] {
    return this.drafts().filter(
      (item) =>
        this.filterCatalog === 'all' ||
        (this.filterCatalog === 'none' ? !item.catalogId : item.catalogId === this.filterCatalog),
    );
  }

  async refresh(): Promise<void> {
    try {
      const [catalogResponse, draftResponse] = await Promise.all([
        fetch('/api/v1/catalogs/', { credentials: 'same-origin' }),
        fetch('/api/v1/questions/drafts', { credentials: 'same-origin' }),
      ]);
      if (!catalogResponse.ok || !draftResponse.ok) {
        this.status.set('Melde dich an, um private Fragen und Kataloge zu laden.');
        return;
      }
      this.catalogs.set(((await catalogResponse.json()) as Api<Catalog[]>).data);
      this.drafts.set(((await draftResponse.json()) as Api<Draft[]>).data);
    } catch {
      this.status.set('Die Verbindung zum Server ist fehlgeschlagen.');
    }
  }

  async createCatalog(): Promise<void> {
    const name = this.newCatalog.trim();
    if (!name) return;
    const response = await fetch('/api/v1/catalogs/', {
      method: 'POST',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name }),
    });
    if (!response.ok) {
      this.status.set('Katalog konnte nicht angelegt werden. Prüfe den Namen.');
      return;
    }
    const catalog = ((await response.json()) as Api<Catalog>).data;
    this.newCatalog = '';
    this.catalogId = catalog.id;
    this.filterCatalog = catalog.id;
    this.renamedCatalog = catalog.name;
    await this.refresh();
  }

  async renameCatalog(): Promise<void> {
    const catalog = this.activeCatalog();
    const name = this.renamedCatalog.trim();
    if (!catalog || !name) return;
    const response = await fetch(`/api/v1/catalogs/${catalog.id}`, {
      method: 'PUT',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name }),
    });
    this.status.set(response.ok ? 'Katalog umbenannt.' : 'Katalog konnte nicht umbenannt werden.');
    if (response.ok) await this.refresh();
  }

  async deleteCatalog(): Promise<void> {
    const catalog = this.activeCatalog();
    if (
      !catalog ||
      !window.confirm(`Katalog „${catalog.name}“ löschen? Die Fragen bleiben erhalten.`)
    )
      return;
    const response = await fetch(`/api/v1/catalogs/${catalog.id}`, {
      method: 'DELETE',
      credentials: 'same-origin',
    });
    if (!response.ok) {
      this.status.set('Katalog konnte nicht gelöscht werden.');
      return;
    }
    if (this.catalogId === catalog.id) this.catalogId = '';
    this.filterCatalog = 'all';
    this.renamedCatalog = '';
    this.status.set('Katalog gelöscht. Die Fragen bleiben privat erhalten.');
    await this.refresh();
  }

  newDraft(): void {
    this.questionId.set('');
    this.latestVersion.set(0);
    this.catalogId = this.activeCatalog()?.id ?? '';
    this.subject = '';
    this.topic = '';
    this.language = 'de';
    this.source = '';
    this.license = '';
    this.selectionMode = 'single';
    this.promptText = '';
    this.promptImageId = '';
    this.promptImageAlt = '';
    this.explanation = '';
    this.answers = [this.emptyAnswer(true), this.emptyAnswer(false)];
    this.status.set('Neue private Frage.');
  }

  editDraft(draft: Draft): void {
    this.questionId.set(draft.questionId);
    this.latestVersion.set(draft.latestVersion);
    this.catalogId = draft.catalogId ?? '';
    this.promptImageAlt = '';
    const content = draft.content;
    this.subject = content.subject ?? '';
    this.topic = content.topic ?? '';
    this.language = content.language ?? 'de';
    this.source = content.source ?? '';
    this.license = content.license ?? '';
    this.selectionMode = content.selectionMode ?? 'single';
    this.promptText = content.prompt?.find((block) => block.kind === 'text')?.text ?? '';
    this.promptImageId = content.prompt?.find((block) => block.kind === 'image')?.mediaId ?? '';
    this.explanation = content.explanation?.find((block) => block.kind === 'text')?.text ?? '';
    this.answers = content.answers?.map((answer) => ({
      text: answer.blocks?.find((block) => block.kind === 'text')?.text ?? '',
      imageId: answer.blocks?.find((block) => block.kind === 'image')?.mediaId ?? '',
      imageAlt: '',
      isCorrect: answer.isCorrect,
    })) ?? [this.emptyAnswer(true), this.emptyAnswer(false)];
    this.status.set('Privaten Entwurf geladen.');
    void this.loadImageDescriptions();
  }

  private async loadImageDescriptions(): Promise<void> {
    const ids = [this.promptImageId, ...this.answers.map((answer) => answer.imageId)].filter(
      Boolean,
    );
    await Promise.all(
      ids.map(async (id) => {
        try {
          const response = await fetch(`/api/v1/media/${id}`, { credentials: 'same-origin' });
          if (!response.ok) return;
          const media = ((await response.json()) as Api<{ altText: string }>).data;
          if (this.promptImageId === id) this.promptImageAlt = media.altText;
          this.answers.forEach((answer) => {
            if (answer.imageId === id) answer.imageAlt = media.altText;
          });
          this.status.set('Privaten Entwurf geladen.');
        } catch {
          /* The image can be replaced in the editor. */
        }
      }),
    );
  }

  addAnswer(): void {
    if (this.answers.length < 8) this.answers.push(this.emptyAnswer(false));
  }
  removeAnswer(index: number): void {
    if (this.answers.length > 2) this.answers.splice(index, 1);
  }
  selectCorrect(index: number): void {
    this.answers.forEach((answer, i) => (answer.isCorrect = i === index));
  }
  normalizeChoice(): void {
    if (this.selectionMode === 'single')
      this.selectCorrect(
        Math.max(
          0,
          this.answers.findIndex((answer) => answer.isCorrect),
        ),
      );
  }
  imageUrl(id: string): string {
    return `/api/v1/media/${id}/content`;
  }

  async uploadImage(event: Event, answerIndex: number): Promise<void> {
    const file = (event.target as HTMLInputElement).files?.item(0);
    if (!file) return;
    const alt = (answerIndex < 0 ? this.promptImageAlt : this.answers[answerIndex].imageAlt).trim();
    if (!alt || file.size > 5 * 1024 * 1024) {
      this.status.set('Bitte zuerst eine Bildbeschreibung angeben und ein Bild bis 5 MiB wählen.');
      return;
    }
    const body = new FormData();
    body.append('file', file);
    body.append('altText', alt);
    try {
      const response = await fetch('/api/v1/media/', {
        method: 'POST',
        body,
        credentials: 'same-origin',
      });
      if (!response.ok) {
        this.status.set('Das Bild konnte nicht hochgeladen werden.');
        return;
      }
      const image = ((await response.json()) as Api<{ id: string }>).data;
      if (answerIndex < 0) this.promptImageId = image.id;
      else this.answers[answerIndex].imageId = image.id;
      this.status.set('Bild privat gespeichert. Speichere nun den Entwurf.');
    } catch {
      this.status.set('Bild-Upload fehlgeschlagen.');
    }
  }

  private content(): DraftContent {
    const blocks = (text: string, imageId: string): Block[] => [
      ...(text.trim() ? [{ kind: 'text' as const, text: text.trim() }] : []),
      ...(imageId ? [{ kind: 'image' as const, mediaId: imageId }] : []),
    ];
    return {
      selectionMode: this.selectionMode,
      subject: this.subject.trim(),
      topic: this.topic.trim(),
      language: this.language.trim(),
      source: this.source.trim(),
      license: this.license.trim(),
      prompt: blocks(this.promptText, this.promptImageId),
      explanation: blocks(this.explanation, ''),
      answers: this.answers.map((answer) => ({
        isCorrect: answer.isCorrect,
        blocks: blocks(answer.text, answer.imageId),
      })),
    };
  }

  async saveDraft(): Promise<boolean> {
    if (this.busy()) return false;
    this.busy.set(true);
    try {
      const id = this.questionId();
      const response = await fetch(
        id ? `/api/v1/questions/${id}/draft` : '/api/v1/questions/drafts',
        {
          method: id ? 'PUT' : 'POST',
          credentials: 'same-origin',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ content: this.content(), catalogId: this.catalogId || null }),
        },
      );
      if (!response.ok) {
        this.status.set('Entwurf konnte nicht gespeichert werden.');
        return false;
      }
      if (!id)
        this.questionId.set(
          ((await response.json()) as Api<{ questionId: string }>).data.questionId,
        );
      this.status.set('Entwurf gespeichert. Er bleibt privat.');
      await this.refresh();
      return true;
    } catch {
      this.status.set('Entwurf konnte nicht gespeichert werden.');
      return false;
    } finally {
      this.busy.set(false);
    }
  }

  async publish(): Promise<void> {
    const content = this.content();
    if (
      !content.subject ||
      !content.topic ||
      !content.source ||
      !content.license ||
      !content.prompt.length ||
      content.answers.some((answer) => !answer.blocks.length) ||
      (content.selectionMode === 'single'
        ? content.answers.filter((answer) => answer.isCorrect).length !== 1
        : content.answers.filter((answer) => answer.isCorrect).length < 2)
    ) {
      this.status.set(
        'Bitte Fach, Thema, Herkunft, Lizenz, Frage, Antworten und richtige Lösung vervollständigen.',
      );
      return;
    }
    if (!(await this.saveDraft())) return;
    this.busy.set(true);
    try {
      const response = await fetch(`/api/v1/questions/${this.questionId()}/publish`, {
        method: 'POST',
        credentials: 'same-origin',
      });
      if (!response.ok) {
        this.status.set('Veröffentlichung fehlgeschlagen. Prüfe die Eingaben und Bilddateien.');
        return;
      }
      const version = ((await response.json()) as Api<{ version: number }>).data;
      this.latestVersion.set(version.version);
      this.status.set(`Fassung ${version.version} veröffentlicht. Die Frage bleibt privat.`);
      await this.refresh();
    } catch {
      this.status.set('Veröffentlichung fehlgeschlagen.');
    } finally {
      this.busy.set(false);
    }
  }

  private emptyAnswer(isCorrect: boolean): Answer {
    return { text: '', imageId: '', imageAlt: '', isCorrect };
  }
}
