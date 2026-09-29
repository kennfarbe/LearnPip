import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface Part {
  code: string;
  title: string;
  questionCount: number;
  timeLimitMinutes: number;
  requiredCorrect: number;
}
interface Profile {
  id: string;
  code: string;
  title: string;
  amateurClass: string;
  version: number;
  revision: string;
  parts: Part[];
}
interface Question {
  code: string;
  prompt: string;
  answers: string[];
}
interface PartResult {
  code: string;
  correct: number;
  total: number;
  passed: boolean;
  credited: boolean;
  timedOut: boolean;
}
interface Simulation {
  id: string;
  profileVersionId: string;
  profileVersion: number;
  profileCode: string;
  catalogRevision: string;
  currentPartCode: string | null;
  deadlineAtUtc: string | null;
  questions: Question[];
  parts: PartResult[];
  passed: boolean | null;
  completedAtUtc: string | null;
}
interface Catalog {
  id: string;
  code: string;
  title: string;
  revision: string;
  license: string;
  sourceUrl: string;
  attribution: string;
  changedOn: string;
}

@Component({
  selector: 'app-exam-profiles',
  imports: [FormsModule],
  template: `
    <section class="exams" aria-labelledby="exam-title">
      <h2 id="exam-title">Prüfungssimulation</h2>
      <p>
        Jeder verlangte Fachteil wird getrennt bewertet. Selbst gemeldete bestandene Teile ersetzen
        keinen amtlichen Nachweis.
      </p>
      <label
        >Profilfassung
        <select [(ngModel)]="profileId">
          <option value="">Bitte auswählen</option>
          @for (profile of profiles(); track profile.id) {
            <option [value]="profile.id">
              {{ profile.title }} · Fassung {{ profile.version }} · Katalog {{ profile.revision }}
            </option>
          }
        </select></label
      >
      @if (selectedProfile(); as profile) {
        <ul>
          @for (part of profile.parts; track part.code) {
            <li>
              {{ part.title }}: {{ part.questionCount }} Fragen,
              {{ part.timeLimitMinutes }} Minuten, mindestens {{ part.requiredCorrect }} richtig
            </li>
          }
        </ul>
      }
      <button type="button" (click)="start()" [disabled]="!profileId">Simulation starten</button>
      @if (simulation(); as run) {
        <h3>Simulation {{ run.profileCode }} · Profilversion {{ run.profileVersion }}</h3>
        <p>Katalogfassung {{ run.catalogRevision }}</p>
        @if (run.currentPartCode) {
          <p>Aktueller Fachteil: {{ run.currentPartCode }} · Frist: {{ run.deadlineAtUtc }}</p>
          @for (question of run.questions; track question.code) {
            <fieldset>
              <legend>{{ question.code }} · {{ question.prompt }}</legend>
              @for (answer of question.answers; track $index) {
                <label
                  ><input
                    type="radio"
                    [name]="question.code"
                    [value]="$index"
                    [checked]="choices[question.code] === $index"
                    (change)="choose(question.code, $index)"
                  />{{ answer }}</label
                >
              }
            </fieldset>
          }
          <button type="button" (click)="finish()">Fachteil abgeben</button>
        } @else {
          <p role="status">
            {{
              run.passed
                ? 'Alle erforderlichen Fachbereiche bestanden.'
                : 'Nicht alle Fachbereiche bestanden.'
            }}
          </p>
        }
        <ul>
          @for (part of run.parts; track part.code) {
            <li>
              {{ part.code }}:
              {{
                part.credited
                  ? 'als bereits bestanden gemeldet'
                  : part.correct + ' / ' + part.total + ' richtig'
              }}
              · {{ part.passed ? 'bestanden' : 'nicht bestanden'
              }}{{ part.timedOut ? ' · Zeit abgelaufen' : '' }}
            </li>
          }
        </ul>
      }
      <h3>Bereits bestandene Fachbereiche (Selbstauskunft)</h3>
      @for (code of creditCodes; track code) {
        <label
          ><input
            type="checkbox"
            [checked]="credits().includes(code)"
            (change)="toggleCredit(code)"
          />{{ code }}</label
        >
      }
      @if (catalogs().length) {
        <h3>Katalogquellen</h3>
        <ul>
          @for (catalog of catalogs(); track catalog.id) {
            <li>
              {{ catalog.title }} · {{ catalog.revision }} ({{ catalog.changedOn }}) ·
              {{ catalog.license }} · {{ catalog.attribution }} ·
              <a [href]="catalog.sourceUrl">Quelle</a>
            </li>
          }
        </ul>
      }
      @if (admin()) {
        <details>
          <summary>Offiziellen Katalog und Profilfassungen verwalten</summary>
          <p>
            JSON-Import mit Quelle, Lizenz, Urheberangabe, Fassungsdatum und bestätigten Rechten.
          </p>
          <label>Katalogimport (JSON) <textarea [(ngModel)]="catalogJson"></textarea></label>
          <button type="button" (click)="submitAdmin('catalogs/import', catalogJson)">
            Katalogfassung importieren
          </button>
          <label>Profilfassung (JSON) <textarea [(ngModel)]="profileJson"></textarea></label>
          <button type="button" (click)="submitAdmin('profiles/versions', profileJson)">
            Profilfassung speichern
          </button>
        </details>
      }
      @if (message()) {
        <p role="status">{{ message() }}</p>
      }
    </section>
  `,
  styles: `
    .exams {
      margin: 2rem 0;
      padding: 1.5rem;
      border: 1px solid #dfe9df;
      border-radius: 1rem;
    }
    label {
      display: block;
      margin: 0.6rem 0;
    }
    fieldset {
      margin: 1rem 0;
      border: 1px solid #dfe9df;
    }
    textarea {
      display: block;
      width: min(100%, 45rem);
      min-height: 8rem;
    }
    button {
      padding: 0.6rem 1rem;
      margin: 0.4rem 0;
      cursor: pointer;
    }
    button:focus-visible,
    input:focus-visible,
    select:focus-visible {
      outline: 3px solid #e5a44c;
    }
  `,
})
export class ExamProfiles implements OnInit {
  readonly profiles = signal<Profile[]>([]);
  readonly catalogs = signal<Catalog[]>([]);
  readonly credits = signal<string[]>([]);
  readonly simulation = signal<Simulation | null>(null);
  readonly admin = signal(false);
  readonly message = signal('');
  readonly creditCodes = ['B', 'V', 'T-N', 'T-E', 'T-A'];
  profileId = '';
  choices: Record<string, number> = {};
  catalogJson = '';
  profileJson = '';

  ngOnInit(): void {
    void this.reload();
  }
  selectedProfile(): Profile | undefined {
    return this.profiles().find((item) => item.id === this.profileId);
  }
  async reload(): Promise<void> {
    const [profiles, catalogs, credits, admin] = await Promise.all([
      fetch('/api/v1/exams/profiles'),
      fetch('/api/v1/exams/catalogs'),
      fetch('/api/v1/exams/credits'),
      fetch('/api/v1/exams/admin/'),
    ]);
    if (profiles.ok) this.profiles.set(((await profiles.json()) as { data: Profile[] }).data);
    if (catalogs.ok) this.catalogs.set(((await catalogs.json()) as { data: Catalog[] }).data);
    if (credits.ok)
      this.credits.set(
        ((await credits.json()) as { data: { code: string }[] }).data.map((item) => item.code),
      );
    this.admin.set(admin.ok);
  }
  private async post(path: string, body: object): Promise<Response> {
    return fetch(path, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
  }
  async start(): Promise<void> {
    const response = await this.post('/api/v1/exams/simulations', {
      profileVersionId: this.profileId,
    });
    if (response.ok) {
      this.simulation.set(((await response.json()) as { data: Simulation }).data);
      this.choices = {};
    } else this.message.set(`Simulation konnte nicht gestartet werden (${response.status}).`);
  }
  async choose(code: string, index: number): Promise<void> {
    const run = this.simulation();
    if (!run) return;
    const response = await fetch(
      `/api/v1/exams/simulations/${run.id}/answers/${encodeURIComponent(code)}`,
      {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ selectedIndex: index }),
      },
    );
    if (response.ok) this.choices[code] = index;
    else this.message.set(`Antwort konnte nicht gespeichert werden (${response.status}).`);
  }
  async finish(): Promise<void> {
    const run = this.simulation();
    if (!run || !run.currentPartCode) return;
    const response = await this.post(
      `/api/v1/exams/simulations/${run.id}/parts/${encodeURIComponent(run.currentPartCode)}/finish`,
      {},
    );
    if (response.ok) {
      this.simulation.set(((await response.json()) as { data: Simulation }).data);
      this.choices = {};
    } else this.message.set(`Fachteil konnte nicht abgeschlossen werden (${response.status}).`);
  }
  async toggleCredit(code: string): Promise<void> {
    const enabled = this.credits().includes(code);
    const response = await fetch(`/api/v1/exams/credits${enabled ? '/' + code : ''}`, {
      method: enabled ? 'DELETE' : 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: enabled ? undefined : JSON.stringify({ code }),
    });
    if (response.ok) await this.reload();
    else this.message.set('Selbstauskunft konnte nicht gespeichert werden.');
  }
  async submitAdmin(path: string, raw: string): Promise<void> {
    try {
      const payload = JSON.parse(raw) as object;
      const response = await this.post(`/api/v1/exams/admin/${path}`, payload);
      if (!response.ok) throw new Error(`${response.status}`);
      this.message.set('Neue Fassung gespeichert.');
      await this.reload();
    } catch {
      this.message.set('Import fehlgeschlagen. Bitte JSON und Berechtigungen prüfen.');
    }
  }
}
