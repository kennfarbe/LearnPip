# Betrieb auf einer VM (VPS oder Proxmox-Gast)

Die Produktionsumgebung steht in [`deploy/compose.prod.yaml`](../deploy/compose.prod.yaml), getrennt vom lokalen [`deploy/compose.yaml`](../deploy/compose.yaml). Sie benötigt eine Linux-VM mit Docker Engine, Compose v2, `openssl`, ein auf die VM zeigendes DNS A/AAAA-Record und eingehende TCP-Ports 80/443 (optional UDP 443). Weder der Docker-Socket noch ein Proxmox-Dienst wird in die Container eingebunden. Die VM sollte genug Speicher für PostgreSQL, drei Anwendungskomponenten und den Proxy haben.

## Erstinstallation

```sh
# Im Repository-Stammverzeichnis, nach Installation von Docker und Compose:
./scripts/prod-init.sh
# LEARNPIP_DOMAIN in deploy/.env.production auf den eigenen DNS-Namen ändern.
# Für E-Mail/OIDC optional die nicht geheimen Angaben in dieser Datei und
# die Passwörter in deploy/secrets/Mail__Password bzw. Oidc__ClientSecret setzen.
# Die Dateien und das Verzeichnis bleiben lokal (Rechte 0600 bzw. 0700).

# Images bauen, Datenbank starten und Schema explizit migrieren:
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml build api worker web migrate
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml up -d db
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml --profile ops run --rm migrate
# Danach API, Worker, Web und HTTPS-Proxy starten:
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml up -d --no-build
```

Caddy bezieht automatisch ein HTTPS-Zertifikat für die Domain. Nur der Proxy veröffentlicht Host-Ports; Web, API, Worker und PostgreSQL bleiben in Compose-Netzwerken. Die API ist unter `https://<domain>/api/v1` erreichbar, `/health/ready` prüft ihre Datenbankverbindung. `/signin-oidc` wird für den optionalen OIDC-Rückruf zur API geleitet. Die API akzeptiert Browser-Schreibzugriffe nur vom konfigurierten HTTPS-Origin.

Bei einem leeren `Mail__Password` und fehlendem Mail-Host ist der E-Mail-Weg deaktiviert; für E-Mail-Codes ist ein festes, zufällig generiertes `Authentication__EmailCodeKey` erforderlich. Den Schlüssel und die Datenbank-Zugangsdaten über Sicherungen behalten. Die Anwendung lädt Laufzeitgeheimnisse als Dateien aus `/run/secrets`; sie stehen nicht im Compose-Environment oder Git. Nach Änderung einer Secret-Datei den betroffenen Dienst neu erstellen (`docker compose ... up -d --force-recreate api`), damit der Bind-Mount den aktuellen Inhalt erhält. Kein `down --volumes` auf einer produktiven Installation ausführen.

Für den ersten Administrator ein vorhandenes Konto anmelden und anschließend den lokalen Einmalbefehl aus [Administration](ADMINISTRATION.md) mit dem API-Image und dessen Datenbank-Secret ausführen:

```sh
account_id="UUID_DES_VORHANDENEN_KONTOS"
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml \
  run --rm -e "Authentication__BootstrapAdminAccountId=$account_id" api --bootstrap-admin
```

Der Produktionsprozess bietet keinen öffentlichen Bootstrap-Endpunkt.

## Neustart, Upgrade und Sicherung

Der normale Neustart verwendet `docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml up -d --no-build`. Benannte Volumes halten die PostgreSQL-18-Daten (`/var/lib/postgresql`) sowie Caddys Zertifikate und Konfiguration über Container-Neuerstellungen hinweg. `restart: unless-stopped` startet die Dienste nach einem Host-Neustart erneut. Migrationsläufe sind bewusst getrennt und müssen vor dem Upgrade der API stattfinden.

Vor einem Upgrade Quellstand und Image-Versionen festlegen, eine Datenbanksicherung außerhalb der VM anlegen und auch `deploy/secrets` sicher aufbewahren:

```sh
./scripts/backup-prod.sh
# Neue Quellversion auschecken; dann:
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml build api worker web migrate
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml --profile ops run --rm migrate
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml up -d --no-build
```

Eine PostgreSQL-Hauptversion nicht allein durch Änderung des Image-Tags aktualisieren: dafür ist ein geplantes Datenbank-Upgrade mit eigener Sicherung/Wiederherstellung nötig. Beim Zurückrollen einer API-Version die Kompatibilität mit dem migrierten Schema prüfen; eine Datenbank-Migration wird nicht automatisch zurückgenommen.

Den Sicherungsbefehl täglich auf dem Host einplanen. Er entfernt lokale Dumps nach 30 Tagen; extern kopierte Dumps müssen dieselbe Frist einhalten. Die Datenbank enthält auch private Medien. Vor der Freigabe einer wiederhergestellten Datenbank zuerst die API anhalten und den Worker einmalig mit `docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml run --rm worker --run-once` ausführen. Dabei werden im Backup enthaltene, inzwischen abgelaufene Konten erneut geprüft und gelöscht. Danach API und Worker starten.

Der Worker prüft Konten täglich; bei Fehlern versucht er es stündlich erneut. Für Warnungen müssen die SMTP-Einstellungen auch am Worker verfügbar sein und ausgehend SMTP erreichbar sein. Ohne verifizierte E-Mail-Adresse oder SMTP entfallen E-Mail-Warnungen; Deaktivierung und Löschung laufen weiterhin. Die Zustellung einer Warnung ist je Phase höchstens einmal versucht; bei SMTP-Fehlern wird kein automatischer erneuter Versand angestoßen.

## Diagnose

```sh
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml ps
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml logs --tail=100 api proxy
curl --fail --silent --show-error "https://$(sed -n 's/^LEARNPIP_DOMAIN=//p' deploy/.env.production)/health/ready"
```

PostgreSQL, API, Worker, Web und Proxy haben Healthchecks. Die API meldet bei erreichbarer Datenbank Bereitschaft; die Migration muss vor dem Anwendungsstart explizit ausgeführt werden. Der Worker prüft seinen regelmäßig geschriebenen Heartbeat; Web und Proxy haben interne HTTP-Probes. Bei einem Proxy-Startfehler zuerst DNS und die Erreichbarkeit von 80/443 prüfen. Sicherungen enthalten private Lerninhalte; Zugriffsrechte und externe Aufbewahrung entsprechend festlegen. Der spätere private Mediendateispeicher ist noch nicht implementiert und muss vor produktiven Datei-Uploads gesondert gesichert werden.
