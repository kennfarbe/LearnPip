# Ein Blick auf LearnPip

Diese Bilder zeigen die echte Weboberfläche mit fiktiven Beispieldaten.
Sie enthalten keine Daten echter Benutzer. Die verfügbare Navigation hängt von
den Berechtigungen des Kontos ab.

## Übersicht

Die Startseite bietet direkte Wege zum Lernen, zu eigenen Fragen und Katalogen.
Darunter wird der persönliche Lernfortschritt zusammengefasst.

![LearnPip im hellen Design: Navigation, drei Einstiegskarten zum Lernen, Fragen und Katalogen sowie eine Zusammenfassung des Lernfortschritts.](screenshots/overview.png)

## Eigene Fragen bearbeiten

Fragen lassen sich suchen und nach Katalog oder Fassungsstatus filtern.
Der Editor zeigt die ausgewählte Frage und ihre Antwortmöglichkeiten.
Darunter liegen die aufklappbaren Bereiche für lokale Exportauswahl und Import.
Der Export bietet kombinierte Filter, eine Lizenzvorschau und einen bestätigten
ZIP-Download ohne öffentliche Veröffentlichung.

![Frageneditor mit der Mathematikfrage „Was ergibt 2 + 2?“, den Antworten Vier und Fünf sowie Such- und Filterfeldern für eigene Fragen und aufklappbaren Bereichen für Export und Import.](screenshots/question-editor.png)

## Kataloge und Inhalte

Private Kataloge bündeln eigene Fragen. Von hier aus gelangt man direkt zur
gefilterten Fragenliste. Lokale ZIP-Fragenpakete lassen sich nach einer Vorschau
und ausdrücklicher Bestätigung in einen neuen privaten Katalog importieren.

![Katalogbereich im hellen Design mit Dateiauswahl und Vorschau-Schaltfläche für den privaten ZIP-Import sowie dem Katalog Mathematik, seiner Fragenanzahl und dem Link zur Fragenliste.](screenshots/catalogs.png)

## Lernen auf dem Smartphone

Die mobile Ansicht zeigt eine Frage nach der anderen. Das dunkle Design kann
in den Einstellungen gewählt werden.

![Mobile LearnPip-Lerneinheit im dunklen Design: Mathematikfrage „Was ergibt 2 + 2?“, ausgewählte Antwort Vier, Fortschrittsanzeige und Schaltfläche zum Prüfen.](screenshots/learning-mobile-dark.png)

## Bilder aktuell halten

Die Bilder werden aus dem Produktionsbuild mit isolierten Beispieldaten erzeugt.
Nach Änderungen an Oberfläche, Texten, Assets oder Build-Abhängigkeiten:

```sh
cd frontend/web
npm ci
npx --no-install playwright install --with-deps chromium
npm run docs:screenshots
npm run docs:screenshots:check
```

Alle fünf Bilder und `docs/screenshots/manifest.json` zusammen einchecken und
visuell prüfen. Bei geänderten Bildinhalten auch die Alternativtexte aktualisieren.
Die CI prüft die Fingerabdrücke der UI-Quellen und Bilddateien und schlägt bei
veralteten Aufnahmen fehl. Sie ersetzt keine visuelle Prüfung der Bilder.

## Lokaler Administratorzugang

![Einstellungen mit beschrifteter Administrator-Anmeldung und aufgeklappter Passwortänderung; Anforderungen und Hinweis auf Sitzungswiderruf stehen vor der Eingabe.](screenshots/admin-password.png)
