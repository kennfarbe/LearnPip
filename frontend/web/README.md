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
```

Die CI verwendet das eingecheckte Lockfile, prüft Prettier, Angular ESLint mit Accessibility-Regeln, den PWA-Test und den Produktionsbuild. ESLint-Warnungen und erkannte Angular-Build-Warnungen führen zum Fehlschlag. Einzelheiten zu Offline-Verhalten und Installation stehen unter [PWA](../../docs/pwa.md).
