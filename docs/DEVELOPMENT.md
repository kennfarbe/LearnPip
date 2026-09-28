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
- PostgreSQL: nur im Compose-Netzwerk erreichbar

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

```sh
dotnet test backend/LearnPip.sln --configuration Release
```

```sh
cd frontend/web
npm ci
npm test --if-present
npm run build
```

Backend und Web werden von getrennten CI-Jobs gebaut. Pull-Request-Builds benötigen keine Produktionsgeheimnisse. Lokale Formatierung folgt `.editorconfig`; Angular verwendet zusätzlich Prettier.

## Bereiche

- `backend/src/LearnPip.Api/`: ASP.NET Core API
- `backend/src/LearnPip.Worker/`: Hintergrunddienst
- `frontend/web/`: Angular-Weboberfläche
- `deploy/`: lokale Docker-Compose-Umgebung und Beispielkonfiguration
- `docs/architecture/`: Architekturentscheidungen
- `tests/`: Testkonventionen und künftige Testprojekte

Siehe auch [Architekturentscheidungen](architecture/README.md) und den README-Abschnitt [Inhalte und Sichtbarkeit](../README.md#inhalte-und-sichtbarkeit).
