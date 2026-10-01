# LearnPip auf Proxmox VE installieren

Diese Anleitung installiert den produktiven Compose-Stack in einer **Debian-13-VM** auf Proxmox VE. Sie setzt eine öffentlich erreichbare Domain voraus. Die vorhandene Host-Kernelkennung `7.0.14-14-pve` ist kein Debian-Versionsname und wird **nicht** in der VM installiert. Auf dem Proxmox-Host mit `uname -r` und `pveversion -v` Kernel und VE-Version getrennt prüfen. Docker läuft in der VM, nicht auf dem Proxmox-Host oder in einem LXC-Container. Die Proxmox-[FAQ](https://pve.proxmox.com/pve-docs/chapter-pve-faq.html) empfiehlt eine QEMU-VM für Docker-Anwendungen.

Alle folgenden Linux-Befehle, außer den ausdrücklich als Proxmox-Host bezeichneten, werden **in der Debian-VM** ausgeführt. Beispielwerte `learn.example.org`, `192.168.1.50` und VM-ID `125` ersetzen. Die Ressourcenvorschläge sind Ausgangswerte, keine gemessenen Mindestanforderungen.

## 1. Voraussetzungen und Netzwerk

- Ein Proxmox-Host mit funktionierender Bridge (typisch `vmbr0`), genügend freiem Speicher und Internetzugang für die VM.
- Eine Domain mit DNS-A-Record auf die öffentliche IPv4-Adresse; einen AAAA-Record nur setzen, wenn IPv6 bis zur VM funktioniert. Bei Heimanschluss TCP **80** und **443** am Router zur VM weiterleiten. Optional UDP **443** für HTTP/3. Auf Proxmox-/VM-Firewalls diese Ports zur VM zulassen; SSH **22** nur für eigene Administrationsnetze. Proxmox-Webzugang **8006** wird für LearnPip nicht veröffentlicht.
- Ein externer Ort für Datenbank-Dumps und eine Proxmox-VM-Sicherung. Ein Dump ausschließlich auf derselben VM schützt nicht bei Ausfall ihres Datenträgers.

Bei CGNAT oder fehlender Portweiterleitung kann Caddy mit dieser Konfiguration kein öffentliches Zertifikat über die normalen HTTP/TLS-Prüfungen beziehen. Dann zuerst einen öffentlich erreichbaren Reverse Proxy oder eine andere Zertifikatslösung bereitstellen; diese Anleitung setzt die direkte Erreichbarkeit voraus. [Caddy: HTTPS-Voraussetzungen](https://caddyserver.com/docs/quick-starts/https).

## 2. Debian-VM in Proxmox anlegen

1. Auf dem Proxmox-Host im Webinterface `https://<host>:8006` anmelden. Unter dem gewünschten Storage die aktuelle [Debian-13-amd64-netinst-ISO](https://www.debian.org/download) als ISO hochladen oder über **Download from URL** beziehen.
2. **Create VM** wählen; beispielsweise VM-ID `125`, Name `learnpip`. Als OS die Debian-ISO auswählen. Standardmäßige QEMU-/VirtIO-Geräte verwenden; für die Festplatte `SCSI` mit VirtIO-SCSI-Controller und für die Netzwerkkarte `VirtIO` an `vmbr0` wählen.
3. Als Startwerte **2 vCPU**, **4 GiB RAM**, **50 GiB Disk** wählen. Bei größeren Katalogen, Bildern oder mehreren Nutzern aufstocken. **Start at boot** aktivieren. QEMU Guest Agent in den VM-Optionen aktivieren (nach Installation im Gast).
4. Debian minimal installieren; **SSH server** und **standard system utilities** auswählen, keine grafische Oberfläche nötig. Einen administrativen Benutzer anlegen. Für die VM eine feste DHCP-Zuweisung am Router oder eine statische Adresse mit korrektem Gateway und DNS einrichten. Nach der Installation ISO aushängen und die VM starten.
5. In der VM anmelden und Pakete installieren:

```sh
sudo apt update
sudo apt full-upgrade
sudo apt install -y sudo curl jq ca-certificates openssl qemu-guest-agent
sudo systemctl enable --now qemu-guest-agent
ip -br address
```

Falls `sudo` bei der Debian-Installation nicht eingerichtet wurde, einmal als `root` anmelden, `apt install sudo` ausführen und den Administrationsbenutzer der Gruppe `sudo` hinzufügen; danach neu anmelden. Die VM-IP für DNS/Router merken. Bei aktiviertem Proxmox-Firewall-Schalter sowohl auf Datacenter-/Node-/VM-Ebene die tatsächlich wirksamen Regeln prüfen.

## 3. Docker Engine und Compose in der VM installieren

Die folgenden Befehle verwenden das offizielle Docker-Repository für Debian 13; [Docker dokumentiert die Installation und unterstützten Debian-Versionen](https://docs.docker.com/engine/install/debian/). Auf einer frischen VM gibt es keine kollidierenden Docker-Pakete.

```sh
sudo apt update
sudo apt install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
sudo tee /etc/apt/sources.list.d/docker.sources >/dev/null <<EOF
Types: deb
URIs: https://download.docker.com/linux/debian
Suites: $(. /etc/os-release && echo "$VERSION_CODENAME")
Components: stable
Architectures: $(dpkg --print-architecture)
Signed-By: /etc/apt/keyrings/docker.asc
EOF
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo systemctl enable --now docker
sudo docker version
sudo docker compose version
```

Wenn bereits `docker.io`, `docker-compose`, `containerd` oder `runc` installiert sind, vor dem Installieren die [Konfliktprüfung in der Docker-Anleitung](https://docs.docker.com/engine/install/debian/#uninstall-old-versions) durchführen. Den Nutzer nicht allein aus Bequemlichkeit zur `docker`-Gruppe hinzufügen; Docker-Zugriff verleiht weitreichende Rechte im Gast. Die folgenden Befehle verwenden deshalb eine Root-Shell innerhalb der VM.

## 4. LearnPip vorbereiten

**Mit Installationsskript:** Nach Einrichtung von Docker kann die kurze
[Skript-Anleitung](RELEASE-INSTALL.md#kurzer-weg-installationsskript) Download, Vorbereitung
und auf Wunsch Start übernehmen. Sie ersetzt die manuellen Befehle in Schritt 4 und 5;
die Netzwerk-, Backup- und Kontrollschritte bleiben erforderlich. Das Skript ist erst
ab dem ersten Release mit dieser Funktion enthalten. Die folgenden Befehle bleiben als
manueller Weg und für v1.0.0 verfügbar.

Für **Updates ohne sudo** den Rootless-Weg in der Skript-Anleitung wählen. Die einmalige
Einrichtung passt Benutzer-Docker und Router-Ports an; Betrieb und Updates erfolgen
danach als normaler Benutzer. Nicht nur die `docker`-Gruppe verwenden. Die folgenden
manuellen Schritte sind der alternativ unterstützte Rootful-Weg und benötigen Root.

Bei einer schon laufenden Git-Clone-Installation zuerst den
[Wechselpfad für vorhandene Konfiguration und Secrets](RELEASE-INSTALL.md#wechsel-von-einer-bisherigen-git-clone-installation)
verwenden; danach nach dem Updatepfad vorgehen.

Im Gast als Administrator das **neueste stabile GitHub Release** laden. Ein Git-Clone,
Git-Historie oder ein GitHub-Konto sind nicht erforderlich. Docker baut die Container aus
dem vollständigen Quellcode dieser Version; Basisimages und Paketabhängigkeiten benötigen
weiterhin Internetzugang. Die [stabile Release-Seite](https://github.com/kennfarbe/LearnPip/releases/latest)
zeigt automatisch auf das neueste veröffentlichte stabile Release. GitHub verwaltet diesen
Link; Semantic Release muss dafür keinen zusätzlichen `latest`-Tag erzeugen.

Die folgenden Befehle nutzen `curl` und `jq` aus Schritt 2. Bei einem Fehler stoppen und
zuerst die Ursache beheben. Insbesondere niemals in ein unvollständig entpacktes Release wechseln.

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

Die Versionsnummer wird einmal ermittelt; der Download bleibt auf genau diesen Tag
festgelegt, auch wenn zwischenzeitlich ein weiteres Release erscheint. Ein fehlendes
Release, HTTP-Fehler oder GitHub-API-Limit werden sichtbar gemeldet. Bereits vorhandene
Versionsverzeichnisse werden nicht überschrieben. Den Download bei Fehlern nicht weiterverwenden.
Eine bestimmte ältere Version lässt sich über [Release-Installation](RELEASE-INSTALL.md)
auswählen. `shared/.env.production` und `shared/secrets/` bleiben bei Updates erhalten.

In `deploy/.env.production` `LEARNPIP_DOMAIN=learn.example.org` durch **deine** öffentliche Domain ersetzen (ohne `https://` und ohne abschließenden Slash). Die Datei enthält optional die nicht geheimen SMTP- und OIDC-Angaben. `prod-init.sh` legt die Datenbank-Zugangsdaten, einen festen E-Mail-Code-Schlüssel und optionale leere Secret-Dateien unter `deploy/secrets/` an. Diese Dateien niemals in Git übernehmen oder nach dem ersten Datenbankstart neu erzeugen. Vor dem Start prüfen:

```sh
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml config --quiet
ls -ld deploy/secrets
ls -l deploy/secrets
```

Wenn E-Mail-Codes gebraucht werden, `LEARNPIP_MAIL_HOST`, `LEARNPIP_MAIL_FROM`, `LEARNPIP_MAIL_USERNAME` und ggf. `LEARNPIP_MAIL_PORT` setzen und das Passwort ohne Zeilenumbruch in `deploy/secrets/Mail__Password` schreiben. OIDC ist optional; dazu Authority, Client-ID und `deploy/secrets/Oidc__ClientSecret` setzen und beim Anbieter `https://<domain>/signin-oidc` registrieren. Zunächst kann LearnPip auch ohne SMTP/OIDC mit einem pseudonymen Konto genutzt werden; dessen einmal angezeigtes Wiederherstellungsgeheimnis sicher verwahren. Für OIDC bei Neustarts ist zusätzlich eine dauerhafte ASP.NET-Data-Protection-Schlüsselverwaltung erforderlich; die mitgelieferte Compose-Datei richtet diese noch nicht ein. Siehe [Identität](IDENTITY.md).

## 5. Start und Kontrolle

Noch im heruntergeladenen Release-Verzeichnis (`/opt/learnpip/releases/vX.Y.Z`) und in der Root-Shell der **VM**:

```sh
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml build api worker web migrate
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml up -d db
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml --profile ops run --rm migrate
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml up -d --no-build
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml ps
```

Die Migration muss erfolgreich enden, bevor die API gestartet wird. Nur Caddy bindet Host-Ports 80/443; PostgreSQL und API werden nicht separat ans LAN veröffentlicht. Caddy bezieht und erneuert das Zertifikat bei passendem DNS und erreichbaren Ports automatisch. Die fertige Anwendung ist unter `https://learn.example.org/` erreichbar. Prüfen:

```sh
curl -fsS https://learn.example.org/health/ready
curl -I https://learn.example.org/
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml logs --tail=100 proxy api
```

Die Domain im Beispiel ersetzen. `health/ready` bestätigt die Verbindung der API zur Datenbank; für einen vollständigen Funktionstest im Browser ein Konto anlegen, das Wiederherstellungsgeheimnis sichern und eine Anmeldung nach Neuladen ausprobieren.

## 6. Sicherung, Updates und Fehlerbehebung

Für eine sofortige Datenbanksicherung in der Root-Shell `./scripts/backup-prod.sh` ausführen. Standardziel ist `/root/learnpip-backups`; mit `LEARNPIP_BACKUP_DIR=/pfad/auf/anderem/datentraeger ./scripts/backup-prod.sh` kann das Ziel geändert werden. Den Dump **regelmäßig außerhalb der VM** speichern und `/opt/learnpip/shared/` geschützt mitsichern. Der Befehl löscht lokale Dumps nach 30 Tagen. Eine tägliche Planung beispielsweise per Root-Crontab: `0 2 * * * /opt/learnpip/current/scripts/backup-prod.sh >>/var/log/learnpip-backup.log 2>&1`. Die externe Kopie und deren Aufbewahrungsdauer gesondert einrichten. Zusätzlich eine Proxmox-VM-Sicherung einplanen; einen Wiederherstellungstest durchführen. [Betrieb](OPERATIONS.md) und [Datenbank-Wiederherstellung](DATABASE.md) enthalten weitere Einzelheiten.

Nach erfolgreichem Start und Healthcheck einen festen lokalen Pfad setzen:

```sh
ln -sfn "$PWD" /opt/learnpip/current
```

`current` zeigt nur auf die tatsächlich installierte und geprüfte Version. Dieser lokale
Link hat keine Verbindung zur GitHub-Release-Seite. Die obige Backup-Crontab verwendet ihn,
damit sie nach einem Versionswechsel weiter funktioniert.

Vor einem Update die [Versionshinweise](https://github.com/kennfarbe/LearnPip/releases/latest)
lesen, einen aktuellen Dump und eine externe VM-Sicherung erstellen:

```sh
cd /opt/learnpip/current
./scripts/backup-prod.sh
```

Dann die Download-Befehle aus Schritt 4 erneut ausführen, um das neue stabile Release in
ein **neues** Versionsverzeichnis zu entpacken. Die gemeinsamen Dateien verknüpfen;
`prod-init.sh` beim Update nicht erneut ausführen:

```sh
ln -s /opt/learnpip/shared/secrets deploy/secrets
ln -s /opt/learnpip/shared/.env.production deploy/.env.production
```

Nun die Compose-Befehle und Healthchecks aus Schritt 5 aus dem neuen Verzeichnis ausführen.
Erst nach erfolgreicher Prüfung `ln -sfn "$PWD" /opt/learnpip/current` setzen. Das feste
Compose-Projekt `learnpip` erhält die benannten Datenbank- und Caddy-Volumes. Es gibt kein
unbeaufsichtigtes Auto-Update: Migrationen und Release Notes müssen vor jedem Wechsel
geprüft werden. Das vorherige Verzeichnis und eine passende Datenbanksicherung zunächst
aufbewahren; bei inkompatiblen Migrationen reicht ein Zurücksetzen von `current` nicht.
Weitere Einzelheiten: [Installieren und Aktualisieren aus Releases](RELEASE-INSTALL.md).

Neue PostgreSQL-Hauptversionen erfordern einen geplanten Datenbank-Upgradeprozess; das Image-Tag allein genügt nicht. Bei Fehlern zuerst `docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml ps` und `logs --tail=100 db api web proxy worker` ausführen. Wenn Caddy kein Zertifikat erhält, DNS-A/AAAA, Portweiterleitung und Firewall **von außerhalb des Heimnetzes** testen. Wenn die API nicht gesund wird, den Migrationslauf und Datenbank-Logs prüfen. Nach Host-Neustart starten die Dienste dank `restart: unless-stopped` erneut; auch das mit `ps` und dem HTTPS-Healthcheck kontrollieren.

### Regelmäßiger Restore-Test und Alarm

Auf dem Proxmox-Gast die Schritte in [Backup und Restore-Test](OPERATIONS.md#backup-und-restore-test-lp-33)
monatlich ausführen. Nach einem erfolgreichen Test auf einer frischen VM ein privates
Bild und einen Lernversuch im Browser kontrollieren. `scripts/monitor-prod.sh`
alle 15 Minuten planen und die Fehlermeldungen an ein überwachtes Alarmziel
schicken. Die Proxmox-Snapshots auf dieselbe 30-Tage-Frist wie Datenbank-Dumps
begrenzen und zusätzlich den freien Speicher des Hosts überwachen.
