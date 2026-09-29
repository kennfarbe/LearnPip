import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface Content {
  id: string;
  title: string;
  mastered: boolean;
}
interface Result {
  estimatedDimension: string;
  scopeContents: number;
  dailyMinutes: number;
  targetPercent: number;
  availableContents: number;
  masteredContents: number;
  planningDays: number;
  studyDays: number;
  expectedQuotePercent: number;
  conservativeQuotePercent: number;
  feasible: boolean;
  suggestedDailyMinutes: number | null;
  suggestedScopeContents: number | null;
  suggestedExamDate: string | null;
  options: string[];
  assumptions: string;
}

@Component({
  selector: 'app-exam-plan',
  imports: [FormsModule],
  template: `
    <section class="exam-plan" aria-labelledby="exam-plan-title">
      <h2 id="exam-plan-title">Prüfungsziel planen</h2>
      <p>Wähle zwei Größen aus. Die dritte wird grob geschätzt; es gibt keine Erfolgsgarantie.</p>
      @if (contents().length) {
        <fieldset>
          <legend>Lerninhalte im Stoffumfang ({{ selected.length }})</legend>
          @for (item of contents(); track item.id) {
            <label
              ><input
                type="checkbox"
                [checked]="selected.includes(item.id)"
                (change)="toggle(item.id)"
              />
              {{ item.title }} {{ item.mastered ? '· bisher sicher' : '' }}</label
            >
          }
        </fieldset>
        <label
          >Zu schätzende Größe
          <select [(ngModel)]="missing">
            <option value="target">Beherrschungsgrad</option>
            <option value="time">Lernzeit</option>
            <option value="scope">Stoffumfang</option>
          </select></label
        >
        <label
          >Lernzeit pro Tag in Minuten
          <input
            type="number"
            [(ngModel)]="minutes"
            min="1"
            max="480"
            [disabled]="missing === 'time'"
        /></label>
        <label
          >Gewünschter Beherrschungsgrad in Prozent
          <input
            type="number"
            [(ngModel)]="target"
            min="1"
            max="100"
            [disabled]="missing === 'target'"
        /></label>
        <label
          >Tageslimit in Minuten <input type="number" [(ngModel)]="limit" min="5" max="480"
        /></label>
        <label>Optionaler Prüfungstermin <input type="date" [(ngModel)]="examDate" /></label>
        @if (!examDate) {
          <label
            >Planungshorizont in Tagen <input type="number" [(ngModel)]="horizon" min="1" max="365"
          /></label>
        }
        <fieldset>
          <legend>Schultage (halbe verfügbare Lernzeit)</legend>
          @for (day of days; track day.id) {
            <label
              ><input
                type="checkbox"
                [checked]="schoolDays.includes(day.id)"
                (change)="toggleSchool(day.id)"
              />{{ day.name }}</label
            >
          }
        </fieldset>
        <label
          >Pausentage (Datum, kommagetrennt)
          <input type="text" [(ngModel)]="breaks" placeholder="2026-10-03, 2026-10-04"
        /></label>
        <button type="button" (click)="estimate()" [disabled]="busy() || !selected.length">
          Schätzen
        </button>
        @if (result(); as plan) {
          <div role="status">
            <h3>{{ plan.feasible ? 'Unter Annahmen machbar' : 'Ziel so nicht plausibel' }}</h3>
            <p>
              {{ plan.scopeContents }} Inhalte · {{ plan.dailyMinutes }} Minuten pro Tag · Ziel
              {{ plan.targetPercent }} %
            </p>
            <p>
              {{ plan.masteredContents }} bisher sicher · {{ plan.studyDays }} Lerntage von
              {{ plan.planningDays }} Tagen.
            </p>
            <p>
              Geschätzte Quote: {{ plan.conservativeQuotePercent }}–{{ plan.expectedQuotePercent }}
              %.
            </p>
            @if (!plan.feasible) {
              <h4>Handlungsoptionen</h4>
              <ul>
                @for (option of plan.options; track option) {
                  <li>{{ option }}</li>
                }
              </ul>
              @if (plan.suggestedExamDate) {
                <p>Möglicher späterer Termin: {{ plan.suggestedExamDate }}</p>
              }
              @if (plan.suggestedDailyMinutes && plan.suggestedDailyMinutes <= limit) {
                <p>Geschätzter Bedarf: {{ plan.suggestedDailyMinutes }} Minuten pro Tag.</p>
              }
              @if (plan.suggestedScopeContents !== null) {
                <p>Alternativer Umfang: {{ plan.suggestedScopeContents }} Inhalte.</p>
              }
            }
            <p>{{ plan.assumptions }}</p>
          </div>
        }
      } @else {
        <p>Erstelle zuerst Lerninhalte, um einen Plan zu schätzen.</p>
      }
      @if (error()) {
        <p role="alert">{{ error() }}</p>
      }
    </section>
  `,
  styles: `
    .exam-plan {
      margin: 2rem 0;
      padding: 1.5rem;
      border: 1px solid #dfe9df;
      border-radius: 1rem;
    }
    label {
      display: block;
      margin: 0.6rem 0;
    }
    input,
    select {
      margin-left: 0.4rem;
      max-width: 100%;
    }
    fieldset {
      margin: 1rem 0;
      border: 1px solid #dfe9df;
    }
    button {
      padding: 0.6rem 1rem;
      cursor: pointer;
    }
    button:focus-visible,
    input:focus-visible,
    select:focus-visible {
      outline: 3px solid #e5a44c;
    }
  `,
})
export class ExamPlan implements OnInit {
  readonly contents = signal<Content[]>([]);
  readonly result = signal<Result | null>(null);
  readonly error = signal('');
  readonly busy = signal(false);
  readonly days = [
    { id: 1, name: 'Mo' },
    { id: 2, name: 'Di' },
    { id: 3, name: 'Mi' },
    { id: 4, name: 'Do' },
    { id: 5, name: 'Fr' },
    { id: 6, name: 'Sa' },
    { id: 7, name: 'So' },
  ];
  selected: string[] = [];
  schoolDays: number[] = [];
  missing = 'target';
  minutes = 30;
  target = 80;
  limit = 120;
  examDate = '';
  horizon = 28;
  breaks = '';

  ngOnInit(): void {
    void this.load();
  }
  private async load(): Promise<void> {
    const response = await fetch('/api/v1/learning/review');
    if (!response.ok) return;
    const items = ((await response.json()) as { data: { contents: Content[] } }).data.contents;
    this.contents.set(items);
    this.selected = items.map((item) => item.id);
  }
  toggle(id: string): void {
    this.selected = this.selected.includes(id)
      ? this.selected.filter((item) => item !== id)
      : [...this.selected, id];
  }
  toggleSchool(day: number): void {
    this.schoolDays = this.schoolDays.includes(day)
      ? this.schoolDays.filter((item) => item !== day)
      : [...this.schoolDays, day];
  }
  async estimate(): Promise<void> {
    this.busy.set(true);
    this.error.set('');
    this.result.set(null);
    const breaks = this.breaks
      .split(',')
      .map((item) => item.trim())
      .filter(Boolean);
    try {
      const response = await fetch('/api/v1/learning/exam-plan/estimate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          scopeContents: this.missing === 'scope' ? null : this.selected.length,
          dailyMinutes: this.missing === 'time' ? null : this.minutes,
          targetPercent: this.missing === 'target' ? null : this.target,
          examDate: this.examDate || null,
          horizonDays: this.examDate ? null : this.horizon,
          dailyLimitMinutes: this.limit,
          schoolDays: this.schoolDays,
          breakDays: breaks,
          contentIds: this.selected,
        }),
      });
      if (!response.ok) throw new Error();
      this.result.set(((await response.json()) as { data: Result }).data);
    } catch {
      this.error.set('Plan konnte nicht geschätzt werden. Prüfe die Eingaben.');
    } finally {
      this.busy.set(false);
    }
  }
}
