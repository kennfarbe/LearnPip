# Mobile Nutzung, Design und PWA

LearnPip passt Kopfbereich, Karten, Formulare und Tabellen an schmale Displays an.
Im Kopfbereich lässt sich das Design auf **Hell**, **Dunkel** oder **System** stellen.
Standard ist System. Die Auswahl wird nur lokal im Browser gespeichert. System folgt
auch einer während der Nutzung geänderten Betriebssystemeinstellung. Ohne Zugriff
auf den Browserspeicher funktioniert die Auswahl für den aktuellen Besuch.

## Installation

Die Produktionsversion muss über HTTPS erreichbar sein (localhost ist zum Testen
zulässig). Manifest, Icons und Service Worker werden mit dem Frontend ausgeliefert.
In Chrome/Edge kann LearnPip über das Installationssymbol oder das Browsermenü
installiert werden. Auf iPhone/iPad in Safari **Teilen → Zum Home-Bildschirm** wählen.
Die Verfügbarkeit hängt vom Browser ab; die Webseite bleibt ohne Installation nutzbar.

## Offline und Updates

Nach dem ersten erfolgreichen Online-Aufruf speichert der Service Worker nur die
öffentliche Anwendungshülle und statische Skripte, Styles, Fonts und App-Icons.
API-Antworten und private Medien werden nicht im Service-Worker-Cache gespeichert.
Offline kann die Oberfläche geöffnet werden; Lernen, Speichern, Anmeldung und andere
Serverfunktionen benötigen weiterhin eine Verbindung. Es gibt keine Offline-Warteschlange.

Der Worker wird nur im Produktionsbuild registriert. Die Version des gehashten
Einstiegsskripts bestimmt die Cache-Version. Ein neuer Worker wartet, bis alle alten
LearnPip-Tabs geschlossen wurden, bevor er aktiviert wird und alte Shell-Caches entfernt.
Dadurch werden laufende Eingaben bei Updates nicht durch ein automatisches Neuladen
unterbrochen. Zum Anwenden eines Updates alle LearnPip-Fenster schließen und erneut öffnen.

## Prüfung

`cd frontend/web && npm ci && npm run format:check && npm run test:pwa && npm run build`

Den Produktionsordner `dist/learnpip-web/browser` über localhost bereitstellen.
Bei 320, 390, 768 und 1440 Pixel Breite auf horizontales Überlaufen prüfen.
Alle drei Designs, Neuladen und einen Wechsel der Systemfarben prüfen. Nach der
Service-Worker-Aktivierung offline neu laden; in Cache Storage dürfen keine API-
oder privaten Medienantworten liegen. Die Entwicklungsversion registriert keinen Worker.
