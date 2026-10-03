# Arbeitsbereiche und Navigation

Die Oberfläche trennt tägliche Lernaufgaben, Inhalte, Administration und
persönliche Einstellungen. Der aktuelle Bereich steht im Seitentitel und ist
in der Navigation mit Textunterstreichung und `aria-current="page"` markiert.

## Bereiche

| Route | Aufgabe |
| --- | --- |
| `/overview` | Nächste Aktionen und kompakter Lernstand |
| `/questions` | Fragen suchen, filtern, erstellen und bearbeiten |
| `/learn` | Kurze Lerneinheiten und optionale Prüfungsvorbereitung |
| `/catalogs` | Private Kataloge, Inhalte, Medien und Lerngruppen |
| `/administration` | Moderation, Updates und offizielle Katalogimporte |
| `/settings` | Sprache, Design, Konto und Familienverknüpfungen |

Die Startadresse und unbekannte Routen führen zur Übersicht. Direkte Links auf
Arbeitsbereiche funktionieren auch nach dem Neuladen. Auf Smartphones öffnet
„Menü öffnen“ dieselben Navigationspunkte in derselben Reihenfolge.

## Fragen und Lernen

Der Fragenbereich zeigt zunächst Suche und Katalog- bzw. Fassungsfilter.
„Neue Frage erstellen“ oder die Auswahl eines Entwurfs öffnet den Editor.
Bildangaben, Katalogzuordnung und Veröffentlichungsdetails sind aufklappbar.
Herkunft und Lizenz bleiben vor dem Veröffentlichen erforderlich. Ein
Bereichswechsel mit ungespeicherten Änderungen verlangt eine Entscheidung;
während des Speicherns wird der Wechsel blockiert.

Eine laufende Lerneinheit zeigt Frage, Antworten, Fortschritt und nächste Aktion.
Werkzeuge zur Inhaltsorganisation stehen im Katalogbereich. Prüfungsvorbereitung
und Simulationen sind vor einer Lerneinheit aufklappbar. Auswahl und Rückmeldung
bleiben bei einem Bereichswechsel erhalten. Beim erneuten Einstieg prüft die API
den Zugriff auf die Sitzung; ein widerrufener Zugriff entfernt sie aus der Ansicht.
Das Neuladen setzt die Sitzung anhand der vorhandenen Sitzungskennung fort.

## Berechtigungen

`GET /api/v1/auth/capabilities` prüft die bestehenden API-Richtlinien für
Administration und Moderation. Die Administrationsrichtlinie verlangt weiterhin
eine frische Sitzung. Die Navigation zeigt Verwaltung nur mit mindestens einer
dieser Berechtigungen. Beim Einstieg prüft der Routenschutz erneut; fehlender
Zugriff oder eine fehlgeschlagene Prüfung führt zur Übersicht mit einem Hinweis.

Moderatorinnen und Moderatoren sehen die Moderation. Updates und offizielle
Importe erscheinen nur mit aktueller Administrationsberechtigung. Die einzelnen
API-Endpunkte behalten ihre eigene Autorisierung. Persönliche Einstellungen sind
vom Verwaltungsbereich getrennt; Lern- und Fragenansichten enthalten keine
Import- oder Updateformulare.

## Prüfung

```sh
cd frontend/web
npm ci
npm run format:check
npm run lint
npm run test:pwa
npm run build
npx --no-install playwright install --with-deps chromium
npm run test:ui
```

Die Browserregressionen verwenden den Produktionsbuild und isolierte API-Fixtures.
Sie prüfen Suche und Filter, Bereichswechsel, ungespeicherte Änderungen,
Sitzungsfortsetzung und Widerruf, Rollenänderungen, Katalogverweise, Tastatur und
mobiles Menü bei 320 Pixeln sowie Kontraste und zugängliche Namen in Hell/Dunkel.
Ein eigener Test aktiviert den echten Service Worker, öffnet eine neue Route
offline und prüft, dass keine API-Antworten im Cache liegen.
Die Backendtests prüfen die Berechtigungsantwort mit PostgreSQL, echten Sitzungen,
Administrator- und Moderationsrollen, Sitzungsalter und Rollenentzug.

### Manuelle Abnahme mit Screenreader

Automatisierte Accessibility-Checks und Browser-Accessibility-Trees ersetzen
keinen Test mit einem tatsächlichen Screenreader. Diese Abnahme ist noch offen:

1. Mit NVDA/Firefox oder VoiceOver/Safari den Sprung zum Hauptinhalt verwenden.
2. Hauptnavigation, aktiven Bereich, Seitentitel und Überschriften prüfen.
3. Mit Tab, Umschalt+Tab, Enter, Leertaste und Escape das mobile Menü und die
   aufklappbaren Abschnitte bedienen. Der Fokus darf nicht verloren gehen.
4. Eine Frage erstellen, den Editor verlassen und die Rückfrage verwerfen.
   Danach speichern und in den Lernbereich wechseln.
5. Eine Antwort prüfen, Rückmeldung und Fortschritt anhören, zur Übersicht und
   zurück wechseln und die Sitzung abschließen.
6. Als normales Konto, Moderation und Administration die sichtbaren Bereiche
   prüfen. Ohne Berechtigung auch die direkte Verwaltungsadresse aufrufen.
7. Bei 200 % Zoom sowie bei schmalem Display und Hell/Dunkel auf abgeschnittene
   Inhalte und die Erreichbarkeit aller Aktionen achten.

Grundlagen sind [WCAG 2.2](https://www.w3.org/TR/WCAG22/) sowie die WAI-Anleitungen
für [Seitenstruktur](https://www.w3.org/WAI/tutorials/page-structure/) und
[Formulare](https://www.w3.org/WAI/tutorials/forms/).
