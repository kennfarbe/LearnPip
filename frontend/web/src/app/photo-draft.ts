import { Component, ElementRef, OnInit, ViewChild, signal } from '@angular/core';
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
}
interface Review {
  recognition: Recognition;
  comparison: string;
  comparisonExplanation: string;
  mediaId: string;
  mode: string;
}

@Component({
  selector: 'app-photo-draft',
  imports: [FormsModule],
  template: `
    <section class="photo" aria-labelledby="photo-title">
      <h2 id="photo-title">Foto in privaten Aufgabenentwurf übernehmen</h2>
      <p>
        Zuschnitt und Vorschau entstehen zuerst auf deinem Gerät. Das zugeschnittene Foto wird nur
        nach deiner Freigabe in deinem privaten Konto gespeichert. Eine KI-Auswertung ist optional.
      </p>
      <label
        >Foto auswählen (JPEG oder PNG, höchstens 5 MiB)
        <input type="file" accept="image/jpeg,image/png" (change)="selectFile($event)" />
      </label>
      @if (file()) {
        <fieldset>
          <legend>Bildbereich in Prozent zuschneiden</legend>
          <label
            >Links
            <input
              type="number"
              min="0"
              max="99"
              [(ngModel)]="cropX"
              (ngModelChange)="invalidateCrop()"
          /></label>
          <label
            >Oben
            <input
              type="number"
              min="0"
              max="99"
              [(ngModel)]="cropY"
              (ngModelChange)="invalidateCrop()"
          /></label>
          <label
            >Breite
            <input
              type="number"
              min="1"
              max="100"
              [(ngModel)]="cropWidth"
              (ngModelChange)="invalidateCrop()"
          /></label>
          <label
            >Höhe
            <input
              type="number"
              min="1"
              max="100"
              [(ngModel)]="cropHeight"
              (ngModelChange)="invalidateCrop()"
          /></label>
          <button type="button" (click)="previewCrop()">Zuschnitt anzeigen</button>
        </fieldset>
      }
      <canvas
        #preview
        [hidden]="!previewReady()"
        aria-label="Vorschau des zugeschnittenen Fotos"
      ></canvas>
      <label
        >Bildbeschreibung für deinen Entwurf
        <input [(ngModel)]="altText" maxlength="300" placeholder="Was zeigt das Foto?" />
      </label>
      <label
        >Betriebsart
        <select [(ngModel)]="mode" (ngModelChange)="providerConfirmed = false">
          @for (entry of modes(); track entry.info.mode) {
            <option [value]="entry.info.mode" [disabled]="!entry.info.available">
              {{ label(entry.info.mode) }}{{ entry.info.available ? '' : ' · nicht verfügbar' }}
            </option>
          }
        </select>
      </label>
      @if (selectedMode(); as selected) {
        <p><strong>Empfänger einer späteren KI-Anfrage:</strong> {{ selected.info.recipient }}.</p>
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
          ><input type="checkbox" [(ngModel)]="storageConfirmed" /> Ich möchte nur diesen Zuschnitt
          privat bei LearnPip hochladen.</label
        >
        <button
          type="button"
          [disabled]="busy() || !storageConfirmed || !altText.trim()"
          (click)="upload()"
        >
          Zuschnitt privat hochladen
        </button>
      }
      @if (mediaId()) {
        <p role="status">Zugeschnittenes Bild privat gespeichert.</p>
        @if (!savedId()) {
          <button type="button" (click)="discard()" [disabled]="busy()">
            Privates Bild verwerfen
          </button>
        }
        @if (!savedId() && mode !== 'off' && selectedMode()?.info?.available) {
          <label
            >Musterlösung aus der Vorlage (falls lesbar, optional)
            <textarea [(ngModel)]="referenceHint" maxlength="4000"></textarea>
          </label>
          <label
            ><input type="checkbox" [(ngModel)]="providerConfirmed" /> Ich darf das Foto verarbeiten
            und bestätige die Übermittlung des Zuschnitts und der optionalen Musterlösung genau an
            den angezeigten Empfänger.</label
          >
          <button type="button" [disabled]="busy() || !providerConfirmed" (click)="extract()">
            Foto mit gewähltem Anbieter analysieren
          </button>
        } @else if (!savedId() && mode === 'off' && !review()) {
          <button type="button" (click)="manualReview()">Ohne KI selbst erfassen</button>
        }
      }
      @if (!savedId() && review(); as result) {
        <div (input)="reviewConfirmed = false">
          <h3>Erkennung prüfen und korrigieren</h3>
          <p>
            Ungeprüfter Vorschlag. Unleserliche Zeichen, Formeln und Zeichnungen im Foto
            vergleichen.
          </p>
          <label
            >Erkannter Text
            <textarea [(ngModel)]="result.recognition.detectedText" rows="3"></textarea>
          </label>
          <label
            >Frage
            <textarea
              [(ngModel)]="result.recognition.questionText"
              maxlength="4000"
              rows="3"
            ></textarea>
          </label>
          <label
            >Formel <textarea [(ngModel)]="result.recognition.formula" maxlength="2000"></textarea>
          </label>
          <label
            >Zeichnung/Bildbeschreibung
            <textarea
              [(ngModel)]="result.recognition.drawingDescription"
              maxlength="2000"
            ></textarea>
          </label>
          <label>Fach <input [(ngModel)]="result.recognition.subject" maxlength="120" /></label>
          <label>Thema <input [(ngModel)]="result.recognition.topic" maxlength="120" /></label>
          <fieldset>
            <legend>Antworten · richtige Lösung selbst festlegen</legend>
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
                />Von mir als richtig geprüft</label
              >
            }
            <button
              type="button"
              [disabled]="result.recognition.answers.length >= 8"
              (click)="result.recognition.answers.push(''); reviewConfirmed = false"
            >
              Antwort ergänzen
            </button>
          </fieldset>
          @if (result.recognition.suggestedCorrectIndex !== null) {
            <p>
              KI-Vorschlag (nicht bestätigt): Antwort
              {{ result.recognition.suggestedCorrectIndex + 1 }}.
            </p>
          }
          <label
            >Errechnete Lösung (ungeprüft)
            <textarea [(ngModel)]="result.recognition.computedSolution" maxlength="4000"></textarea>
          </label>
          <label
            >Lösungsweg (eine Zeile je Schritt)
            <textarea [(ngModel)]="stepsText" maxlength="4000" rows="4"></textarea>
          </label>
          <label
            >Musterlösung aus der Vorlage
            <textarea
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
        </div>
        <label
          ><input type="checkbox" [(ngModel)]="reviewConfirmed" /> Ich habe Bild, Frage, Antworten
          und Lösung geprüft oder offene Unsicherheiten erkannt. Nur einen privaten Entwurf
          speichern.</label
        >
        <button type="button" [disabled]="busy() || !reviewConfirmed" (click)="saveDraft()">
          Als privaten Entwurf speichern
        </button>
      }
      @if (savedId()) {
        <p role="status">
          Privater Entwurf gespeichert. Er ist oben im Frageneditor geöffnet; Veröffentlichung und
          öffentliche Einreichung sind eigene Schritte.
        </p>
        <button type="button" (click)="startAgain()">Weitere Aufgabe erfassen</button>
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
  @ViewChild('preview') preview?: ElementRef<HTMLCanvasElement>;
  readonly modes = signal<Mode[]>([]);
  readonly file = signal<File | null>(null);
  readonly previewReady = signal(false);
  readonly mediaId = signal('');
  readonly review = signal<Review | null>(null);
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
      },
    });
    this.correctIndex = -1;
    this.stepsText = '';
    this.reviewConfirmed = false;
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
  async saveDraft(): Promise<void> {
    const result = this.review();
    if (!result || !this.reviewConfirmed || !this.mediaId() || this.busy()) return;
    this.busy.set(true);
    const text = (value: string) => (value.trim() ? [{ kind: 'text', text: value.trim() }] : []);
    const explanation = [
      result.recognition.computedSolution.trim(),
      this.stepsText.trim(),
      result.recognition.referenceSolution?.trim()
        ? `Musterlösung aus der Vorlage: ${result.recognition.referenceSolution.trim()}`
        : '',
      result.recognition.drawingDescription?.trim()
        ? `Zeichnung: ${result.recognition.drawingDescription.trim()}`
        : '',
      result.recognition.detectedText.trim()
        ? `Erkannter Originaltext: ${result.recognition.detectedText.trim()}`
        : '',
      result.recognition.uncertainties.length
        ? `Ungeprüft: ${result.recognition.uncertainties.join('; ')}`
        : '',
    ]
      .filter(Boolean)
      .join('\n');
    const content = {
      selectionMode: 'single',
      subject: result.recognition.subject.trim(),
      topic: result.recognition.topic.trim(),
      language: 'de',
      source: 'Privater Fotoentwurf',
      license: '',
      prompt: [
        ...text(
          [
            result.recognition.questionText,
            result.recognition.formula?.trim()
              ? `Formel: ${result.recognition.formula.trim()}`
              : '',
          ]
            .filter(Boolean)
            .join('\n')
            .slice(0, 4000),
        ),
        { kind: 'image', mediaId: this.mediaId() },
      ],
      explanation: text(explanation.slice(0, 4000)),
      answers: result.recognition.answers.map((answer, index) => ({
        isCorrect: index === this.correctIndex,
        blocks: text(answer),
      })),
    };
    try {
      const response = await fetch('/api/v1/questions/drafts', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ content, catalogId: null }),
      });
      if (!response.ok) throw new Error(`${response.status}`);
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
