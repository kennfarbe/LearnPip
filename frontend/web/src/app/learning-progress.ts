import { Component, OnDestroy, OnInit, signal } from '@angular/core';

interface Topic {
  subject: string;
  topic: string;
  totalContents: number;
  masteredContents: number;
  improvedContents: number;
}
interface Week {
  label: string;
  completedSessions: number;
  activeDays: number;
}
interface Progress {
  totalContents: number;
  masteredContents: number;
  improvedContents: number;
  participationPoints: number;
  learningDays: number;
  topics: Topic[];
  recentWeeks: Week[];
}

@Component({
  selector: 'app-learning-progress',
  template: `
    <section class="progress-panel" aria-labelledby="progress-title">
      <div class="heading">
        <div>
          <h2 id="progress-title">Dein Lernweg</h2>
          <p>Jeder kleine Schritt zählt. Eine Pause nimmt dir keinen Fortschritt weg.</p>
        </div>
        <button type="button" class="secondary" (click)="refresh()">Aktualisieren</button>
      </div>
      @if (progress(); as data) {
        <div class="highlights">
          <div>
            <strong>{{ data.masteredContents }} / {{ data.totalContents }}</strong
            ><span>Lerninhalte sicher</span>
          </div>
          <div>
            <strong>{{ data.improvedContents }}</strong
            ><span>Schwierigkeiten überwunden</span>
          </div>
          <div>
            <strong>{{ data.learningDays }}</strong
            ><span>Lerntage insgesamt</span>
          </div>
          <div>
            <strong>{{ data.participationPoints }}</strong
            ><span>Mitmachpunkte</span>
          </div>
        </div>
        <p class="note">
          Punkte gibt es für bewusstes Üben und Erklärungen, begrenzt pro Inhalt und Tag. Die
          Antwortgeschwindigkeit zählt nicht.
        </p>
        @if (data.topics.length) {
          <h3>Deine Themen</h3>
          <ul class="topics">
            @for (topic of data.topics; track topic.subject + ':' + topic.topic) {
              <li>
                <div class="topic-head">
                  <strong>{{ topic.subject }} · {{ topic.topic }}</strong>
                  <span>{{ topic.masteredContents }} von {{ topic.totalContents }} sicher</span>
                </div>
                <div
                  class="bar"
                  role="progressbar"
                  [attr.aria-valuenow]="topic.masteredContents"
                  [attr.aria-valuemax]="topic.totalContents"
                  aria-valuemin="0"
                  [attr.aria-label]="topic.subject + ' ' + topic.topic"
                >
                  <span [style.width.%]="percent(topic)"></span>
                </div>
                @if (topic.improvedContents) {
                  <p class="improved">
                    {{ topic.improvedContents }} Inhalt(e) nach anfänglicher Unsicherheit besser
                    beantwortet
                  </p>
                }
              </li>
            }
          </ul>
        } @else {
          <p>Erstelle und veröffentliche eine Frage, um deinen Lernweg zu beginnen.</p>
        }
        <h3>Kurze Einheiten in den letzten vier Wochen</h3>
        <div class="weeks">
          @for (week of data.recentWeeks; track week.label) {
            <div>
              <span>{{ week.label }}</span>
              <strong>{{ week.completedSessions }} Einheit(en)</strong>
              <small>{{ week.activeDays }} Lerntag(e)</small>
            </div>
          }
        </div>
        <p class="note">
          Hier gibt es keine tägliche Serie, die nach einer Pause verloren geht. Deine Ergebnisse
          bleiben erhalten.
        </p>
      } @else if (error()) {
        <p role="status">{{ error() }}</p>
      }
    </section>
  `,
  styleUrl: './learning-progress.css',
})
export class LearningProgress implements OnInit, OnDestroy {
  readonly progress = signal<Progress | null>(null);
  readonly error = signal('');
  private readonly onChanged = () => void this.refresh();

  ngOnInit(): void {
    window.addEventListener('learnpip:progress-changed', this.onChanged);
    void this.refresh();
  }

  ngOnDestroy(): void {
    window.removeEventListener('learnpip:progress-changed', this.onChanged);
  }

  async refresh(): Promise<void> {
    try {
      const response = await fetch('/api/v1/learning/progress', { credentials: 'same-origin' });
      if (!response.ok) {
        this.error.set('Melde dich an, um deinen Lernweg zu sehen.');
        return;
      }
      this.progress.set(((await response.json()) as { data: Progress }).data);
      this.error.set('');
    } catch {
      this.error.set('Fortschritt konnte nicht geladen werden.');
    }
  }

  percent(topic: Topic): number {
    return topic.totalContents ? (topic.masteredContents / topic.totalContents) * 100 : 0;
  }
}
