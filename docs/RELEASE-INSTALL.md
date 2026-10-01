# LearnPip aus einem GitHub Release installieren und aktualisieren

Jedes GitHub Release bietet automatisch ein vollständiges Quellcodearchiv (`tar.gz` oder `zip`)
für den Versionstag. Es enthält Dockerfiles, produktive Compose-Datei, Initialisierungs- und
Backup-Skripte und die Dokumentation. Docker baut die API, Worker und Web-App aus **diesem Tag**
auf der eigenen VM; es müssen keine großen versionierten Container-Images in GitHub Packages
aufbewahrt werden. Voraussetzung: Docker Engine mit Compose-Plugin, Internetzugang für Basisimages
und Paketabhängigkeiten, genügend Speicher, Domain und DNS. Die [Proxmox-Anleitung](PROXMOX.md)
beschreibt die VM, Docker, Firewall, Mail und OIDC.

## Erstinstallation

In den folgenden Befehlen `X.Y.Z` durch die gewünschte veröffentlichte Version ersetzen.
Die Quellen werden pro Version in einem eigenen Verzeichnis entpackt. Geheimnisse liegen
außerhalb dieser Verzeichnisse und dürfen bei Updates nicht ersetzt werden.

```sh
sudo -i
mkdir -p /opt/learnpip/shared/secrets /opt/learnpip/releases/vX.Y.Z
curl -fL https://github.com/kennfarbe/LearnPip/archive/refs/tags/vX.Y.Z.tar.gz \
  -o /tmp/learnpip-vX.Y.Z.tar.gz
tar -xzf /tmp/learnpip-vX.Y.Z.tar.gz --strip-components=1 \
  -C /opt/learnpip/releases/vX.Y.Z
cd /opt/learnpip/releases/vX.Y.Z
ln -s /opt/learnpip/shared/secrets deploy/secrets
ln -s /opt/learnpip/shared/.env.production deploy/.env.production
./scripts/prod-init.sh
nano deploy/.env.production
```

`LEARNPIP_DOMAIN` auf die eigene Domain setzen; optionale Integrationen nach [PROXMOX.md](PROXMOX.md)
konfigurieren. Das Root-Verzeichnis `/opt/learnpip/shared` und die PostgreSQL- und Caddy-Volumes
regelmäßig extern sichern. Anschließend:

```sh
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml config --quiet
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml build api worker web migrate
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml up -d db
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml --profile ops run --rm migrate
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml up -d --no-build
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml ps
```

Die Migration muss erfolgreich enden, bevor die API neu gestartet wird. Anschließend
`https://<domain>/health/ready` prüfen.

## Aktualisieren

Die Release Notes auf Migrationshinweise prüfen. Im aktuellen Release-Verzeichnis vor dem
Wechsel `./scripts/backup-prod.sh` ausführen und die VM samt Secrets sichern. Den neuen
Versionstag in einem neuen `/opt/learnpip/releases/vX.Y.Z` wie oben entpacken und dieselben
beiden symbolischen Links auf `/opt/learnpip/shared` anlegen. **`prod-init.sh` nicht erneut
benötigt**, da die Geheimnisse erhalten bleiben. Aus dem neuen Release-Verzeichnis die sechs
Compose-Befehle oben ausführen. Das Compose-Projekt hat den festen Namen `learnpip`, sodass
benannte PostgreSQL- und Caddy-Volumes über Versionswechsel erhalten bleiben.

Das vorige Quellverzeichnis und dessen lokal gebaute Images für einen möglichen Rückfall zunächst
aufbewahren. Bei inkompatibler Schemaänderung ist ein Rückfall nur zusammen mit der passenden
Datenbanksicherung sicher. Alte Release-Verzeichnisse und ungenutzte Images später lokal
bereinigen; sie verbrauchen keinen GitHub-Speicher. PostgreSQL-Hauptversionen benötigen einen
separaten Upgradeprozess.
