import { Injectable, signal } from '@angular/core';
import type { Feedback, Session } from './learning-session';

@Injectable({ providedIn: 'root' })
export class LearningSessionState {
  readonly session = signal<Session | null>(null);
  readonly selected = signal<string[]>([]);
  readonly feedback = signal<Feedback | null>(null);
  readonly showFull = signal(false);
  readonly guidanceLevel = signal(0);
  readonly busy = signal(false);
  wasGuessed = false;
  translationReport = '';
}
