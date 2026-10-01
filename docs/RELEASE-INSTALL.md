# LearnPip aus Docker Hub installieren und aktualisieren

Stabile LearnPip-Releases werden nach erfolgreichem semantic-release automatisch als Multi-Arch-Images
für `linux/amd64` und `linux/arm64` nach Docker Hub veröffentlicht:

- `kennfarbe/learnpip:api-vX.Y.Z`
- `kennfarbe/learnpip:worker-vX.Y.Z`
- `kennfarbe/learnpip:web-vX.Y.Z`

Zusätzlich existieren `api-latest`, `worker-latest` und `web-latest`. Produktive Installationen
verwenden absichtlich immer einen festen `vX.Y.Z`-Tag. PostgreSQL und Caddy verwenden weiterhin
ihre offiziellen Images. Auf dem Zielsystem werden weder Git noch Node.js noch das .NET SDK benötigt.

## Erstinstallation

Die Proxmox-Anleitung beschreibt VM, Docker, Netzwerk und Firewall. Danach reicht das kleine
Deployment-Paket aus dem GitHub-Release; der Anwendungsquellcode muss nicht gebaut werden.

Die Produktionsdateien `deploy/compose.prod.yaml`, `deploy/Caddyfile`,
`deploy/.env.production.example` sowie die benötigten Betriebsskripte werden aus dem passenden
GitHub-Release verwendet. `LEARNPIP_VERSION` muss auf dessen Tag gesetzt sein, zum Beispiel:

```sh
LEARNPIP_VERSION=v1.2.3
```

Konfiguration und Secrets bleiben dauerhaft auf dem Server. `prod-init.sh` wird nur bei der
Erstinstallation ausgeführt.

Vor dem ersten Start:

```sh
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml config --quiet
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml pull
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml up -d db
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml --profile ops run --rm migrate
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml up -d
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml ps
```

Die Migration muss erfolgreich enden, bevor die neue API gestartet wird.

## Aktualisieren

1. Release Notes lesen und einen aktuellen Datenbank-/VM-Backupstand herstellen.
2. Die zum neuen Release gehörenden Deployment-Dateien übernehmen.
3. `LEARNPIP_VERSION` auf den neuen stabilen Tag ändern.
4. Neue Images ziehen.
5. Migration mit genau diesem API-Image ausführen.
6. Dienste neu erstellen und Healthcheck prüfen.

```sh
./scripts/backup-prod.sh
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml pull
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml --profile ops run --rm migrate
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml up -d
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml ps
```

Es gibt bewusst kein unbeaufsichtigtes Auto-Update: Datenbankmigrationen und Release Notes sollen
vor einem Versionswechsel geprüft werden. Ein Rollback der Container ist durch Zurücksetzen von
`LEARNPIP_VERSION` möglich, sofern das Datenbankschema abwärtskompatibel ist. Bei inkompatiblen
Migrationen muss zusätzlich die vor dem Upgrade erstellte Datenbanksicherung wiederhergestellt werden.

## Wechsel von einer bisherigen Source-/Git-Installation

Vor dem Wechsel Datenbank und VM sichern. Vorhandene `.env.production` und `deploy/secrets/`
unverändert übernehmen; insbesondere keine neuen Datenbank- oder Verschlüsselungsschlüssel erzeugen.
Danach auf die Image-basierte Compose-Datei wechseln, `LEARNPIP_VERSION` setzen, `docker compose pull`
ausführen, migrieren und erst anschließend API/Worker/Web neu starten. Die benannten PostgreSQL- und
Caddy-Volumes bleiben durch den festen Compose-Projektnamen `learnpip` erhalten.
