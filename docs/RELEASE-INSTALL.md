# LearnPip aus einem GitHub Release installieren und aktualisieren

Jedes GitHub Release bietet automatisch ein vollständiges Quellcodearchiv (`tar.gz` oder `zip`)
für den Versionstag. Es enthält Dockerfiles, produktive Compose-Datei, Initialisierungs- und
Backup-Skripte und die Dokumentation. Docker baut die API, Worker und Web-App aus **diesem Tag**
auf der eigenen VM; es müssen keine großen versionierten Container-Images in GitHub Packages
aufbewahrt werden. Voraussetzung: Docker Engine mit Compose-Plugin, Internetzugang für Basisimages
und Paketabhängigkeiten, genügend Speicher, Domain und DNS. Die [Proxmox-Anleitung](PROXMOX.md)
beschreibt die VM, Docker, Firewall, Mail und OIDC.

## Kurzer Weg: Installationsskript

Das Skript `scripts/install-release.sh` übernimmt Release-Download, Entpacken,
gemeinsame Konfiguration/Secrets und auf ausdrücklichen Wunsch Build, Migration und Start.
**Nur in der Debian-VM ausführen**, niemals auf dem Proxmox-Host. Docker Engine und
Compose müssen wie in [PROXMOX.md](PROXMOX.md) bereits eingerichtet sein.

### Empfohlen: Einmalige Einrichtung, danach ohne sudo

Für unprivilegierte Installation und Updates **Docker Rootless** als dedizierten
Linux-Benutzer verwenden. Mitgliedschaft in der normalen `docker`-Gruppe ist keine
Alternative: sie verleiht praktisch Root-Rechte. Die folgenden Administrationsschritte
sind einmalig auf einer **frischen VM ohne laufende Container** erforderlich, nachdem
das offizielle Docker-Repository aus der Proxmox-Anleitung eingerichtet wurde:

```sh
sudo apt install -y uidmap dbus-user-session slirp4netns docker-ce-rootless-extras
# Nur auf einer frischen VM: rootful Docker nicht parallel automatisch betreiben.
sudo systemctl disable --now docker.service docker.socket
sudo loginctl enable-linger "$USER"
```

Danach als normaler Benutzer (kein `sudo -i`):

```sh
dockerd-rootless-setuptool.sh install
systemctl --user enable --now docker
docker context use rootless
docker info
```

`docker info` muss `rootless` unter Security Options anzeigen. Docker benötigt
Subuid/Subgid-Bereiche für den Benutzer; bei fehlenden Bereichen die
[offizielle Rootless-Anleitung](https://docs.docker.com/engine/security/rootless/)
verwenden. Compose **2.24.4 oder neuer** ist für die Port-Override-Datei erforderlich.
Der Installer prüft bei einem normalen Benutzer den tatsächlich aktiven Docker-Daemon
und verweigert Installation/Updates über einen rootful Daemon.

Rootless bindet am VM-Host **8080** und **8443** statt 80/443. Router/Firewall einmalig
auf öffentlich TCP 80 → VM 8080 und TCP/UDP 443 → VM 8443 einrichten; DNS muss passen.
Alternativ einen vorhandenen vorgeschalteten Proxy passend konfigurieren. Für Caddys
normale Zertifikatsprüfung muss die öffentliche Erreichbarkeit weiterhin gegeben sein.
Es wird weder das systemweite Limit für unprivilegierte Ports abgesenkt noch eine
zusätzliche Capability vergeben. Siehe [Rootless-Portregeln](https://docs.docker.com/engine/security/rootless/tips/).

Ohne Administratorrechte von Beginn an funktioniert dies nur, wenn Rootless Docker,
Benutzerbereiche und Netzwerk schon eingerichtet sind. Das Skript selbst braucht dann
auch bei der Erstinstallation kein sudo. Es verwendet standardmäßig `$HOME/learnpip`.
Rootful Installationen unter `/opt/learnpip` bleiben unterstützt, erfordern aber weiterhin
Root-Rechte; sie werden nicht automatisch in Rootless umgewandelt. Datenbank-Volumes
gehören zum jeweiligen Daemon und müssen beim Wechsel separat gesichert/wiederhergestellt werden.

Das Skript steht ab dem ersten Release mit dieser Änderung zur Verfügung; **v1.0.0 enthält
es noch nicht**. Bis dahin den manuellen Weg unten verwenden. Nach dem Merge dieses
Feature-PRs veröffentlicht Semantic Release eine neue Version.

```sh
sudo apt install -y curl ca-certificates python3 openssl util-linux
release_tag=$(curl -fsSL https://api.github.com/repos/kennfarbe/LearnPip/releases/latest | \
  python3 -c 'import json,sys; r=json.load(sys.stdin); assert not r["draft"] and not r["prerelease"]; print(r["tag_name"])')
printf '%s\n' "$release_tag" | grep -Eq '^v[0-9]+\.[0-9]+\.[0-9]+$' || exit 1
installer_file=$(mktemp /tmp/learnpip-installer-XXXXXXXX.sh)
curl -fSL "https://raw.githubusercontent.com/kennfarbe/LearnPip/$release_tag/scripts/install-release.sh" \
  -o "$installer_file" || exit 1
less "$installer_file"
# Eigene Domain einsetzen. Das Skript fragt vor dem Start nach einer Bestätigung.
bash "$installer_file" install --version "$release_tag" --domain learn.meine-domain.de
```

Der Download wird erst angesehen und dann ausgeführt; kein `curl | sudo bash`.
Ohne Argumente bzw. mit `prepare` wird **nur vorbereitet**, nicht gestartet:

```sh
bash "$installer_file" prepare --version "$release_tag"
nano "$HOME/learnpip/shared/.env.production"
bash "$installer_file" install --version "$release_tag"
```

Diese Variante erlaubt SMTP/OIDC und weitere Einstellungen vor dem ersten Start.
Das Skript erzeugt keine Domain, DNS-Einträge, Portweiterleitungen oder Admin-Konten.
Nach dem Start Anmeldung im Browser testen und externe Backups einrichten.

### Updates per Skript

Release Notes lesen und eine aktuelle **externe VM-Sicherung einschließlich Secrets**
erstellen. Dann in der VM:

```sh
bash "$HOME/learnpip/current/scripts/install-release.sh" update
```

Das Skript fragt vor Änderungen nach, erstellt mit dem bisherigen Release einen
Datenbank-Dump, baut die neue Version, führt Migrationen aus und prüft den HTTPS-
Readiness-Endpunkt. Erst danach wird `current` umgeschaltet. Ein schon installiertes
Release ist ein No-op. Konfiguration und Datenbank-Secrets werden nicht ersetzt.
`--version vX.Y.Z` wählt ein bestimmtes veröffentlichtes Stable Release;
`--directory /pfad/learnpip` eine eigene Installationsbasis (bei allen Aufrufen dieselbe
verwenden). `--yes` überspringt nur die Bestätigungsfrage, nicht Backup oder Prüfungen.
Updates laufen als derselbe Benutzer und im selben Rootless-Docker-Kontext wie die
Installation; keine sudo-Passwortabfrage und kein Eintrag in der Docker-Gruppe nötig.
Diese Befehle gelten für den Rootless-Weg; für den ausdrücklich gewählten alten Rootful-
Weg `sudo bash` und `/opt/learnpip` verwenden. Die folgenden manuellen Abschnitte
dokumentieren weiterhin den Rootful-Weg.

Bei Download-, Build-, Backup- oder Migrationsfehlern bricht das Skript ab. Ein
fehlgeschlagener Healthcheck oder eine Migration kann bereits veränderte Dienste bzw.
Daten hinterlassen, auch wenn `current` noch auf die vorige Version zeigt. **Kein
automatischer Datenbank-Rollback.** Logs prüfen und den Wiederherstellungsplan verwenden.
Der lokale Dump allein schützt nicht vor einem VM-Ausfall. Bestehende Clone-Installationen
zuerst über den Wechselpfad unten auf gemeinsame Dateien und einen geprüften `current`-
Link umstellen; das Skript importiert keine laufende fremde Installation automatisch.

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
