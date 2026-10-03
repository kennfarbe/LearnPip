# LearnPip-Weboberfläche

Das Angular-22-Frontend stellt die responsive Weboberfläche einschließlich Anmeldung, privater Fragen, Lernsitzungen, Fortschritt, Verwaltung und optionaler KI-Funktionen bereit. Es unterstützt Hell/Dunkel/System und eine Progressive Web App (PWA). Die webbasierten Admin-Updates aus PR #91 benötigen zusätzlich den separat eingerichteten Host-Operator.

## Lokal entwickeln

```sh
npm ci
npm start
```

Die Entwicklungsoberfläche ist unter <http://localhost:4200> erreichbar. Den vollständigen Stack einschließlich API und PostgreSQL startest du im Repository-Stammverzeichnis mit `./scripts/dev-up.sh`. Siehe [Entwicklungsanleitung](../../docs/DEVELOPMENT.md).

## Prüfen und bauen

```sh
npm ci
npm run format:check
npm run lint
npm run test:pwa
npm run build
npx --no-install playwright install --with-deps chromium
npm run test:ui
```

Die CI verwendet das eingecheckte Lockfile, prüft Prettier, Angular ESLint mit Accessibility-Regeln, den PWA-Test und den Produktionsbuild. ESLint-Warnungen und erkannte Angular-Build-Warnungen führen zum Fehlschlag. Einzelheiten zu Offline-Verhalten und Installation stehen unter [PWA](../../docs/pwa.md).

## Arbeitsbereiche

Die sechs Hauptbereiche besitzen eigene Routen. Verwaltungsrechte werden beim
Einstieg erneut über die API geprüft. Suche, Editor, Lerneinheit und persönliche
Einstellungen sind voneinander getrennt. Bedienung, Berechtigungen und die
noch offene manuelle Screenreader-Abnahme stehen unter
[Arbeitsbereiche und Navigation](../../docs/UI-WORKSPACES.md).

Die UI-Tests verwenden Chromium, den Produktionsbuild und isolierte API-Fixtures.
Sie laufen auch in der CI. Für ein bereits installiertes Chromium kann lokal
`PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH` auf dessen Programmdatei gesetzt werden.

## Beispielbilder pflegen

Die [Bildvorschau](../../docs/UI-PREVIEW.md) zeigt die aktuelle Oberfläche.
Bei Designänderungen `npm run docs:screenshots` ausführen, Bilder visuell prüfen
und zusammen mit dem Manifest einchecken. Die CI prüft mit
`npm run docs:screenshots:check`, ob die Aufnahmen zu den UI-Quellen passen.
Jedes Bild braucht einen beschreibenden Alternativtext, der bei Inhaltsänderungen
mit aktualisiert wird.
