import { Component, inject, OnInit, signal } from '@angular/core';
import { LanguageService } from './language';
import { FormsModule } from '@angular/forms';

interface Part {
  code: string;
  title: string;
  questionCount: number;
  timeLimitMinutes: number;
  requiredCorrect: number;
  allowVariants: boolean;
}
interface Profile {
  id: string;
  code: string;
  title: string;
  amateurClass: string;
  version: number;
  revision: string;
  parts: Part[];
  sessions: ExamSession[];
  rulesSourceUrl: string | null;
  rulesCheckedOn: string | null;
  createdAtUtc: string;
}
interface ExamSession {
  date: string;
  place: string;
  registrationDeadline: string | null;
  sourceUrl: string;
  checkedOn: string;
}
interface Forecast {
  status: 'window' | 'insufficient';
  earliestReadyDate: string | null;
  latestReadyDate: string | null;
  suggestedExamDate: string | null;
  profileVersion: number;
  evidence: {
    answeredCatalogQuestions: number;
    catalogQuestions: number;
    spacedMasteredContents: number;
    ownContents: number;
    completedSimulations: number;
    recentSimulations: number;
    recentPassedSimulations: number;
  };
  reasons: string[];
  assumptions: string;
  formalAdmissionStatus: string;
  rulesSourceUrl: string | null;
  rulesCheckedOn: string | null;
  sessions: (ExamSession & { registrationStatus: 'open' | 'closed' | 'unknown' })[];
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
  selectedAnswers: Record<string, number>;
}
interface RunSummary {
  id: string;
  profileVersionId: string;
  completedAtUtc: string | null;
}
interface PowerTest {
  id: string;
  profileCode: string;
  catalogRevision: string;
  questionMode: string;
  stage: number;
  stages: number;
  completedAtUtc: string | null;
  questions: Question[];
  selectedAnswers: Record<string, number>;
  parts: { code: string; correct: number; total: number }[];
  mistakes: {
    code: string;
    partCode: string;
    prompt: string;
    selectedAnswer: string | null;
    correctAnswer: string;
  }[];
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
      <h2 id="exam-title">{{ language.t('Prüfungssimulation') }}</h2>
      <p>
        {{
          language.t(
            'Jeder verlangte Fachteil wird getrennt bewertet. Selbst gemeldete bestandene Teile ersetzen keinen amtlichen Nachweis.'
          )
        }}
      </p>
      <label
        >{{ language.t('Profilfassung')
        }}<select [(ngModel)]="profileId">
          <option value="">{{ language.t('Bitte auswählen') }}</option>
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
        <h3>Termine und Anmeldung · Profilfassung {{ profile.version }}</h3>
        @if (profile.rulesSourceUrl && profile.rulesCheckedOn) {
          <p>
            Prüfungsregeln geprüft am {{ profile.rulesCheckedOn }} ·
            <a [href]="profile.rulesSourceUrl">{{ language.t('Regelquelle') }}</a>
          </p>
        } @else {
          <p>
            {{
              language.t('Für diese ältere Profilfassung ist kein geprüfter Regelstand hinterlegt.')
            }}
          </p>
        }
        @for (session of profile.sessions; track session.date + session.place) {
          <p>
            {{ session.date }} · {{ session.place }} · Anmeldefrist:
            {{ session.registrationDeadline ?? 'nicht veröffentlicht' }} · Quelle geprüft am
            {{ session.checkedOn }} ·
            <a [href]="session.sourceUrl">{{ language.t('Terminquelle') }}</a>
          </p>
        } @empty {
          <p>
            {{ language.t('Für diese Profilfassung sind keine geprüften Termine hinterlegt.') }}
          </p>
        }
        <button type="button" (click)="loadForecast()">
          {{ language.t('Bereitschaft einschätzen') }}
        </button>
        @if (forecastProfileId === profile.id && forecast(); as estimate) {
          <section aria-label="Bereitschaftsprognose" role="status">
            <h3>{{ language.t('Lernbereitschaft') }}</h3>
            @if (estimate.status === 'window') {
              <p>
                Grobes Bereitschaftsfenster: {{ estimate.earliestReadyDate }} bis
                {{ estimate.latestReadyDate }}.
              </p>
            } @else {
              <p>
                {{
                  language.t('Ein belastbares Bereitschaftsfenster lässt sich noch nicht ableiten.')
                }}
              </p>
            }
            <p>
              Katalogabdeckung: {{ estimate.evidence.answeredCatalogQuestions }} /
              {{ estimate.evidence.catalogQuestions }} Fragegruppen beantwortet.
            </p>
            <p>
              Zeitversetzt sicher wiederholte eigene Inhalte:
              {{ estimate.evidence.spacedMasteredContents }} / {{ estimate.evidence.ownContents }}.
            </p>
            <p>
              Simulationen dieser Fassung:
              {{ estimate.evidence.completedSimulations }} abgeschlossen,
              {{ estimate.evidence.recentPassedSimulations }} in den letzten 30 Tagen bestanden.
            </p>
            @if (estimate.suggestedExamDate) {
              <p>
                Erster passender Termin mit bekannter offener Frist:
                {{ estimate.suggestedExamDate }}. Vor einer Anmeldung bei der Quelle erneut prüfen.
              </p>
            }
            @for (reason of estimate.reasons; track reason) {
              <p>{{ reason }}</p>
            }
            <p>{{ estimate.assumptions }}</p>
            <h3>{{ language.t('Formale Zulassung') }}</h3>
            <p>{{ estimate.formalAdmissionStatus }}</p>
            <p>{{ language.t('LearnPip meldet niemanden automatisch zur Prüfung an.') }}</p>
          </section>
        }
      }
      <label
        >{{ language.t('Fragen')
        }}<select [(ngModel)]="questionMode">
          <option value="original">{{ language.t('Originalfragen') }}</option>
          @if (selectedProfile()?.parts?.every((part) => part.allowVariants)) {
            <option value="variant">{{ language.t('Varianten') }}</option>
            <option value="mixed">{{ language.t('Gemischt') }}</option>
          }
        </select>
      </label>
      <button type="button" (click)="start()" [disabled]="!profileId">
        {{ language.t('Simulation starten') }}
      </button>
      <p>
        {{
          language.t(
            'Die Zeit läuft je Fachteil ab Start auch nach Schließen der Seite weiter. Offene Läufe können fortgesetzt werden.'
          )
        }}
      </p>
      @for (run of simulationHistory(); track run.id) {
        @if (!run.completedAtUtc) {
          <button type="button" (click)="resumeSimulation(run.id)">
            {{ language.t('Simulation fortsetzen') }}
          </button>
        }
      }
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
          <button type="button" (click)="finish()">{{ language.t('Fachteil abgeben') }}</button>
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
      <h2>{{ language.t('Powertest') }}</h2>
      <p>
        {{
          language.t(
            'Alle Fragen des gewählten Profils in Etappen ohne Zeitlimit. Die Fehleranalyse erscheint nach der letzten Etappe.'
          )
        }}
      </p>
      <label
        >{{ language.t('Fragen pro Etappe')
        }}<input type="number" min="1" max="100" [(ngModel)]="stageSize" />
      </label>
      <button type="button" (click)="startPower()" [disabled]="!profileId">
        {{ language.t('Powertest starten') }}
      </button>
      @for (run of powerHistory(); track run.id) {
        @if (!run.completedAtUtc) {
          <button type="button" (click)="resumePower(run.id)">
            {{ language.t('Powertest fortsetzen') }}
          </button>
        }
      }
      @if (powerTest(); as run) {
        <h3>{{ run.profileCode }} · Etappe {{ run.stage }} von {{ run.stages }}</h3>
        <p>Katalogfassung {{ run.catalogRevision }} · {{ run.questionMode }}</p>
        @if (!run.completedAtUtc) {
          @for (question of run.questions; track question.code) {
            <fieldset>
              <legend>{{ question.code }} · {{ question.prompt }}</legend>
              @for (answer of question.answers; track $index) {
                <label
                  ><input
                    type="radio"
                    [name]="'power-' + question.code"
                    [checked]="powerChoices[question.code] === $index"
                    (change)="choosePower(question.code, $index)"
                  />{{ answer }}</label
                >
              }
            </fieldset>
          }
          <button type="button" (click)="finishPower()">{{ language.t('Etappe abgeben') }}</button>
        } @else {
          <h4>{{ language.t('Auswertung je Fachteil') }}</h4>
          @for (part of run.parts; track part.code) {
            <p>{{ part.code }}: {{ part.correct }} / {{ part.total }} richtig</p>
          }
          <h4>{{ language.t('Fehleranalyse') }}</h4>
          @for (mistake of run.mistakes; track mistake.code) {
            <p>
              {{ mistake.partCode }} · {{ mistake.code }}: {{ mistake.prompt }}<br />
              Gewählt: {{ mistake.selectedAnswer ?? 'keine Antwort' }} · Richtig:
              {{ mistake.correctAnswer }}
            </p>
          } @empty {
            <p>{{ language.t('Alle Fragen richtig beantwortet.') }}</p>
          }
        }
      }
      <h3>{{ language.t('Bereits bestandene Fachbereiche (Selbstauskunft)') }}</h3>
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
        <h3>{{ language.t('Katalogquellen') }}</h3>
        <ul>
          @for (catalog of catalogs(); track catalog.id) {
            <li>
              {{ catalog.title }} · {{ catalog.revision }} ({{ catalog.changedOn }}) ·
              {{ catalog.license }} · {{ catalog.attribution }} ·
              <a [href]="catalog.sourceUrl">{{ language.t('Quelle') }}</a>
            </li>
          }
        </ul>
      }
      @if (admin()) {
        <details>
          <summary>{{ language.t('Offiziellen Katalog und Profilfassungen verwalten') }}</summary>
          <p>
            {{
              language.t(
                'JSON-Import mit Quelle, Lizenz, Urheberangabe, Fassungsdatum und bestätigten Rechten.'
              )
            }}
          </p>
          <label
            >{{ language.t('Katalogimport (JSON)') }}<textarea [(ngModel)]="catalogJson"></textarea>
          </label>
          <button type="button" (click)="submitAdmin('catalogs/import', catalogJson)">
            {{ language.t('Katalogfassung importieren') }}
          </button>
          <label
            >{{ language.t('Profilfassung (JSON)') }}<textarea [(ngModel)]="profileJson"></textarea>
          </label>
          <button type="button" (click)="submitAdmin('profiles/versions', profileJson)">
            {{ language.t('Profilfassung speichern') }}
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
  readonly language = inject(LanguageService);
  readonly profiles = signal<Profile[]>([]);
  readonly catalogs = signal<Catalog[]>([]);
  readonly credits = signal<string[]>([]);
  readonly simulation = signal<Simulation | null>(null);
  readonly simulationHistory = signal<RunSummary[]>([]);
  readonly powerHistory = signal<RunSummary[]>([]);
  readonly powerTest = signal<PowerTest | null>(null);
  readonly forecast = signal<Forecast | null>(null);
  readonly admin = signal(false);
  readonly message = signal('');
  readonly creditCodes = ['B', 'V', 'T-N', 'T-E', 'T-A'];
  profileId = '';
  forecastProfileId = '';
  questionMode = 'original';
  stageSize = 25;
  choices: Record<string, number> = {};
  powerChoices: Record<string, number> = {};
  catalogJson = '';
  profileJson = '';

  ngOnInit(): void {
    void this.reload();
  }
  selectedProfile(): Profile | undefined {
    return this.profiles().find((item) => item.id === this.profileId);
  }
  async loadForecast(): Promise<void> {
    const id = this.profileId;
    this.forecast.set(null);
    this.forecastProfileId = id;
    const response = await fetch(`/api/v1/exams/profiles/${id}/forecast`);
    if (response.ok) {
      if (this.profileId === id)
        this.forecast.set(((await response.json()) as { data: Forecast }).data);
    } else this.message.set(`Prognose konnte nicht geladen werden (${response.status}).`);
  }
  async reload(): Promise<void> {
    const [profiles, catalogs, credits, admin, simulations, powerTests] = await Promise.all([
      fetch('/api/v1/exams/profiles'),
      fetch('/api/v1/exams/catalogs'),
      fetch('/api/v1/exams/credits'),
      fetch('/api/v1/exams/admin/'),
      fetch('/api/v1/exams/simulations'),
      fetch('/api/v1/exams/power-tests'),
    ]);
    if (profiles.ok) this.profiles.set(((await profiles.json()) as { data: Profile[] }).data);
    if (catalogs.ok) this.catalogs.set(((await catalogs.json()) as { data: Catalog[] }).data);
    if (credits.ok)
      this.credits.set(
        ((await credits.json()) as { data: { code: string }[] }).data.map((item) => item.code),
      );
    this.admin.set(admin.ok);
    if (simulations.ok)
      this.simulationHistory.set(((await simulations.json()) as { data: RunSummary[] }).data);
    if (powerTests.ok)
      this.powerHistory.set(((await powerTests.json()) as { data: RunSummary[] }).data);
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
      questionMode: this.questionMode,
    });
    if (response.ok) {
      this.simulation.set(((await response.json()) as { data: Simulation }).data);
      this.choices = this.simulation()!.selectedAnswers;
      await this.reload();
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
      this.choices = this.simulation()!.selectedAnswers;
      await this.reload();
    } else this.message.set(`Fachteil konnte nicht abgeschlossen werden (${response.status}).`);
  }
  async resumeSimulation(id: string): Promise<void> {
    const response = await fetch(`/api/v1/exams/simulations/${id}`);
    if (response.ok) {
      const run = ((await response.json()) as { data: Simulation }).data;
      this.simulation.set(run);
      this.choices = run.selectedAnswers;
    }
  }
  async startPower(): Promise<void> {
    const response = await this.post('/api/v1/exams/power-tests', {
      profileVersionId: this.profileId,
      questionMode: this.questionMode,
      stageSize: this.stageSize,
    });
    if (response.ok) {
      const run = ((await response.json()) as { data: PowerTest }).data;
      this.powerTest.set(run);
      this.powerChoices = run.selectedAnswers;
      await this.reload();
    } else this.message.set(`Powertest konnte nicht gestartet werden (${response.status}).`);
  }
  async resumePower(id: string): Promise<void> {
    const response = await fetch(`/api/v1/exams/power-tests/${id}`);
    if (response.ok) {
      const run = ((await response.json()) as { data: PowerTest }).data;
      this.powerTest.set(run);
      this.powerChoices = run.selectedAnswers;
    }
  }
  async choosePower(code: string, index: number): Promise<void> {
    const run = this.powerTest();
    if (!run) return;
    const response = await fetch(
      `/api/v1/exams/power-tests/${run.id}/answers/${encodeURIComponent(code)}`,
      {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ selectedIndex: index }),
      },
    );
    if (response.ok) this.powerChoices[code] = index;
    else this.message.set(`Antwort konnte nicht gespeichert werden (${response.status}).`);
  }
  async finishPower(): Promise<void> {
    const run = this.powerTest();
    if (!run) return;
    const response = await this.post(`/api/v1/exams/power-tests/${run.id}/stages/finish`, {});
    if (response.ok) {
      const next = ((await response.json()) as { data: PowerTest }).data;
      this.powerTest.set(next);
      this.powerChoices = next.selectedAnswers;
      await this.reload();
    } else this.message.set(`Etappe konnte nicht abgeschlossen werden (${response.status}).`);
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
