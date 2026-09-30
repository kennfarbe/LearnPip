# LearnPip aus einem GitHub Release installieren und aktualisieren

Ein Release enthält eine Installationsdatei `learnpip-install-vX.Y.Z.tar.gz`, eine zugehörige
SHA-256-Datei, die Versionshinweise und die drei gleich versionierten Container-Images
`ghcr.io/kennfarbe/learnpip-{api,worker,web}:X.Y.Z`. API und Migrationslauf verwenden dasselbe
Image. PostgreSQL und Caddy werden von Docker Compose als externe Images bezogen. Docker Engine
mit Compose-Plugin, Internetzugang, eine Domain und eine VM mit dauerhaftem Speicher werden benötigt.
Die Images müssen in GitHub Packages öffentlich lesbar sein; andernfalls ist vor `pull` eine
GHCR-Anmeldung erforderlich. Informationen zu Proxmox und DNS: [PROXMOX.md](PROXMOX.md).

## Erstinstallation

Als Administrator in der Docker-VM das gewünschte **veröffentlichte** Release auf GitHub
herunterladen, beide Anlagen in dasselbe Verzeichnis legen und die veröffentlichte Prüfsumme
prüfen. Für `X.Y.Z` die tatsächliche Versionsnummer einsetzen:

```sh
mkdir -p /opt/learnpip && cd /opt/learnpip
curl -fL -O https://github.com/kennfarbe/LearnPip/releases/download/vX.Y.Z/learnpip-install-vX.Y.Z.tar.gz
curl -fL -O https://github.com/kennfarbe/LearnPip/releases/download/vX.Y.Z/learnpip-install-vX.Y.Z.tar.gz.sha256
sha256sum -c learnpip-install-vX.Y.Z.tar.gz.sha256
tar -xzf learnpip-install-vX.Y.Z.tar.gz
./scripts/prod-init.sh
```

`deploy/.env.production` editieren: `LEARNPIP_DOMAIN` auf die eigene Domain und
`LEARNPIP_VERSION=X.Y.Z` setzen. Die von `prod-init.sh` erzeugten Geheimnisse bleiben auf
dieser VM und dürfen bei Updates nicht neu erzeugt oder überschrieben werden. Optional SMTP,
OIDC und KI nach [PROXMOX.md](PROXMOX.md) einrichten. Danach:

```sh
cd /opt/learnpip
docker compose --env-file deploy/.env.production -f deploy/compose.release.yaml config --quiet
docker compose --env-file deploy/.env.production -f deploy/compose.release.yaml pull
docker compose --env-file deploy/.env.production -f deploy/compose.release.yaml up -d db
docker compose --env-file deploy/.env.production -f deploy/compose.release.yaml --profile ops run --rm migrate
docker compose --env-file deploy/.env.production -f deploy/compose.release.yaml up -d
docker compose --env-file deploy/.env.production -f deploy/compose.release.yaml ps
```

Erst nach erfolgreicher Migration wird die API gestartet. `https://<domain>/health/ready`
prüfen. Sicherungen von PostgreSQL, `deploy/secrets/`, `deploy/.env.production` und den Docker
Volumes für die Proxy-Zertifikate außerhalb der VM aufbewahren.

## Update und Rückfall

Vor dem Update Datenbank und VM sichern und die Release Notes auf Migrationshinweise prüfen.
`LEARNPIP_COMPOSE_FILE=compose.release.yaml ./scripts/backup-prod.sh` erzeugt einen Dump.
Die neue Installationsdatei samt Prüfsumme wie oben herunterladen und **im bestehenden**
`/opt/learnpip` entpacken. Danach ausschließlich `LEARNPIP_VERSION` in
`deploy/.env.production` auf die neue Nummer ändern und die fünf Compose-Befehle oben ab
`config --quiet` ausführen. Die lokale `.env.production` und `deploy/secrets/` sind nicht im
Release-Paket enthalten und bleiben erhalten.

Für einen Rückfall die zuvor verwendete Versionsnummer in `.env.production` eintragen und
Images erneut starten. Eine Datenbankmigration kann nicht gefahrlos durch ein älteres Image
rückgängig gemacht werden; bei inkompatibler Schemaänderung zuerst die zur alten Version
gehörende Datenbanksicherung zurückspielen. Postgres-Hauptversionswechsel separat planen.
