import { Injectable, signal } from '@angular/core';
import { uiEnglish } from './ui-english';

export type UiLanguage = 'de' | 'en';

const english: Record<string, string> = {
  Übersicht: 'Overview',
  Fragen: 'Questions',
  Lernen: 'Practice',
  'Kataloge und Inhalte': 'Catalogs and content',
  Verwaltung: 'Administration',
  Einstellungen: 'Settings',
  'Zum Hauptinhalt': 'Skip to main content',
  'LearnPip Übersicht': 'LearnPip overview',
  Hauptnavigation: 'Main navigation',
  'Menü öffnen': 'Open menu',
  'Menü schließen': 'Close menu',
  'Dein Lernstand und dein nächster Schritt.': 'Your progress and your next step.',
  'Eigene Fragen finden, erstellen und bearbeiten.': 'Find, create and edit your questions.',
  'Eine kurze Lerneinheit nach der anderen.': 'One short learning session at a time.',
  'Deinen Lernstoff und deine Gruppen organisieren.': 'Organize your content and study groups.',
  'Einreichungen und die Anwendung verwalten.': 'Manage submissions and the application.',
  'Sprache, Design und dein persönliches Konto.': 'Language, appearance and your personal account.',
  'Dieser Bereich ist für dein Konto nicht verfügbar.':
    'This area is not available for your account.',
  'Dein nächster Schritt': 'Your next step',
  'Jetzt lernen': 'Practice now',
  'Neue Frage erstellen': 'Create a question',
  'Lernstoff organisieren': 'Organize learning content',
  'Kataloge und Lerninhalte an einem Ort.': 'Catalogs and learning content in one place.',
  'Kataloge öffnen': 'Open catalogs',
  'Frage aus einem Foto erstellen': 'Create a question from a photo',
  'KI-Unterstützung für Fragen': 'AI support for questions',
  'Übersetzungen bearbeiten': 'Edit translations',
  'Eine Prüfung vorbereiten': 'Prepare for an exam',
  'Prüfungssimulation öffnen': 'Open exam simulation',
  'Lerninhalte und Varianten organisieren': 'Organize learning content and variants',
  'Private Medien': 'Private media',
  Lerngruppen: 'Study groups',
  'Rückmeldungen zu öffentlichen Fragen': 'Feedback on public questions',
  'Offizielle Kataloge und Prüfungsprofile': 'Official catalogs and exam profiles',
  'Berechtigungen aktualisieren': 'Refresh permissions',
  'Sprache und Design': 'Language and appearance',
  Sprache: 'Language',
  Design: 'Appearance',
  Hell: 'Light',
  Dunkel: 'Dark',
  Familienverknüpfungen: 'Family links',
  'Deine Fragen bleiben beim Löschen eines Katalogs erhalten.':
    'Deleting a catalog keeps your questions.',
  'Katalog anlegen': 'Create catalog',
  'Fragen anzeigen': 'Show questions',
  'Katalog bearbeiten': 'Edit catalog',
  'Noch keine privaten Kataloge vorhanden.': 'No private catalogs yet.',
  'Katalog löschen? Die Fragen bleiben erhalten.': 'Delete catalog? Your questions will be kept.',
  'Katalog gespeichert. Die Fragen bleiben erhalten.':
    'Catalog saved. Your questions have been kept.',
  'Katalog konnte nicht geändert werden.': 'The catalog could not be changed.',
  'Fragen suchen': 'Search questions',
  Fassungsstatus: 'Version status',
  'Alle Fragen': 'All questions',
  'Nur Entwürfe': 'Drafts only',
  'Mit veröffentlichter Fassung': 'With a published version',
  'Kataloge verwalten': 'Manage catalogs',
  'Fragen gefunden': 'questions found',
  'Editor schließen': 'Close editor',
  'Sprache und Katalogzuordnung': 'Language and catalog assignment',
  'Bild zur Frage hinzufügen': 'Add an image to the question',
  'Bild zur Antwort hinzufügen': 'Add an image to the answer',
  'Erklärung, Herkunft und Lizenz': 'Explanation, source and license',
  'Herkunft und Lizenz sind vor dem Veröffentlichen erforderlich.':
    'Source and license are required before publishing.',
  'Wähle eine Frage oder erstelle eine neue.': 'Select a question or create a new one.',
  'Deine Entwürfe bleiben privat.': 'Your drafts stay private.',
  'Bitte warte, bis der aktuelle Vorgang abgeschlossen ist.':
    'Please wait until the current action is complete.',
  'Ungespeicherte Änderungen verwerfen?': 'Discard unsaved changes?',
  'Fortschritt im Detail': 'Progress in detail',
  'Fortschritt der Lerneinheit': 'Session progress',
  'Sitzung abschließen': 'Finish session',
  'Nächste Frage': 'Next question',
  'Sitzung abgeschlossen': 'Session complete',
  'Lerninhalte und Varianten': 'Learning content and variants',
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
