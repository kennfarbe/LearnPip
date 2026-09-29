import { Component, inject, ElementRef, OnInit, ViewChild, signal } from '@angular/core';
import { LanguageService } from './language';
import { FormsModule } from '@angular/forms';

interface Mode {
  info: {
    mode: string;
    available: boolean;
    recipient: string;
    dataShared: string;
    dailyQuota: number;
    maxImageBytes: number;
  };
  usedToday: number;
}
interface Recognition {
  detectedText: string;
  questionText: string;
  subject: string;
  topic: string;
  formula: string | null;
  drawingDescription: string | null;
  answers: string[];
  suggestedCorrectIndex: number | null;
  computedSolution: string;
  steps: string[];
  referenceSolution: string | null;
  uncertainties: string[];
  hint?: string | null;
  nextStep?: string | null;
}
interface SolutionCheck {
  status: 'verified' | 'unverified' | 'conflict';
  reason: string;
  expected: string | null;
}
interface Review {
  recognition: Recognition;
  comparison: string;
  comparisonExplanation: string;
  mediaId: string;
  mode: string;
  verification: SolutionCheck;
}

@Component({
  selector: 'app-photo-draft',
  imports: [FormsModule],
  template: `
    <section class="photo" aria-labelledby="photo-title">
      <h2 id="photo-title">{{ language.t('Foto in privaten Aufgabenentwurf übernehmen') }}</h2>
      <p>
        {{
          language.t(
            'Zuschnitt und Vorschau entstehen zuerst auf deinem Gerät. Das zugeschnittene Foto wird nur nach deiner Freigabe in deinem privaten Konto gespeichert. Eine KI-Auswertung ist optional.'
          )
        }}
      </p>
      <label
        >{{ language.t('Foto auswählen (JPEG oder PNG, höchstens 5 MiB)')
        }}<input type="file" accept="image/jpeg,image/png" (change)="selectFile($event)" />
      </label>
      @if (file()) {
        <fieldset>
          <legend>{{ language.t('Bildbereich in Prozent zuschneiden') }}</legend>
          <label
            >{{ language.t('Links')
            }}<input
              type="number"
              min="0"
              max="99"
              [(ngModel)]="cropX"
              (ngModelChange)="invalidateCrop()"
          /></label>
          <label
            >{{ language.t('Oben')
            }}<input
              type="number"
              min="0"
              max="99"
              [(ngModel)]="cropY"
              (ngModelChange)="invalidateCrop()"
          /></label>
          <label
            >{{ language.t('Breite')
            }}<input
              type="number"
              min="1"
              max="100"
              [(ngModel)]="cropWidth"
              (ngModelChange)="invalidateCrop()"
          /></label>
          <label
            >{{ language.t('Höhe')
            }}<input
              type="number"
              min="1"
              max="100"
              [(ngModel)]="cropHeight"
              (ngModelChange)="invalidateCrop()"
          /></label>
          <button type="button" (click)="previewCrop()">
            {{ language.t('Zuschnitt anzeigen') }}
          </button>
        </fieldset>
      }
      <canvas
        #preview
        [hidden]="!previewReady()"
        aria-label="Vorschau des zugeschnittenen Fotos"
      ></canvas>
      <label
        >{{ language.t('Bildbeschreibung für deinen Entwurf')
        }}<input [(ngModel)]="altText" maxlength="300" placeholder="Was zeigt das Foto?" />
      </label>
      <label
        >{{ language.t('Betriebsart')
        }}<select [(ngModel)]="mode" (ngModelChange)="providerConfirmed = false">
          @for (entry of modes(); track entry.info.mode) {
            <option [value]="entry.info.mode" [disabled]="!entry.info.available">
              {{ label(entry.info.mode) }}{{ entry.info.available ? '' : ' · nicht verfügbar' }}
            </option>
          }
        </select>
      </label>
      @if (selectedMode(); as selected) {
        <p>
          <strong>{{ language.t('Empfänger einer späteren KI-Anfrage:') }}</strong>
          {{ selected.info.recipient }}.
        </p>
        <p>{{ selected.info.dataShared }}</p>
        @if (mode !== 'off') {
          <p>
            Bildlimit beim Anbieter: {{ selected.info.maxImageBytes }} Bytes · Tagesquote:
            {{ selected.usedToday }} / {{ selected.info.dailyQuota }}.
          </p>
        }
      }
      @if (previewReady() && !mediaId()) {
        <label
          ><input type="checkbox" [(ngModel)]="storageConfirmed" />{{
            language.t('Ich möchte nur diesen Zuschnitt privat bei LearnPip hochladen.')
          }}</label
        >
        <button
          type="button"
          [disabled]="busy() || !storageConfirmed || !altText.trim()"
          (click)="upload()"
        >
          {{ language.t('Zuschnitt privat hochladen') }}
        </button>
      }
      @if (mediaId()) {
        <p role="status">{{ language.t('Zugeschnittenes Bild privat gespeichert.') }}</p>
        @if (!savedId()) {
          <button type="button" (click)="discard()" [disabled]="busy()">
            {{ language.t('Privates Bild verwerfen') }}
          </button>
        }
        @if (!savedId() && mode !== 'off' && selectedMode()?.info?.available) {
          <label
            >{{ language.t('Musterlösung aus der Vorlage (falls lesbar, optional)')
            }}<textarea [(ngModel)]="referenceHint" maxlength="4000"></textarea>
          </label>
          <label
            ><input type="checkbox" [(ngModel)]="providerConfirmed" />{{
              language.t(
                'Ich darf das Foto verarbeiten und bestätige die Übermittlung des Zuschnitts und der optionalen Musterlösung genau an den angezeigten Empfänger.'
              )
            }}</label
          >
          <button type="button" [disabled]="busy() || !providerConfirmed" (click)="extract()">
            {{ language.t('Foto mit gewähltem Anbieter analysieren') }}
          </button>
        } @else if (!savedId() && mode === 'off' && !review()) {
          <button type="button" (click)="manualReview()">
            {{ language.t('Ohne KI selbst erfassen') }}
          </button>
        }
      }
      @if (!savedId() && review(); as result) {
        <div (input)="invalidateReview()" (change)="invalidateReview()">
          <h3>{{ language.t('Erkennung prüfen und korrigieren') }}</h3>
          <p>
            {{
              language.t(
                'Ungeprüfter Vorschlag. Unleserliche Zeichen, Formeln und Zeichnungen im Foto vergleichen.'
              )
            }}
          </p>
          <label
            >{{ language.t('Erkannter Text')
            }}<textarea [(ngModel)]="result.recognition.detectedText" rows="3"></textarea>
          </label>
          <label
            >{{ language.t('Frage')
            }}<textarea
              [(ngModel)]="result.recognition.questionText"
              maxlength="4000"
              rows="3"
            ></textarea>
          </label>
          <label
            >{{ language.t('Formel')
            }}<textarea [(ngModel)]="result.recognition.formula" maxlength="2000"></textarea>
          </label>
          <label
            >{{ language.t('Zeichnung/Bildbeschreibung')
            }}<textarea
              [(ngModel)]="result.recognition.drawingDescription"
              maxlength="2000"
            ></textarea>
          </label>
          <label
            >{{ language.t('Fach')
            }}<input [(ngModel)]="result.recognition.subject" maxlength="120"
          /></label>
          <label
            >{{ language.t('Thema') }}<input [(ngModel)]="result.recognition.topic" maxlength="120"
          /></label>
          <fieldset>
            <legend>{{ language.t('Antworten · richtige Lösung selbst festlegen') }}</legend>
            @for (answer of result.recognition.answers; track $index; let i = $index) {
              <label
                >Antwort {{ i + 1 }}
                <input
                  [ngModel]="answer"
                  (ngModelChange)="result.recognition.answers[i] = $event"
                  maxlength="4000"
              /></label>
              <label
                ><input
                  type="radio"
                  name="photo-correct"
                  [checked]="correctIndex === i"
                  (change)="correctIndex = i"
                />{{ language.t('Von mir als richtig geprüft') }}</label
              >
            }
            <button
              type="button"
              [disabled]="result.recognition.answers.length >= 8"
              (click)="result.recognition.answers.push(''); reviewConfirmed = false"
            >
              {{ language.t('Antwort ergänzen') }}
            </button>
          </fieldset>
          @if (result.recognition.suggestedCorrectIndex !== null) {
            <p>
              KI-Vorschlag (nicht bestätigt): Antwort
              {{ result.recognition.suggestedCorrectIndex + 1 }}.
            </p>
          }
          <label
            >{{ language.t('Errechnete Lösung (ungeprüft)')
            }}<textarea
              [(ngModel)]="result.recognition.computedSolution"
              maxlength="4000"
            ></textarea>
          </label>
          <label
            >{{ language.t('Lösungsweg (eine Zeile je Schritt)')
            }}<textarea [(ngModel)]="stepsText" maxlength="4000" rows="4"></textarea>
          </label>
          <label
            >{{ language.t('Erster Hinweis ohne Lösung')
            }}<textarea [(ngModel)]="result.recognition.hint" maxlength="500"></textarea>
          </label>
          <label
            >{{ language.t('Nächster Schritt ohne Lösung')
            }}<textarea [(ngModel)]="result.recognition.nextStep" maxlength="500"></textarea>
          </label>
          <label
            >{{ language.t('Musterlösung aus der Vorlage')
            }}<textarea
              [(ngModel)]="result.recognition.referenceSolution"
              maxlength="4000"
            ></textarea>
          </label>
          <p>
            Vorlagenabgleich: {{ comparison(result.recognition) }}. Textgleichheit beweist keine
            mathematische Richtigkeit.
          </p>
          @for (uncertainty of result.recognition.uncertainties; track $index) {
            <p>Unsicherheit: {{ uncertainty }}</p>
          }
          <button type="button" [disabled]="busy() || correctIndex < 0" (click)="checkSolution()">
            {{ language.t('Korrigierte Lösung unabhängig prüfen') }}
          </button>
          @if (verification(); as check) {
            <p role="status">
              {{
                check.status === 'verified'
                  ? 'Rechnerisch geprüft'
                  : check.status === 'conflict'
                    ? 'Widerspruch'
                    : 'Fachlich ungeprüft'
              }}:
              {{ check.reason }}
            </p>
          }
        </div>
        <label
          ><input
            type="checkbox"
            [(ngModel)]="reviewConfirmed"
            [disabled]="!verification() || verification()?.status === 'conflict'"
          />{{
            language.t(
              'Ich habe Bild, Frage, Antworten und Lösung geprüft oder offene Unsicherheiten erkannt. Nur einen privaten Entwurf speichern.'
            )
          }}</label
        >
        <button
          type="button"
          [disabled]="
            busy() || !reviewConfirmed || !verification() || verification()?.status === 'conflict'
          "
          (click)="saveDraft()"
        >
          {{ language.t('Als privaten Entwurf speichern') }}
        </button>
      }
      @if (savedId()) {
        <p role="status">
          {{
            language.t(
              'Privater Entwurf gespeichert. Er ist oben im Frageneditor geöffnet; Veröffentlichung und öffentliche Einreichung sind eigene Schritte.'
            )
          }}
        </p>
        <button type="button" (click)="startAgain()">
          {{ language.t('Weitere Aufgabe erfassen') }}
        </button>
      }
      @if (message()) {
        <p role="alert">{{ message() }}</p>
      }
    </section>
  `,
  styles: `
    .photo {
      margin: 2rem 0;
      padding: 1.5rem;
      border: 1px solid #dfe9df;
      border-radius: 1rem;
    }
    label {
      display: block;
      margin: 0.7rem 0;
    }
    canvas {
      display: block;
      max-width: 100%;
      max-height: 22rem;
      object-fit: contain;
    }
    canvas[hidden] {
      display: none;
    }
    textarea {
      display: block;
      width: min(100%, 45rem);
      min-height: 4rem;
    }
    button {
      padding: 0.6rem 1rem;
      margin: 0.4rem 0;
      cursor: pointer;
    }
    button:focus-visible,
    input:focus-visible,
    select:focus-visible,
    textarea:focus-visible {
      outline: 3px solid #e5a44c;
    }
  `,
})
export class PhotoDraft implements OnInit {
  readonly language = inject(LanguageService);
  @ViewChild('preview') preview?: ElementRef<HTMLCanvasElement>;
  readonly modes = signal<Mode[]>([]);
  readonly file = signal<File | null>(null);
  readonly previewReady = signal(false);
  readonly mediaId = signal('');
  readonly review = signal<Review | null>(null);
  readonly verification = signal<SolutionCheck | null>(null);
  readonly savedId = signal('');
  readonly message = signal('');
  readonly busy = signal(false);
  mode = 'off';
  cropX = 0;
  cropY = 0;
  cropWidth = 100;
  cropHeight = 100;
  altText = '';
  storageConfirmed = false;
  providerConfirmed = false;
  reviewConfirmed = false;
  referenceHint = '';
  correctIndex = -1;
  stepsText = '';
  disclosureVersion = '';
  private reviewRevision = 0;

  ngOnInit(): void {
    void this.reloadModes();
  }
  label(mode: string): string {
    return (
      (
        {
          off: 'Aus',
          'operator-cloud': 'Betreiber-Cloud',
          'operator-local': 'Betreiber-lokal',
          'user-key': 'Eigener API-Schlüssel',
        } as Record<string, string>
      )[mode] ?? mode
    );
  }
  selectedMode(): Mode | undefined {
    return this.modes().find((item) => item.info.mode === this.mode);
  }
  async reloadModes(): Promise<void> {
    const response = await fetch('/api/v1/ai/modes');
    if (!response.ok) return;
    const data = ((await response.json()) as { data: { modes: Mode[]; disclosureVersion: string } })
      .data;
    this.modes.set(data.modes);
    this.disclosureVersion = data.disclosureVersion;
  }
  selectFile(event: Event): void {
    if (this.mediaId()) {
      this.message.set(
        'Vor einem neuen Zuschnitt das bisherige private Bild verwerfen oder eine neue Aufgabe beginnen.',
      );
      (event.target as HTMLInputElement).value = '';
      return;
    }
    const selected = (event.target as HTMLInputElement).files?.item(0) ?? null;
    if (
      !selected ||
      !['image/jpeg', 'image/png'].includes(selected.type) ||
      selected.size > 5 * 1024 * 1024
    ) {
      this.message.set('Bitte JPEG/PNG bis 5 MiB auswählen.');
      this.file.set(null);
      return;
    }
    this.file.set(selected);
    this.previewReady.set(false);
    this.storageConfirmed = false;
    this.review.set(null);
    this.verification.set(null);
    this.message.set('');
  }
  invalidateCrop(): void {
    this.previewReady.set(false);
    this.storageConfirmed = false;
  }
  startAgain(): void {
    this.mediaId.set('');
    this.savedId.set('');
    this.review.set(null);
    this.verification.set(null);
    this.file.set(null);
    this.previewReady.set(false);
    this.storageConfirmed = false;
    this.providerConfirmed = false;
    this.reviewConfirmed = false;
    this.correctIndex = -1;
    this.message.set('Neue Aufgabe auswählen.');
  }
  async previewCrop(): Promise<void> {
    const file = this.file();
    const canvas = this.preview?.nativeElement;
    if (
      !file ||
      !canvas ||
      !Number.isFinite(this.cropX) ||
      !Number.isFinite(this.cropY) ||
      !Number.isFinite(this.cropWidth) ||
      !Number.isFinite(this.cropHeight) ||
      this.cropX < 0 ||
      this.cropY < 0 ||
      this.cropWidth <= 0 ||
      this.cropHeight <= 0 ||
      this.cropX + this.cropWidth > 100 ||
      this.cropY + this.cropHeight > 100
    ) {
      this.message.set('Der Zuschnitt muss innerhalb des Bildes liegen.');
      return;
    }
    try {
      const bitmap = await createImageBitmap(file);
      const width = (bitmap.width * this.cropWidth) / 100;
      const height = (bitmap.height * this.cropHeight) / 100;
      const scale = Math.min(1, 2048 / width, 2048 / height);
      canvas.width = Math.max(1, Math.round(width * scale));
      canvas.height = Math.max(1, Math.round(height * scale));
      canvas
        .getContext('2d')
        ?.drawImage(
          bitmap,
          (bitmap.width * this.cropX) / 100,
          (bitmap.height * this.cropY) / 100,
          width,
          height,
          0,
          0,
          canvas.width,
          canvas.height,
        );
      bitmap.close();
      this.previewReady.set(true);
      this.message.set('Zuschnitt lokal erstellt. Prüfe die Vorschau.');
    } catch {
      this.message.set('Das Bild konnte nicht gelesen werden.');
    }
  }
  async upload(): Promise<void> {
    const canvas = this.preview?.nativeElement;
    if (!canvas || !this.previewReady() || !this.storageConfirmed || !this.altText.trim()) return;
    this.busy.set(true);
    try {
      const blob = await new Promise<Blob | null>((resolve) =>
        canvas.toBlob(resolve, 'image/jpeg', 0.86),
      );
      if (
        !blob ||
        blob.size > 5 * 1024 * 1024 ||
        (this.mode !== 'off' && blob.size > (this.selectedMode()?.info.maxImageBytes ?? 0))
      ) {
        this.message.set('Zuschnitt zu groß. Wähle einen kleineren Bereich.');
        return;
      }
      const form = new FormData();
      form.append('file', blob, 'zuschnitt.jpg');
      form.append('altText', this.altText.trim());
      const response = await fetch('/api/v1/media/', { method: 'POST', body: form });
      if (!response.ok) throw new Error(`${response.status}`);
      this.mediaId.set(((await response.json()) as { data: { id: string } }).data.id);
      this.message.set('Bild ist nur in deinem Konto gespeichert.');
    } catch {
      this.message.set('Privater Bild-Upload fehlgeschlagen.');
    } finally {
      this.busy.set(false);
    }
  }
  async discard(): Promise<void> {
    if (!this.mediaId()) return;
    const response = await fetch(`/api/v1/media/${this.mediaId()}`, { method: 'DELETE' });
    if (response.ok) {
      this.mediaId.set('');
      this.review.set(null);
      this.previewReady.set(false);
      this.message.set('Privates Bild gelöscht.');
    } else this.message.set('Bild konnte nicht gelöscht werden.');
  }
  manualReview(): void {
    this.review.set({
      mediaId: this.mediaId(),
      mode: 'off',
      comparison: 'unknown',
      comparisonExplanation: '',
      verification: { status: 'unverified', reason: 'Bitte selbst prüfen.', expected: null },
      recognition: {
        detectedText: '',
        questionText: '',
        subject: '',
        topic: '',
        formula: '',
        drawingDescription: '',
        answers: ['', ''],
        suggestedCorrectIndex: null,
        computedSolution: '',
        steps: [],
        referenceSolution: null,
        uncertainties: ['Bitte Bild selbst lesen und Lösung prüfen.'],
        hint: '',
        nextStep: '',
      },
    });
    this.correctIndex = -1;
    this.stepsText = '';
    this.reviewConfirmed = false;
    this.verification.set(null);
  }
  async extract(): Promise<void> {
    if (!this.mediaId() || !this.providerConfirmed || this.mode === 'off') return;
    this.busy.set(true);
    this.review.set(null);
    try {
      const response = await fetch('/api/v1/ai/photo/extract', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          mediaId: this.mediaId(),
          mode: this.mode,
          disclosureVersion: this.disclosureVersion,
          confirmed: true,
          referenceSolutionHint: this.referenceHint.trim() || null,
        }),
      });
      if (!response.ok) throw new Error(`${response.status}`);
      const value = ((await response.json()) as { data: Review }).data;
      value.recognition.answers = value.recognition.answers.length
        ? value.recognition.answers
        : ['', ''];
      this.review.set(value);
      this.verification.set(null);
      this.stepsText = value.recognition.steps.join('\n');
      this.correctIndex = -1;
      this.reviewConfirmed = false;
      this.providerConfirmed = false;
      await this.reloadModes();
      this.message.set('Vorschlag erhalten. Prüfe ihn vor dem Speichern am Originalbild.');
    } catch {
      this.message.set(
        'Erkennung fehlgeschlagen. Du kannst ohne KI selbst erfassen oder erneut versuchen.',
      );
    } finally {
      this.busy.set(false);
    }
  }
  comparison(recognition: Recognition): string {
    if (!recognition.referenceSolution?.trim()) return 'Keine Musterlösung erkannt';
    const normalize = (text: string) => text.replace(/\s/g, '').toLocaleUpperCase();
    return normalize(recognition.referenceSolution) === normalize(recognition.computedSolution)
      ? 'Textgleichheit'
      : 'Textabweichung';
  }
  invalidateReview(): void {
    this.reviewRevision++;
    this.verification.set(null);
    this.reviewConfirmed = false;
  }
  async checkSolution(): Promise<void> {
    const recognition = this.review()?.recognition;
    if (!recognition || this.correctIndex < 0 || this.correctIndex >= recognition.answers.length)
      return;
    this.busy.set(true);
    const revision = this.reviewRevision;
    this.verification.set(null);
    this.reviewConfirmed = false;
    try {
      const response = await fetch('/api/v1/ai/photo/check', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          formula: recognition.formula,
          questionText: recognition.questionText,
          computedSolution: recognition.computedSolution,
          referenceSolution: recognition.referenceSolution,
          chosenAnswer: recognition.answers[this.correctIndex],
          steps: this.stepsText
            .split('\n')
            .map((step) => step.trim())
            .filter(Boolean),
        }),
      });
      if (!response.ok) throw new Error(`${response.status}`);
      const check = ((await response.json()) as { data: SolutionCheck }).data;
      if (revision !== this.reviewRevision) return;
      this.verification.set(check);
      this.message.set('Prüfstatus ansehen und Bild weiterhin selbst vergleichen.');
    } catch {
      this.message.set('Lösungsprüfung fehlgeschlagen. Bitte erneut versuchen.');
    } finally {
      this.busy.set(false);
    }
  }
  async saveDraft(): Promise<void> {
    const result = this.review();
    if (
      !result ||
      !this.reviewConfirmed ||
      !this.verification() ||
      this.verification()?.status === 'conflict' ||
      !this.mediaId() ||
      this.busy()
    )
      return;
    this.busy.set(true);
    result.recognition.steps = this.stepsText
      .split('\n')
      .map((step) => step.trim())
      .filter(Boolean);
    try {
      const response = await fetch('/api/v1/ai/photo/drafts', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          mediaId: this.mediaId(),
          recognition: result.recognition,
          correctIndex: this.correctIndex,
          confirmed: true,
        }),
      });
      if (!response.ok) {
        this.message.set(
          response.status === 409
            ? 'Widerspruch erkannt. Lösung und Antworten korrigieren, dann erneut prüfen.'
            : 'Entwurf konnte nicht gespeichert werden. Prüfe die Felder.',
        );
        this.invalidateReview();
        return;
      }
      const id = ((await response.json()) as { data: { questionId: string } }).data.questionId;
      this.savedId.set(id);
      window.dispatchEvent(new CustomEvent('learnpip:photo-draft', { detail: id }));
      this.message.set('Nur als privater Entwurf gespeichert.');
    } catch {
      this.message.set('Entwurf konnte nicht gespeichert werden. Prüfe die Felder.');
    } finally {
      this.busy.set(false);
    }
  }
}
