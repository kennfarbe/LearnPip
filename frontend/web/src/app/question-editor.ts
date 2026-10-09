import { FormulaText } from './formula-text';
import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { QuestionRights } from './question-rights';
import { LanguageService } from './language';
import { JsonPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

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
  catalogIds?: string[];
  latestVersion: number;
  content: DraftContent;
};
type Catalog = { id: string; name: string; questionCount: number };
type Api<T> = { data: T };
type SubmissionPreview = {
  rights?: unknown;
  rightsReport?: string[];
  communityEnabled?: boolean;
  communityEligible?: boolean;
  previewToken: string;
  hasImages: boolean;
  version: {
    source: string;
    prompt: { kind: string; text: string | null; mediaId: string | null; altText: string | null }[];
    answers: {
      blocks: {
        kind: string;
        text: string | null;
        mediaId: string | null;
        altText: string | null;
      }[];
    }[];
  };
};

@Component({
  selector: 'app-question-editor',
  imports: [FormulaText, JsonPipe, FormsModule, RouterLink],
  template: `
    <section class="editor" aria-labelledby="editor-title">
      <header>
        <p class="eyebrow">{{ uiLanguage.t('Mein Lernstoff') }}</p>
        <h2 id="editor-title">{{ uiLanguage.t('Fragen sammeln') }}</h2>
        <p>
          {{
            uiLanguage.t(
              'Erstelle Fragen mit Text oder Foto, speichere sie privat und veröffentliche eine Fassung erst, wenn du bereit bist.'
            )
          }}
        </p>
      </header>

      <div class="workspace">
        <aside aria-label="Private Kataloge und Entwürfe">
          <h3>{{ uiLanguage.t('Private Kataloge') }}</h3>
          <label for="catalog-filter">{{ uiLanguage.t('Anzeigen') }}</label>
          <select id="catalog-filter" [(ngModel)]="filterCatalog">
            <option value="all">{{ uiLanguage.t('Alle Entwürfe') }}</option>
            <option value="none">{{ uiLanguage.t('Ohne Katalog') }}</option>
            @for (catalog of catalogs(); track catalog.id) {
              <option [value]="catalog.id">{{ catalog.name }} ({{ catalog.questionCount }})</option>
            }
          </select>
          <label for="draft-search">{{ uiLanguage.t('Fragen suchen') }}</label>
          <input id="draft-search" type="search" [(ngModel)]="search" />
          <label for="draft-status">{{ uiLanguage.t('Fassungsstatus') }}</label>
          <select id="draft-status" [(ngModel)]="filterStatus">
            <option value="all">{{ uiLanguage.t('Alle Fragen') }}</option>
            <option value="draft">{{ uiLanguage.t('Nur Entwürfe') }}</option>
            <option value="published">{{ uiLanguage.t('Mit veröffentlichter Fassung') }}</option>
          </select>
          <button type="button" (click)="newDraft()" [disabled]="!rights.allows('create')">
            {{ uiLanguage.t('Neue Frage erstellen') }}
          </button>
          <a routerLink="/catalogs">{{ uiLanguage.t('Kataloge verwalten') }}</a>
          <p role="status">{{ visibleDrafts().length }} {{ uiLanguage.t('Fragen gefunden') }}</p>
          <ul class="draft-list">
            @for (draft of visibleDrafts(); track draft.questionId) {
              <li>
                <button
                  type="button"
                  [attr.aria-current]="questionId() === draft.questionId ? 'true' : null"
                  (click)="editDraft(draft)"
                >
                  {{ draftTitle(draft) }}
                  <small>{{ draft.content.subject }} · {{ draft.content.topic }}</small>
                  <small>{{
                    draft.latestVersion ? 'Fassung ' + draft.latestVersion : 'Nur Entwurf'
                  }}</small>
                </button>
              </li>
            }
          </ul>
        </aside>

        @if (editing()) {
          <div class="form-panel">
            <div class="answer-heading">
              <h3 id="question-form-title" tabindex="-1">
                {{ uiLanguage.t(questionId() ? 'Frage bearbeiten' : 'Neue Frage') }}
              </h3>
              <button type="button" class="secondary" (click)="closeEditor()">
                {{ uiLanguage.t('Editor schließen') }}
              </button>
            </div>
            <p class="privacy">
              {{
                latestVersion()
                  ? 'Änderungen werden erst mit einer neuen Fassung wirksam.'
                  : 'Dieser Entwurf bleibt privat, bis du ihn ausdrücklich veröffentlichst.'
              }}
            </p>
            <div class="two-column">
              <label
                >{{ uiLanguage.t('Fach')
                }}<input [(ngModel)]="subject" maxlength="120" placeholder="z. B. Biologie"
              /></label>
              <label
                >{{ uiLanguage.t('Thema')
                }}<input [(ngModel)]="topic" maxlength="120" placeholder="z. B. Pflanzen"
              /></label>
            </div>
            <details class="editor-disclosure">
              <summary>{{ uiLanguage.t('Sprache und Katalogzuordnung') }}</summary>
              <div class="two-column catalog-selection">
                <label
                  >{{ uiLanguage.t('Sprache')
                  }}<input [(ngModel)]="language" maxlength="35" placeholder="de"
                /></label>
                <fieldset>
                  <legend>{{ uiLanguage.t('Kataloge') }}</legend>
                  <p>
                    {{
                      uiLanguage.t('Mehrere Kataloge möglich. Keine Auswahl bedeutet ohne Katalog.')
                    }}
                  </p>
                  @for (catalog of catalogs(); track catalog.id) {
                    <label class="choice">
                      <input
                        type="checkbox"
                        [checked]="catalogIds.includes(catalog.id)"
                        (change)="toggleCatalog(catalog.id)"
                      />{{ catalog.name }}
                    </label>
                  }
                </fieldset>
              </div>
            </details>
            <label
              >{{ uiLanguage.t('Frage')
              }}<textarea
                [(ngModel)]="promptText"
                maxlength="4000"
                rows="4"
                placeholder="Formuliere deine Frage"
              ></textarea>
            </label>
            <details class="editor-disclosure">
              <summary>{{ uiLanguage.t('Bild zur Frage hinzufügen') }}</summary>
              <div class="image-field">
                <label for="prompt-image">{{
                  uiLanguage.t('Foto oder Bilddatei zur Frage')
                }}</label>
                <input
                  id="prompt-image"
                  type="file"
                  accept="image/jpeg,image/png"
                  (change)="uploadImage($event, -1)"
                />
                <label
                  >{{ uiLanguage.t('Bildbeschreibung')
                  }}<input
                    [(ngModel)]="promptImageAlt"
                    maxlength="300"
                    placeholder="Was ist auf dem Bild zu sehen?"
                /></label>
                @if (promptImageId) {
                  <img [src]="imageUrl(promptImageId)" [alt]="promptImageAlt" />
                  <button type="button" class="secondary" (click)="promptImageId = ''">
                    {{ uiLanguage.t('Bild aus Frage entfernen') }}
                  </button>
                }
              </div>
            </details>
            <label
              >{{ uiLanguage.t('Auswahlart')
              }}<select [(ngModel)]="selectionMode" (change)="normalizeChoice()">
                <option value="single">{{ uiLanguage.t('Eine richtige Antwort') }}</option>
                <option value="multiple">{{ uiLanguage.t('Mehrere richtige Antworten') }}</option>
              </select>
            </label>
            <fieldset>
              <legend>{{ uiLanguage.t('Antworten') }}</legend>
              @for (answer of answers; track $index; let i = $index) {
                <div class="answer">
                  <div class="answer-heading">
                    <strong>Antwort {{ i + 1 }}</strong>
                    @if (answers.length > 2) {
                      <button type="button" class="secondary" (click)="removeAnswer(i)">
                        {{ uiLanguage.t('Entfernen') }}
                      </button>
                    }
                  </div>
                  <label
                    >{{ uiLanguage.t('Antworttext')
                    }}<textarea [(ngModel)]="answer.text" maxlength="4000" rows="2"></textarea>
                  </label>
                  <details class="editor-disclosure">
                    <summary>{{ uiLanguage.t('Bild zur Antwort hinzufügen') }}</summary>
                    <label
                      >{{ uiLanguage.t('Bilddatei (optional)')
                      }}<input
                        type="file"
                        accept="image/jpeg,image/png"
                        (change)="uploadImage($event, i)"
                    /></label>
                    <label
                      >{{ uiLanguage.t('Bildbeschreibung')
                      }}<input
                        [(ngModel)]="answer.imageAlt"
                        maxlength="300"
                        placeholder="Bild beschreiben"
                    /></label>
                    @if (answer.imageId) {
                      <img [src]="imageUrl(answer.imageId)" [alt]="answer.imageAlt" />
                      <button type="button" class="secondary" (click)="answer.imageId = ''">
                        {{ uiLanguage.t('Bild entfernen') }}
                      </button>
                    }
                  </details>
                  @if (selectionMode === 'single') {
                    <label class="choice"
                      ><input
                        type="radio"
                        name="correct-answer"
                        [checked]="answer.isCorrect"
                        (change)="selectCorrect(i)"
                      />{{ uiLanguage.t('Richtig') }}</label
                    >
                  } @else {
                    <label class="choice"
                      ><input type="checkbox" [(ngModel)]="answer.isCorrect" />{{
                        uiLanguage.t('Richtig')
                      }}</label
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
                {{ uiLanguage.t('Antwort hinzufügen') }}
              </button>
            </fieldset>
            <details class="editor-disclosure">
              <summary>{{ uiLanguage.t('Erklärung, Herkunft und Lizenz') }}</summary>
              <p>
                {{ uiLanguage.t('Herkunft und Lizenz sind vor dem Veröffentlichen erforderlich.') }}
              </p>
              <label
                >{{ uiLanguage.t('Lösungsweg und Erklärung')
                }}<textarea
                  [(ngModel)]="explanation"
                  maxlength="4000"
                  rows="4"
                  placeholder="Warum ist die Lösung richtig?"
                ></textarea>
              </label>
              <div class="two-column">
                <label
                  >{{ uiLanguage.t('Herkunft')
                  }}<input [(ngModel)]="source" maxlength="500" placeholder="z. B. Eigene Frage"
                /></label>
                <label
                  >{{ uiLanguage.t('Lizenz')
                  }}<input [(ngModel)]="license" maxlength="120" placeholder="z. B. eigene Inhalte"
                /></label>
              </div>
            </details>
            <div class="actions">
              <button
                type="button"
                [disabled]="busy() || !rights.allows(questionId() ? 'editOwn' : 'create')"
                (click)="saveDraft()"
              >
                {{ uiLanguage.t('Privat speichern') }}
              </button>
              <button
                type="button"
                class="publish"
                [disabled]="busy() || !rights.allows('editOwn')"
                (click)="publish()"
              >
                {{ latestVersion() ? 'Neue Fassung veröffentlichen' : 'Fassung veröffentlichen' }}
              </button>
              @if (questionId() && latestVersion() && rights.allows('create')) {
                <button
                  type="button"
                  class="secondary"
                  [disabled]="busy()"
                  (click)="createVariant()"
                >
                  {{ uiLanguage.t('Bearbeitbare Variante zum Lerninhalt anlegen') }}
                </button>
              }
            </div>
            @if (questionId() && rights.allows('deleteOwn')) {
              <details>
                <summary>Eigene Frage löschen</summary>
                <label
                  >Löschgrund (10–500 Zeichen)<textarea
                    [(ngModel)]="deleteReason"
                    maxlength="500"
                  ></textarea>
                </label>
                <label
                  ><input type="checkbox" [(ngModel)]="deleteConfirmed" />Löschung ausdrücklich
                  bestätigen</label
                >
                <button
                  type="button"
                  (click)="deleteQuestion()"
                  [disabled]="busy() || !deleteConfirmed || deleteReason.trim().length < 10"
                >
                  Frage löschen
                </button>
              </details>
            }
            @if (latestVersion()) {
              <p class="privacy">
                Fassung {{ latestVersion() }} ist
                {{ visibility() === 'public' ? 'öffentlich' : 'privat' }}. Eine neue Fassung bleibt
                immer privat. Eine öffentliche Einreichung wird erst nach Moderation sichtbar.
              </p>
              @if (submissionStatus()) {
                <p role="status">Einreichung: {{ submissionStatus() }}</p>
              }
              @if (
                visibility() === 'public' ||
                submissionStatus() === 'pending' ||
                submissionStatus() === 'minor_hold'
              ) {
                <button type="button" class="secondary" [disabled]="busy()" (click)="withdraw()">
                  {{ uiLanguage.t('Freigabe oder Einreichung zurückziehen') }}
                </button>
              } @else {
                <button
                  type="button"
                  class="secondary"
                  [disabled]="busy()"
                  (click)="requestPreview()"
                >
                  {{ uiLanguage.t('Öffentliche Einreichung vorbereiten und Vorschau anzeigen') }}
                </button>
                @if (submissionPreview(); as preview) {
                  <div class="privacy">
                    <h4>{{ uiLanguage.t('Vorschau der einzureichenden Fassung') }}</h4>
                    @if (!preview.communityEnabled) {
                      <p>
                        {{
                          uiLanguage.t(
                            'Öffentliche Einreichungen sind auf dieser Instanz administrativ gesperrt. Private Nutzung bleibt möglich.'
                          )
                        }}
                      </p>
                    }
                    <ul>
                      @for (issue of preview.rightsReport ?? []; track $index) {
                        <li>{{ issue }}</li>
                      }
                    </ul>
                    <p>
                      {{
                        uiLanguage.t(
                          'Einzelnachweise unter Quellen und Rechte je Frage und Bild prüfen und speichern. Die Einreichung darf fremde Lizenzen nicht ersetzen. CC BY-SA 4.0 erlaubt Weitergabe und Bearbeitung unter Namensnennung und Share-Alike; erteilte offene Lizenzen sind nicht widerrufbar.'
                        )
                      }}
                    </p>
                    <details>
                      <summary>
                        {{ uiLanguage.t('Tatsächliche Einzelquellen und Lizenzen') }}
                      </summary>
                      <pre>{{ preview.rights | json }}</pre>
                    </details>
                    <p>Herkunft: {{ preview.version.source }}</p>
                    @for (block of preview.version.prompt; track $index) {
                      @if (block.kind === 'text') {
                        <p><app-formula-text [text]="block.text ?? ''" /></p>
                      }
                      @if (block.kind === 'image' && block.mediaId) {
                        <img [src]="imageUrl(block.mediaId)" [alt]="block.altText || ''" />
                      }
                    }
                    <ol>
                      @for (answer of preview.version.answers; track $index) {
                        <li>
                          @for (block of answer.blocks; track $index) {
                            @if (block.kind === 'text') {
                              <app-formula-text [text]="block.text ?? ''" />
                            }
                            @if (block.kind === 'image' && block.mediaId) {
                              <img [src]="imageUrl(block.mediaId)" [alt]="block.altText || ''" />
                            }
                          }
                        </li>
                      }
                    </ol>
                    <label
                      >{{ uiLanguage.t('Inhaltslizenz')
                      }}<select
                        [(ngModel)]="publicLicense"
                        (ngModelChange)="rightsConfirmed = false; imageRightsConfirmed = false"
                      >
                        <option value="">{{ uiLanguage.t('Bitte bewusst auswählen') }}</option>
                        <option value="CC BY 4.0">{{ uiLanguage.t('CC BY 4.0') }}</option>
                        <option value="CC BY-SA 4.0">{{ uiLanguage.t('CC BY-SA 4.0') }}</option>
                        <option value="CC0 1.0">{{ uiLanguage.t('CC0 1.0') }}</option>
                        <option value="DL-DE/BY-2.0">DL-DE/BY-2.0</option>
                      </select>
                    </label>
                    <label
                      >{{ uiLanguage.t('Urheberangabe')
                      }}<input
                        [(ngModel)]="authorAttribution"
                        maxlength="120"
                        (ngModelChange)="rightsConfirmed = false; imageRightsConfirmed = false"
                    /></label>
                    <label
                      >{{ uiLanguage.t('Alterserklärung')
                      }}<select [(ngModel)]="ageDeclaration">
                        <option value="">{{ uiLanguage.t('Bitte auswählen') }}</option>
                        <option value="adult">{{ uiLanguage.t('Volljährig') }}</option>
                        <option value="minor">
                          {{ uiLanguage.t('Minderjährig (gesonderte Prüfung ohne Freigabe)') }}
                        </option>
                      </select>
                    </label>
                    <label
                      ><input type="checkbox" [(ngModel)]="rightsConfirmed" />{{
                        uiLanguage.t(
                          'Ich besitze die nötigen Rechte an Text und Antworten und stimme der gewählten öffentlichen Lizenz zu.'
                        )
                      }}</label
                    >
                    @if (preview.hasImages) {
                      <label
                        ><input type="checkbox" [(ngModel)]="imageRightsConfirmed" />{{
                          uiLanguage.t(
                            'Ich besitze die nötigen Bildrechte und habe persönliche Daten geprüft. Schulbuchfotos ohne Rechte darf ich nicht einreichen.'
                          )
                        }}</label
                      >
                    }
                    <button
                      type="button"
                      [disabled]="busy() || !preview.communityEligible"
                      (click)="submitForReview()"
                    >
                      {{ uiLanguage.t('Diese Fassung zur Moderation einreichen') }}
                    </button>
                  </div>
                }
              }
            }
          </div>
        } @else {
          <div class="form-panel">
            <h3>{{ uiLanguage.t('Wähle eine Frage oder erstelle eine neue.') }}</h3>
            <p>{{ uiLanguage.t('Deine Entwürfe bleiben privat.') }}</p>
          </div>
        }
      </div>
      @if (status()) {
        <p role="status" class="status">{{ status() }}</p>
      }
    </section>
  `,
  styleUrl: './question-editor.css',
})
export class QuestionEditor implements OnInit, OnDestroy {
  readonly uiLanguage = inject(LanguageService);
  readonly catalogs = signal<Catalog[]>([]);
  readonly drafts = signal<Draft[]>([]);
  readonly questionId = signal('');
  readonly latestVersion = signal(0);
  readonly visibility = signal<'private' | 'public'>('private');
  readonly submissionPreview = signal<SubmissionPreview | null>(null);
  readonly submissionStatus = signal('');
  readonly status = signal('');
  readonly busy = signal(false);
  readonly editing = signal(false);
  readonly rights = inject(QuestionRights);
  deleteConfirmed = false;
  deleteReason = '';
  private readonly route = inject(ActivatedRoute);
  search = '';
  filterStatus = 'all';
  filterCatalog = 'all';
  catalogIds: string[] = [];
  publicLicense = 'CC BY-SA 4.0';
  authorAttribution = '';
  ageDeclaration = '';
  rightsConfirmed = false;
  imageRightsConfirmed = false;
  subject = '';
  topic = '';
  language: string = this.uiLanguage.current();
  source = '';
  license = '';
  selectionMode: 'single' | 'multiple' = 'single';
  promptText = '';
  promptImageId = '';
  promptImageAlt = '';
  explanation = '';
  answers: Answer[] = [this.emptyAnswer(true), this.emptyAnswer(false)];
  private savedState = this.draftState();

  ngOnInit(): void {
    this.filterCatalog = this.route.snapshot.queryParamMap.get('catalog') ?? 'all';
    void this.rights.refresh();
    void this.refresh();
    if (this.route.snapshot.queryParamMap.get('create') === 'true') this.newDraft();
    window.addEventListener('beforeunload', this.beforeUnload);
    window.addEventListener('learnpip:photo-draft', this.openPhotoDraft);
  }

  ngOnDestroy(): void {
    window.removeEventListener('beforeunload', this.beforeUnload);
    window.removeEventListener('learnpip:photo-draft', this.openPhotoDraft);
  }

  private readonly openPhotoDraft = (event: Event): void => {
    const id = (event as CustomEvent<string>).detail;
    void this.refresh().then(() => {
      const draft = this.drafts().find((item) => item.questionId === id);
      if (draft) this.editDraft(draft);
    });
  };

  memberships(draft: Draft): string[] {
    return draft.catalogIds ?? (draft.catalogId ? [draft.catalogId] : []);
  }

  toggleCatalog(id: string): void {
    this.catalogIds = this.catalogIds.includes(id)
      ? this.catalogIds.filter((item) => item !== id)
      : [...this.catalogIds, id];
  }

  activeCatalog(): Catalog | undefined {
    return this.catalogs().find((item) => item.id === this.filterCatalog);
  }

  draftTitle(draft: Draft): string {
    return (
      draft.content.prompt?.find((block) => block.kind === 'text')?.text ||
      draft.content.subject ||
      this.uiLanguage.t('Unbenannte Frage')
    );
  }

  visibleDrafts(): Draft[] {
    const term = this.search.trim().toLocaleLowerCase();
    return this.drafts().filter((item) => {
      const inCatalog =
        this.filterCatalog === 'all' ||
        (this.filterCatalog === 'none'
          ? this.memberships(item).length === 0
          : this.memberships(item).includes(this.filterCatalog));
      const matchesStatus =
        this.filterStatus === 'all' ||
        (this.filterStatus === 'draft' ? !item.latestVersion : item.latestVersion > 0);
      const text = [this.draftTitle(item), item.content.subject, item.content.topic]
        .join(' ')
        .toLocaleLowerCase();
      return inCatalog && matchesStatus && text.includes(term);
    });
  }

  private draftState(): string {
    return JSON.stringify({ content: this.content(), catalogIds: this.catalogIds });
  }

  private isDirty(): boolean {
    return this.editing() && this.savedState !== this.draftState();
  }

  canLeave(): boolean {
    if (this.busy()) {
      this.status.set(
        this.uiLanguage.t('Bitte warte, bis der aktuelle Vorgang abgeschlossen ist.'),
      );
      return false;
    }
    return (
      !this.isDirty() || window.confirm(this.uiLanguage.t('Ungespeicherte Änderungen verwerfen?'))
    );
  }

  closeEditor(): void {
    if (this.canLeave()) this.editing.set(false);
  }

  private readonly beforeUnload = (event: BeforeUnloadEvent): void => {
    if (this.isDirty() || this.busy()) event.preventDefault();
  };

  private focusEditor(): void {
    requestAnimationFrame(() => document.getElementById('question-form-title')?.focus());
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

  newDraft(): void {
    if (!this.canLeave()) return;
    this.editing.set(true);
    this.questionId.set('');
    this.latestVersion.set(0);
    this.visibility.set('private');
    this.submissionPreview.set(null);
    this.submissionStatus.set('');
    this.catalogIds = this.activeCatalog() ? [this.activeCatalog()!.id] : [];
    this.subject = '';
    this.topic = '';
    this.language = this.uiLanguage.current();
    this.source = '';
    this.license = '';
    this.selectionMode = 'single';
    this.promptText = '';
    this.promptImageId = '';
    this.promptImageAlt = '';
    this.explanation = '';
    this.answers = [this.emptyAnswer(true), this.emptyAnswer(false)];
    this.savedState = this.draftState();
    this.focusEditor();
    this.status.set('Neue private Frage.');
  }

  editDraft(draft: Draft): void {
    if (!this.canLeave()) return;
    this.editing.set(true);
    this.deleteConfirmed = false;
    this.deleteReason = '';
    this.questionId.set(draft.questionId);
    this.latestVersion.set(draft.latestVersion);
    this.visibility.set('private');
    this.submissionPreview.set(null);
    this.submissionStatus.set('');
    if (draft.latestVersion) void this.loadVisibility(draft.questionId, draft.latestVersion);
    this.catalogIds = this.memberships(draft);
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
    this.savedState = this.draftState();
    this.focusEditor();
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

  async createVariant(): Promise<void> {
    if (!this.questionId() || !this.latestVersion() || this.busy()) return;
    this.busy.set(true);
    try {
      const response = await fetch(`/api/v1/questions/${this.questionId()}/variants/drafts`, {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          content: this.content(),
          catalogIds: this.catalogIds,
          catalogId: null,
        }),
      });
      if (!response.ok) {
        this.status.set('Variante konnte nicht angelegt werden.');
        return;
      }
      const draft = ((await response.json()) as Api<Draft>).data;
      await this.refresh();
      this.savedState = this.draftState();
      this.busy.set(false);
      this.editDraft(draft);
      this.status.set(
        'Variante als privater Entwurf angelegt. Formuliere Frage und Antworten neu und prüfe die Lösung vor der Veröffentlichung.',
      );
    } catch {
      this.status.set('Variante konnte nicht angelegt werden.');
    } finally {
      this.busy.set(false);
    }
  }

  async saveDraft(): Promise<boolean> {
    if (this.busy()) return false;
    const submittedState = this.draftState();
    this.busy.set(true);
    try {
      const id = this.questionId();
      const response = await fetch(
        id ? `/api/v1/questions/${id}/draft` : '/api/v1/questions/drafts',
        {
          method: id ? 'PUT' : 'POST',
          credentials: 'same-origin',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            content: this.content(),
            catalogIds: this.catalogIds,
            catalogId: null,
          }),
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
      this.savedState = submittedState;
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

  async deleteQuestion(): Promise<void> {
    if (!this.deleteConfirmed || !this.questionId() || this.deleteReason.trim().length < 10) return;
    this.busy.set(true);
    try {
      const response = await fetch('/api/v1/questions/delete', {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          questionIds: [this.questionId()],
          reason: this.deleteReason,
          confirmed: true,
        }),
      });
      if (!response.ok) throw new Error();
      this.deleteConfirmed = false;
      this.deleteReason = '';
      this.savedState = this.draftState();
      this.editing.set(false);
      this.questionId.set('');
      await this.refresh();
      this.status.set('Frage gelöscht.');
    } catch {
      this.status.set('Frage konnte nicht gelöscht werden. Bitte aktuelle Rechte prüfen.');
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
      this.visibility.set('private');
      this.submissionPreview.set(null);
      this.submissionStatus.set('');
      this.status.set(`Fassung ${version.version} veröffentlicht. Die Frage bleibt privat.`);
      await this.refresh();
    } catch {
      this.status.set('Veröffentlichung fehlgeschlagen.');
    } finally {
      this.busy.set(false);
    }
  }

  private async loadVisibility(questionId: string, number: number): Promise<void> {
    try {
      const response = await fetch(`/api/v1/questions/${questionId}/versions/${number}`, {
        credentials: 'same-origin',
      });
      if (!response.ok) return;
      const version = ((await response.json()) as Api<{ visibility: 'private' | 'public' }>).data;
      if (this.questionId() === questionId && this.latestVersion() === number)
        this.visibility.set(version.visibility);
      const submission = await fetch(
        `/api/v1/questions/${questionId}/versions/${number}/submission`,
        {
          credentials: 'same-origin',
        },
      );
      if (submission.ok && this.questionId() === questionId && this.latestVersion() === number) {
        const info = ((await submission.json()) as Api<{ status: string }>).data;
        this.submissionStatus.set(info.status);
      }
    } catch {
      /* Continue showing the safe private state. */
    }
  }

  async withdraw(): Promise<void> {
    const id = this.questionId();
    const number = this.latestVersion();
    if (!id || !number || this.busy()) return;
    this.busy.set(true);
    try {
      const response = await fetch(`/api/v1/questions/${id}/versions/${number}/visibility`, {
        method: 'PUT',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ visibility: 'private' }),
      });
      if (!response.ok) throw new Error();
      this.visibility.set('private');
      this.submissionStatus.set('withdrawn');
      this.submissionPreview.set(null);
      this.status.set('Die öffentliche Freigabe oder Einreichung wurde zurückgenommen.');
    } catch {
      this.status.set('Sichtbarkeit konnte nicht geändert werden.');
    } finally {
      this.busy.set(false);
    }
  }

  async requestPreview(): Promise<void> {
    try {
      const response = await fetch(
        `/api/v1/questions/${this.questionId()}/versions/${this.latestVersion()}/submission-preview`,
        {
          credentials: 'same-origin',
        },
      );
      if (!response.ok) throw new Error();
      this.submissionPreview.set(((await response.json()) as Api<SubmissionPreview>).data);
      this.rightsConfirmed = false;
      this.imageRightsConfirmed = false;
      this.status.set('Prüfe Vorschau, Herkunft, Lizenz und Bildrechte vor der Einreichung.');
    } catch {
      this.status.set('Die Vorschau konnte nicht geladen werden.');
    }
  }

  async submitForReview(): Promise<void> {
    const preview = this.submissionPreview();
    if (
      !preview ||
      !preview.communityEligible ||
      !this.publicLicense ||
      !this.authorAttribution.trim() ||
      !this.rightsConfirmed ||
      !this.ageDeclaration ||
      (preview.hasImages && !this.imageRightsConfirmed)
    ) {
      this.status.set(
        'Bitte Lizenz, Urheber, Alterserklärung und alle Rechtebestätigungen ausfüllen.',
      );
      return;
    }
    this.busy.set(true);
    try {
      const response = await fetch(
        `/api/v1/questions/${this.questionId()}/versions/${this.latestVersion()}/submission`,
        {
          method: 'POST',
          credentials: 'same-origin',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            previewToken: preview.previewToken,
            licenseChoice: this.publicLicense,
            authorAttribution: this.authorAttribution.trim(),
            rightsConfirmed: this.rightsConfirmed,
            imageRightsConfirmed: this.imageRightsConfirmed,
            ageDeclaration: this.ageDeclaration,
          }),
        },
      );
      if (!response.ok) throw new Error();
      const result = ((await response.json()) as Api<{ status: string }>).data;
      this.submissionStatus.set(result.status);
      this.submissionPreview.set(null);
      this.status.set(
        result.status === 'minor_hold'
          ? 'Die Einreichung Minderjähriger bleibt ohne öffentliches Freigabeverfahren gesperrt.'
          : 'Zur Moderation eingereicht. Die Fassung bleibt bis zur Freigabe privat.',
      );
    } catch {
      this.status.set(
        'Einreichung fehlgeschlagen. Bitte Vorschau und Rechteangaben erneut prüfen.',
      );
    } finally {
      this.busy.set(false);
    }
  }

  private emptyAnswer(isCorrect: boolean): Answer {
    return { text: '', imageId: '', imageAlt: '', isCorrect };
  }
}
