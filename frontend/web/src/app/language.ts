import { Injectable, signal } from '@angular/core';
import { uiEnglish } from './ui-english';

export type UiLanguage = 'de' | 'en';

const english: Record<string, string> = {
  'Technischer Aufbau': 'Under construction',
  'Jeden Tag ein bisschen schlauer.': 'Learn a little every day.',
  'LearnPip entsteht als Ort für kurze, persönliche Lerneinheiten. Das Grundgerüst ist bereit; Erstelle eigene Fragen und übe sie in kurzen Sitzungen.':
    'LearnPip is a place for short, personal lessons. Create your own questions and practice in short sessions.',
  'Die Anwendung befindet sich im Aufbau.': 'The application is under construction.',
  'Fragen sammeln': 'Collect questions',
  'Eigene Fragen, Antworten und Erklärungen an einem privaten Ort.':
    'Keep your questions, answers and explanations in one private place.',
  'In kleinen Schritten üben': 'Practice in small steps',
  'Kurze Lerneinheiten, die in den Alltag passen.': 'Short lessons that fit your day.',
  'Fortschritt erkennen': 'Track progress',
  'Sehen, was schon sitzt und was noch etwas Übung braucht.':
    'See what you know and what needs more practice.',
  'Ein Open-Source-Projekt in der Aufbauphase': 'An open source project in development',
  'Private Inhalte bleiben privat.': 'Private content stays private.',
  'Kurz lernen': 'Quick practice',
  'Sitzung starten': 'Start session',
  'Neue Sitzung': 'New session',
  Prüfen: 'Check answer',
  Weiter: 'Continue',
  'Hinweis anzeigen': 'Show hint',
  'Nächsten Schritt anzeigen': 'Show next step',
  'Ausführliche Erklärung': 'Full explanation',
  'Erklärung einklappen': 'Hide explanation',
  'Ohne Wertung überspringen': 'Skip without grading',
  'Richtig beantwortet': 'Correct answer',
  'Noch nicht richtig': 'Not quite right',
  'Übersetzung fehlt': 'Translation missing',
  'Frage und Antworten': 'Question and answers',
  Lösung: 'Explanation',
  Bildbeschreibung: 'Image description',
  'Entwurf speichern': 'Save draft',
  'Übersetzung freigeben': 'Approve translation',
  'Übersetzungsfehler melden': 'Report a translation error',
};

@Injectable({ providedIn: 'root' })
export class LanguageService {
  readonly current = signal<UiLanguage>(
    localStorage.getItem('learnpip-ui-language') === 'en' ? 'en' : 'de',
  );

  set(value: UiLanguage): void {
    this.current.set(value);
    localStorage.setItem('learnpip-ui-language', value);
    document.documentElement.lang = value;
    document.title =
      value === 'de'
        ? 'LearnPip – Jeden Tag ein bisschen schlauer'
        : 'LearnPip – Learn a little every day';
  }

  t(de: string): string {
    return this.current() === 'en' ? (english[de] ?? uiEnglish[de] ?? de) : de;
  }
}
