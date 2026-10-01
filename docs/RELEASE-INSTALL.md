# LearnPip aus einem GitHub Release installieren und aktualisieren

Jedes GitHub Release bietet automatisch ein vollständiges Quellcodearchiv (`tar.gz` oder `zip`)
für den Versionstag. Es enthält Dockerfiles, produktive Compose-Datei, Initialisierungs- und
Backup-Skripte und die Dokumentation. Docker baut die API, Worker und Web-App aus **diesem Tag**
auf der eigenen VM; es müssen keine großen versionierten Container-Images in GitHub Packages
aufbewahrt werden. Voraussetzung: Docker Engine mit Compose-Plugin, Internetzugang für Basisimages
und Paketabhängigkeiten, genügend Speicher, Domain und DNS. Die [Proxmox-Anleitung](PROXMOX.md)
beschreibt die VM, Docker, Firewall, Mail und OIDC.

## Fester Link zum neuesten Stable Release

- Release-Seite: <https://github.com/kennfarbe/LearnPip/releases/latest>
- Metadaten für Skripte: <https://api.github.com/repos/kennfarbe/LearnPip/releases/latest>

GitHub aktualisiert `latest` automatisch bei Veröffentlichung eines stabilen Releases.
Drafts und Prereleases werden ausgeschlossen. Semantic Release veröffentlicht bereits
stabile Releases von `main`; eine zusätzliche Änderung seiner Konfiguration ist nicht nötig.
Ein beweglicher Git-Tag namens `latest` wird nicht angelegt. Die Versionstags bleiben bestehen.

`releases/latest/download/<Dateiname>` funktioniert nur für hochgeladene Release-Assets,
nicht für GitHubs automatisch generiertes Quellcodearchiv. LearnPip lädt deshalb einmal die
Metadaten und verwendet anschließend den konkreten Versionstag. So gibt es keinen zweiten
Archiv-Anhang, keine Container-Images und keinen zusätzlichen Release-Speicherbedarf.
Siehe [GitHub: Links auf Releases](https://docs.github.com/en/repositories/releasing-projects-on-github/linking-to-releases).

## Erstinstallation

In einer VM mit Docker und Compose `curl`, `jq`, `tar`, `ca-certificates` und `openssl`
installieren (Debian: `sudo apt install -y curl jq tar ca-certificates openssl`).
Quellen werden pro Version in einem eigenen Verzeichnis entpackt. Geheimnisse und
Konfiguration liegen außerhalb dieser Verzeichnisse und werden bei Updates wiederverwendet.
Bei Fehlern in den folgenden Befehlen stoppen; unvollständig entpackte Quellen nicht starten.

```sh
sudo -i
# Bei einem fehlgeschlagenen Befehl diese Root-Shell beenden.
set -e
install -d -m 700 /opt/learnpip/shared/secrets
install -d /opt/learnpip/releases

# GitHub latest enthält keine Drafts oder Vorabversionen.
release_json=$(curl --fail --silent --show-error --location \
  -H 'Accept: application/vnd.github+json' \
  https://api.github.com/repos/kennfarbe/LearnPip/releases/latest)
release_tag=$(printf '%s' "$release_json" | jq -er \
  'select(.draft == false and .prerelease == false) | .tag_name')
# Nur die vom Projekt verwendeten stabilen Versionstags akzeptieren.
printf '%s\n' "$release_tag" | grep -Eq '^v[0-9]+\.[0-9]+\.[0-9]+$' || exit 1
release_dir="/opt/learnpip/releases/$release_tag"
# Ein vorhandenes Verzeichnis niemals überschreiben.
[ ! -e "$release_dir" ] || { echo "Bereits vorhanden: $release_dir"; exit 1; }
archive_file=$(mktemp /tmp/learnpip-release-XXXXXXXX.tar.gz)
curl --fail --show-error --location \
  "https://github.com/kennfarbe/LearnPip/archive/refs/tags/$release_tag.tar.gz" \
  -o "$archive_file"
mkdir "$release_dir"
tar -xzf "$archive_file" --strip-components=1 -C "$release_dir"
rm "$archive_file"
cd "$release_dir"
printf 'Heruntergeladen: %s\n' "$release_tag"
```

Nur bei der **Erstinstallation** die gemeinsame Konfiguration und Secrets vorbereiten:

```sh
# Die gemeinsame Konfiguration vor dem Link anlegen (kein leerer Symlink).
[ -f /opt/learnpip/shared/.env.production ] || \
  cp deploy/.env.production.example /opt/learnpip/shared/.env.production
chmod 600 /opt/learnpip/shared/.env.production
ln -s /opt/learnpip/shared/secrets deploy/secrets
ln -s /opt/learnpip/shared/.env.production deploy/.env.production
./scripts/prod-init.sh
nano deploy/.env.production
```

### Eine konkrete Version statt latest installieren

Die Metadaten-Abfrage durch `release_tag=vX.Y.Z` ersetzen (`X.Y.Z` durch einen
veröffentlichten Versionstag ersetzen); danach dieselben Download- und Entpackbefehle
ab der Prüfung des Tags ausführen. Releases sind unter
<https://github.com/kennfarbe/LearnPip/releases> aufgelistet. Für reproduzierbare
Installationen die gewählte Version dokumentieren.

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
`https://<domain>/health/ready` und die Anmeldung im Browser prüfen. Erst nach erfolgreicher
Prüfung einen festen lokalen Pfad für Betrieb und Backup setzen:

```sh
ln -sfn "$PWD" /opt/learnpip/current
```

## Aktualisieren

Die Release Notes auf Migrationshinweise prüfen. Im aktuellen Release-Verzeichnis vor dem
Wechsel `./scripts/backup-prod.sh` ausführen und die VM samt Secrets sichern. Das neueste
stabile Release mit den obigen Download-Befehlen in ein neues Versionsverzeichnis entpacken und dieselben
beiden symbolischen Links auf `/opt/learnpip/shared` anlegen (die gemeinsame Konfiguration nicht kopieren oder ersetzen). **`prod-init.sh` nicht erneut
benötigt**, da die Geheimnisse erhalten bleiben. Aus dem neuen Release-Verzeichnis die sechs
Compose-Befehle oben ausführen. Das Compose-Projekt hat den festen Namen `learnpip`, sodass
benannte PostgreSQL- und Caddy-Volumes über Versionswechsel erhalten bleiben.

Das vorige Quellverzeichnis und dessen lokal gebaute Images für einen möglichen Rückfall zunächst
aufbewahren. `current` erst nach erfolgreichem Healthcheck auf das neue Verzeichnis setzen. Bei inkompatibler Schemaänderung ist ein Rückfall nur zusammen mit der passenden
Datenbanksicherung sicher. Alte Release-Verzeichnisse und ungenutzte Images später lokal
bereinigen; sie verbrauchen keinen GitHub-Speicher. PostgreSQL-Hauptversionen benötigen einen
separaten Upgradeprozess.

## Wechsel von einer bisherigen Git-Clone-Installation

Wenn LearnPip bereits aus `/opt/LearnPip` läuft, vor dem Wechsel einen Datenbank-Dump
und eine externe VM-Sicherung erstellen. **Keine neuen Datenbank-Zugangsdaten erzeugen.**
Vor der Erstinstallationsvorbereitung oben die vorhandenen Dateien einmal übernehmen:

```sh
sudo -i
set -e
install -d -m 700 /opt/learnpip/shared
[ ! -e /opt/learnpip/shared/.env.production ] || exit 1
[ ! -e /opt/learnpip/shared/secrets ] || exit 1
cp -a /opt/LearnPip/deploy/.env.production /opt/learnpip/shared/.env.production
cp -a /opt/LearnPip/deploy/secrets /opt/learnpip/shared/secrets
```

Dann das Release laden, die beiden Links anlegen und die Schritte unter **Aktualisieren**
ausführen. Den bisherigen Clone zunächst aufbewahren. Der feste Compose-Projektname
`learnpip` bleibt derselbe; die Datenbank-Volumes werden weiterverwendet. Bei abweichendem
alten Projekt- oder Volumen-Namen zuerst eine gezielte Migration planen.
