# LearnPip Web

Angular-Webclient für LearnPip. Das Frontend ist ein technisches Grundgerüst; Lern- und Kontofunktionen folgen in späteren Issues.

## Lokal entwickeln

```sh
npm ci
npm start
```

Die lokale Entwicklungsoberfläche ist unter <http://localhost:4200> erreichbar. Der vollständige Stack einschließlich API und PostgreSQL startet aus dem Repository-Stamm mit `./scripts/dev-up.sh`; Details stehen in [docs/DEVELOPMENT.md](../../docs/DEVELOPMENT.md).

## Prüfen und bauen

```sh
npm run format:check
npm run build
```

Die CI installiert Abhängigkeiten über `npm ci`, prüft Prettier-Formatierung und baut das Produktionsbundle. Das npm-Lockfile ist eingecheckt, damit lokale und CI-Installationen dieselben aufgelösten Versionen verwenden.
