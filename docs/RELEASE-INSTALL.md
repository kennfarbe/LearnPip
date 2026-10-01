# LearnPip aus Docker Hub installieren und aktualisieren

Stabile LearnPip-Releases werden nach erfolgreichem semantic-release automatisch als Multi-Arch-Images
für `linux/amd64` und `linux/arm64` nach Docker Hub veröffentlicht:

- `kennfarbe/learnpip:api-vX.Y.Z`
- `kennfarbe/learnpip:worker-vX.Y.Z`
- `kennfarbe/learnpip:web-vX.Y.Z`

Zusätzlich existieren `api-latest`, `worker-latest` und `web-latest`. Produktive Installationen
verwenden absichtlich immer einen festen `vX.Y.Z`-Tag. PostgreSQL und Caddy verwenden weiterhin
ihre offiziellen Images. Auf dem Zielsystem werden weder Git noch Node.js noch das .NET SDK benötigt.

## Komfort-Installer

Für Erstinstallation und spätere Updates steht `scripts/install-release.sh` bereit. Das Skript
lädt nur die kleine Release-Konfiguration aus GitHub; die eigentliche Anwendung kommt als fertige
versionierte Images aus Docker Hub. Es klont kein Repository und kompiliert LearnPip nicht auf der VM.

Erstinstallation mit dem neuesten stabilen Release:

```sh
bash scripts/install-release.sh install --domain learn.meine-domain.de
```

Zum Prüfen und Konfigurieren vor dem ersten Start kann zunächst `prepare` verwendet werden.
Ein reguläres Update erfolgt anschließend mit:

```sh
bash "$HOME/learnpip/current/scripts/install-release.sh" update
```

Bei Rootless Docker läuft dies als normaler Benutzer ohne sudo. Das Skript prüft Stable-Release-
Metadaten, erhält Secrets und Konfiguration, erstellt vor Updates ein Datenbank-Backup, setzt
`LEARNPIP_VERSION` auf den neuen Release-Tag, führt `docker compose pull`, Migration und
Healthcheck aus und schaltet erst danach den `current`-Link um. Ein Datenbank-Rollback erfolgt
nicht automatisch.


### Interne LAN-/VPN-Installation

Für Installationen, die nur im lokalen Netz oder später über VPN erreichbar sein sollen, kann der Installer Caddys interne CA verwenden. Es sind dann keine öffentlichen DNS-Einträge und keine Portweiterleitungen aus dem Internet erforderlich:

```sh
bash scripts/install-release.sh install --domain learnpip.internal.example --internal
```

Der Modus wird als `LEARNPIP_INTERNAL=true` in der gemeinsamen Konfiguration gespeichert und bei späteren Updates automatisch wiederverwendet. Caddy stellt für den internen Hostnamen ein Zertifikat über seine lokale CA aus. Clients vertrauen dieser CA zunächst nicht automatisch; importiere das Caddy-Root-Zertifikat nur auf Geräten, die LearnPip verwenden sollen. Der interne DNS muss `learnpip.internal.example` auf die LAN-Adresse der LearnPip-VM auflösen. Keine Ports 80/443 am Internet-Router freigeben.

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
