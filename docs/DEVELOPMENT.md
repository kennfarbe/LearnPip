# Lokale Entwicklung

LearnPip ist ein Monorepo mit getrennten Backend-, Web-, Deploy- und Testbereichen. Für den Docker-Stack benötigst du Docker mit Compose v2 sowie `openssl`.

## Gesamten Stack starten

Im Repository-Stammverzeichnis:

```sh
./scripts/dev-up.sh
```

Beim ersten Aufruf erzeugt das Skript eine lokale `deploy/.env` mit zufälligem Datenbankpasswort. Die Datei hat restriktive Dateirechte und ist von Git ausgeschlossen. Es werden keine Zugangsdaten oder Beispielpasswörter eingecheckt. Die Container veröffentlichen Web und API nur auf dem lokalen Rechner.

- Web: <http://localhost:4200>
- API-Liveness: <http://localhost:8080/health/live>
- PostgreSQL: nur lokal unter `127.0.0.1:5432` erreichbar; im Container-Netzwerk unter `db:5432`

Container stoppen, Datenbankvolume aber behalten:

```sh
./scripts/dev-down.sh
```

Für einen vollständigen lokalen Reset kann das Datenbankvolume nach dem Stoppen bewusst entfernt werden:

```sh
docker compose --env-file deploy/.env --file deploy/compose.yaml down --volumes
```

Dieser letzte Befehl löscht den lokalen Datenbankinhalt.

## Datenvolume bei PostgreSQL 18

Die lokale Compose-Datei verwendet PostgreSQL 18 und bindet das persistente Volume
unter `/var/lib/postgresql` ein. PostgreSQL 18 verwendet standardmäßig ein
versionsspezifisches Unterverzeichnis (`18/docker`). Vor dem Wechsel eines
bereits bestehenden lokalen Datenvolumes von einem älteren Mount-Pfad bitte
die Datenbank sichern und den tatsächlichen Datenbestand prüfen. **Den
Mount-Pfad nicht blind ändern und keine Volumes löschen.** Eine Anpassung
bereits vorhandener Datenbestände erfordert einen gesondert geprüften
Migrations-/Wiederherstellungsweg; ein reines Umhängen migriert keine Daten.

## Komponenten einzeln bauen

Start the local stack first, then run the migration and ownership integration tests against a temporary database:

```sh
./scripts/test-backend.sh
```

The test creates and drops its own PostgreSQL database; it does not touch the local LearnPip database.

```sh
cd frontend/web
npm ci
npm test --if-present
npm run format:check
npm run lint
npm run test:pwa
npm run build
```

Backend und Web werden von getrennten CI-Jobs gebaut. Der Backend-Job startet eine Wegwerf-PostgreSQL-Instanz für Migrationstests. Pull-Request-Builds benötigen keine Produktionsgeheimnisse. Lokale Formatierung folgt `.editorconfig`; Angular verwendet zusätzlich Prettier.

Die lokale API kann ohne E-Mail- oder OIDC-Anbieter pseudonyme Konten erstellen. Browser-Sitzungscookies verlangen HTTPS; auf `http://localhost` kann zum Test das einmalig ausgegebene Bearer-Token verwendet werden. Die optionalen Identitätsdienste und ihre Konfiguration stehen unter [Konten und Identitätswege](IDENTITY.md).

## Bereiche

- `backend/src/LearnPip.Api/`: ASP.NET Core API
- `backend/src/LearnPip.Worker/`: Hintergrunddienst
- `frontend/web/`: Angular-Weboberfläche
- `deploy/`: lokale Docker-Compose-Umgebung und Beispielkonfiguration
- `docs/architecture/`: Architekturentscheidungen
- `tests/`: Testkonventionen und künftige Testprojekte

Siehe auch [Architekturentscheidungen](architecture/README.md) und den README-Abschnitt [Inhalte und Sichtbarkeit](../README.md#inhalte-und-sichtbarkeit).

## Aktualisierungen durch Dependabot

Dependabot verwendet für alle Paketbereiche Conventional Commits mit dem
Präfix `build` und dem Scope `deps` beziehungsweise `deps-dev`. Die Prüfung
von PR-Titeln und Commit-Nachrichten bleibt für alle Beiträge aktiv.

Zusammengehörige `@angular/*`-Pakete und `SkiaSharp*`-Pakete werden jeweils
in einem gemeinsamen PR aktualisiert. Für Sicherheitsupdates sind eigene
Gruppen eingerichtet. Die Gruppierung ersetzt keine Kompatibilitätsprüfung;
Builds, Tests, Integration und Sicherheitsprüfungen bleiben verpflichtend.

Angular 22.2 benötigt laut Paketauflösung TypeScript `>=6.0 <6.1`.
Dependabot stellt daher Versionsupdates auf TypeScript ab 6.1 vorerst zurück.
Nach einem Angular-Upgrade den unterstützten Bereich erneut prüfen und die
Ausnahme in `.github/dependabot.yml` anpassen oder entfernen.

Web-Abhängigkeiten können die Darstellung verändern. Deshalb gilt auch bei
Dependabot die [Anleitung zur Aktualisierung der Dokumentationsbilder](UI-PREVIEW.md#bilder-aktuell-halten).
Die Screenshot-Prüfung bleibt aktiv. Bei reinen Web-Abhängigkeitsupdates von
Dependabot erzeugt die CI nach Build und UI-Tests automatisch aktuelle Bilder.
Ein separater Workflow ergänzt die fünf PNG-Dateien und das Manifest im PR.
Der erste Lauf bleibt rot, solange die Bilder noch nicht eingecheckt sind.
Der neue Commit löst erneut CI und Sicherheitsprüfungen aus. Sind die Bilder
bereits aktuell, wird kein weiterer Commit erzeugt. Die visuelle Prüfung
bleibt Teil der Maintainer-Abnahme.

Der schreibende Workflow führt ausschließlich Code vom Standardbranch aus.
Er akzeptiert nur offene Dependabot-PRs aus diesem Repository, deren Änderungen
auf die beiden Web-Paketdateien und die sechs erzeugten Bilddateien beschränkt
sind. Die Artefakte stammen aus dem zugehörigen CI-Lauf; Dateiliste, Größe,
PNG-Kopf, Abmessungen und Manifest-Fingerabdrücke werden geprüft. Vor dem
Schreiben wird der aktuelle PR-Commit erneut geprüft; es gibt keinen Force-Push.
Build- oder Testfehler erzeugen keine neuen Bilder. Gemischte Änderungen und
Beiträge anderer Autoren benötigen weiterhin die manuelle Aktualisierung.

Für den Screenshot-Commit wird die vorhandene Release-App mit
`RELEASE_APP_CLIENT_ID` und `RELEASE_APP_PRIVATE_KEY` verwendet. Sie benötigt
Schreibzugriff auf Repository-Inhalte. Lesende Abfragen verwenden den
separaten, nur lesenden Workflow-Token. Der App-Token wird nicht an den
Dependabot-Build übergeben. Die Automatik ist erst nach dem Merge aktiv.
`--force` oder `--legacy-peer-deps` sind keine Lösung für Paketkonflikte.

Die neue Konfiguration wirkt nach dem Merge auf den Standardbranch.
Bereits offene Einzel-PRs nicht unverändert übernehmen: Dependabot neu
prüfen lassen und die künftig erzeugten Gruppen verwenden. Bei alten
NuGet-PRs müssen auch die Commit-Nachrichten dem neuen Präfix entsprechen;
eine bloße Änderung des PR-Titels genügt nicht. Alte PRs erst schließen,
wenn ein Ersatz-PR das betreffende Update abdeckt. Ein nicht kompatibles
TypeScript-Update bleibt zurückgestellt.
