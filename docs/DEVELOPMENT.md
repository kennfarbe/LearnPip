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
