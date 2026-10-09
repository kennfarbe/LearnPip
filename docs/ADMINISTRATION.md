# Rollen und Administration (LP-10)

LearnPip unterscheidet die Systemrollen `user` (Standard für aktive Konten), `moderator` und `admin`. Moderierende dürfen ausdrücklich vorgesehene Moderationsaktionen ausführen, erhalten dadurch aber keinen Zugriff auf private Fragen, Medien oder fremde Gruppen. Gruppenrollen (`member`, `leader`) gelten nur innerhalb ihrer jeweiligen Gruppe. Gruppenbesitzende und Gruppenleitende können die Mitgliedschaft dort verwalten, aber keine Systemrollen vergeben.

## Ersten Administrator einrichten

Die Release-Installation erstellt direkt einen lokalen Administrator. Standardname ist `admin`; interaktiv kann ein anderer Name gewählt werden (1–64 ASCII-Buchstaben/Ziffern, Punkt, Unterstrich, Bindestrich; Groß-/Kleinschreibung wird vereinheitlicht). Ein eigenes Passwort wird verdeckt eingegeben und bestätigt. Leere Eingabe erzeugt ein kryptografisch zufälliges Passwort mit 192 Bit Entropie, das nach erfolgreicher Kontoeinrichtung einmalig am lokalen Terminal erscheint. Jetzt im Passwortmanager speichern. Passwortanforderungen: 12–128 Zeichen, keine verpflichtenden Zeichenklassen, keine stille Kürzung, nicht ausschließlich Leerzeichen.

```sh
./scripts/install-release.sh install --version latest --admin-user admin
```

Nichtinteraktive Installation benötigt `--admin-password-file /absoluter/pfad/zur/datei`. Die Datei enthält genau eine Passwortzeile, gehört dem installierenden Benutzer und hat Modus `600` oder `400`; symbolische Links und fremde Besitzer werden abgelehnt. Das Passwort wird über Standardeingabe an den einmaligen Ops-Container übertragen, niemals als Kommandoargument, Umgebungsvariable oder in der Compose-Konfiguration. Nach erfolgreicher Installation die selbst bereitgestellte Passwortdatei entfernen und den Zugang im Passwortmanager verwahren. Keine Passwörter in Git, Shell-History oder CI-Logs; kein `bash -x` für Passwortabläufe.

Unter **Einstellungen > Administrator-Anmeldung** anmelden. **Lokales Passwort ändern** verlangt das bisherige Passwort und die Bestätigung des neuen Passworts. Jede erfolgreiche Änderung widerruft sämtliche Sitzungen; anschließend erneut anmelden. Der Server nutzt ASP.NET Core Identity PasswordHasher (versioniertes, zufällig gesalzenes PBKDF2-HMAC-SHA512, 600.000 Iterationen), gleiche Fehlerantworten, IP-Rate-Limits und eine vorübergehende Kontosperre nach fünf Fehlversuchen für 15 Minuten. Cookiezugriffe und auch die Anmeldung prüfen die gleiche Herkunft gegen CSRF. Es gibt keinen öffentlichen Registrierungs-/Reset-/Bootstrap-Endpunkt für Administratoren.

Einrichtung, Bootstrap-Marker, Rolle und Audit werden gemeinsam unter PostgreSQL-Transaktionssperre gespeichert. Parallele Einrichtung erzeugt genau einen Administrator. Wiederholung lässt Zugang und Rollen unverändert (Ops-Exitcode `10`); Updates rufen die Einrichtung nicht auf. Eine fehlgeschlagene Migration oder Einrichtung meldet keinen Installationserfolg. Bestehende Code-/E-Mail-/Provider-Konten und frühere Administratoren bleiben bestehen.

### Lokale Wiederherstellung und bestehende Installationen

Eine bestehende Installation ohne Administrator nach abgeschlossener Migration
direkt auf dem Docker-Host einrichten:

```sh
cd ~/learnpip/current
./scripts/setup-admin.sh
```

Das ausführbare Skript wird mit dem Release ausgeliefert. Bei einem abweichenden
Installationspfad dessen `current`-Verzeichnis verwenden. Bash, Python 3 und
Docker Compose müssen verfügbar sein; derselbe Benutzer und Docker-Kontext wie
beim bestehenden Deployment genügen, auch bei Rootless Docker ohne `sudo`.
Es verwendet die vorhandene `deploy/.env.production`, die Compose-Konfiguration
einschließlich LAN-/Rootless-Modus und die bestehenden Secrets. Der gleiche
Compose-Projektname muss gelten; eine eigene Einstellung über
`COMPOSE_PROJECT_NAME` auch beim Aufruf beibehalten. Ohne bestehende Datenbank
im Projekt wird die Einrichtung abgelehnt. Das Skript startet keine Datenbank,
führt keine Migration aus und legt kein zweites Deployment an.

Der Benutzername hat die Vorgabe `admin`; ungültige Namen werden erneut abgefragt.
Das Passwort wird am Terminal verdeckt eingegeben und ein zweites Mal bestätigt.
Ungültige Passwörter oder abweichende Bestätigung führen zur erneuten Eingabe
im selben Aufruf. Es gelten 12–128 UTF-16-Codeeinheiten wie im Backend, nicht
ausschließlich Leerraum, keine Nullzeichen oder Zeilenumbrüche. Ein Zeichen
außerhalb der Unicode-Basisebene, etwa ein Emoji, zählt dabei als zwei Einheiten.
Leerzeichen im Passwort bleiben erhalten; es gibt keine zusätzlichen
Zeichenklassen und kein automatisches Zufallspasswort in diesem Skript.

Passwörter werden ausschließlich über stdin an `initialize-admin` übertragen,
ohne Passwortdatei, Prozessargument oder exportierte Umgebungsvariable.
Container-Ausgaben werden nicht durchgereicht. Der Backend-Rückgabecode `10`
wird als „Administrator bereits eingerichtet; unverändert“ angezeigt und ist
ein erfolgreicher Aufruf ohne Änderung. Andere Docker-/Backendfehler, EOF oder
Abbruch melden keinen Erfolg und führen zu keiner weiteren Eingabeschleife.
Bei Abbruch während eines bereits laufenden Ops-Aufrufs den tatsächlichen
Administratorstatus prüfen; eine abgeschlossene Datenbanktransaktion kann
nicht durch den Abbruch rückgängig gemacht werden.

Danach unter **Einstellungen > Administrator-Anmeldung** anmelden. Vorhandene
Konten und Administratorzugänge werden durch das Skript nicht überschrieben;
ein Passwort-Reset ist weiterhin eine getrennte lokale Aktion:

Bei vergessenem Passwort nur auf dem vertrauenswürdigen Host mit Datenbankzugang arbeiten. In einer geschützten Datei zwei Zeilen bereitstellen: bestehender Benutzername und neues Passwort. Dann im aktuellen Release-Verzeichnis mit denselben Compose-Dateien wie die Installation ausführen:

```sh
docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml \
  --profile ops run --rm -T initialize-admin --reset-admin-password < /geschuetzte/reset-datei
```

Bei Rootless-/LAN-Betrieb die zugehörigen `-f deploy/compose.rootless.yaml` und `-f deploy/compose.internal.yaml` vor `--profile` ergänzen. Der lokale Reset verändert keine Rollen, widerruft alle Sitzungen und wird ohne Passwortwerte auditiert. Passwortdatei anschließend entfernen. Host-/Datenbankzugang ist der Identitätsnachweis; dieser Weg darf niemals öffentlich exponiert werden.

Ein schon verwendeter Bootstrap bleibt gesperrt. Bereits bestehende Administratoren behalten ihren bisherigen Anmeldeweg; es wird ihnen beim Update kein Passwort aufgezwungen.

Der bisherige lokale Weg für ein bereits vorhandenes Konto bleibt verfügbar:

```sh
Authentication__BootstrapAdminAccountId=<UUID-des-bestehenden-Kontos> dotnet LearnPip.Api.dll --bootstrap-admin
```

Zuvor `dotnet LearnPip.Api.dll --migrate` ausführen. Alle Bootstrap-Wege verwenden dieselbe Datenbanksperre und denselben Marker. Der Schutz des letzten Administrators bleibt erhalten.

## Administrations-API

`/api/v1/admin/*` erfordert eine gültige Administratorsitzung, die höchstens 15 Minuten alt ist. Danach erneut anmelden. Schreibzugriffe per Sitzungscookie unterliegen der Prüfung der konfigurierten gleichen Herkunft (Origin/CSRF); Bearer-Token werden ebenfalls unterstützt.

- `PUT` und `DELETE /api/v1/admin/accounts/{accountId}/roles/{moderator|admin}`: Systemrolle vergeben oder entziehen.
- `PUT /api/v1/groups/{groupId}/members/{accountId}/role` mit `{"code":"member"}` oder `{"code":"leader"}`: Gruppenrolle als berechtigte Gruppenleitung verwalten.
- `PUT /api/v1/admin/settings/maintenance-notice` mit `{"value":"..."}`: administrativen Hinweis mit maximal 1.000 Zeichen setzen.
- `GET /api/v1/admin/audit`: die 100 jüngsten Änderungen mit Akteur, Ziel, altem/neuem Wert und UTC-Zeit abrufen. Der lokale Bootstrap hat keinen Konto-Akteur.

Der Wartungshinweis ist ein administrativer Wert; konsumierende Komponenten entscheiden über seine Anzeige. Beliebige Konfigurationsschlüssel oder Geheimnisse lassen sich nicht über die API ändern.

## Webbasierte Release-Prüfung und Updates (Issues #75–#77)

Administrierende sehen installierte Version, neuestes geprüftes stabiles Release, Versionsstatus und Release Notes. Die Prüfung kann manuell oder nach einem Intervall (`daily`, `weekly`, `monthly`, `never`) erfolgen. Zum Anstoßen eines Updates muss die konkrete Zielversion ausdrücklich bestätigt werden.

- `GET /api/v1/admin/updates`: Status und gegebenenfalls Auftrag anzeigen.
- `POST /api/v1/admin/updates/check`: stabile Releases manuell prüfen.
- `PUT /api/v1/admin/updates/interval`: Prüfintervall setzen.
- `POST /api/v1/admin/updates/install` mit `{"version":"vX.Y.Z"}`: verifizierten Auftrag einreihen.

**Wichtig:** Der API-Container selbst führt weder Host-Kommandos noch Docker-Aktionen aus. Der gesonderte, unprivilegierte Update-Operator muss auf dem Host konfiguriert und gestartet sein; ohne ihn ist kein Web-Update betriebsbereit. Vor dem Update sind ein funktionierendes Backup, ausreichend Speicher und Rootless Docker erforderlich. Ein erfolgreicher Download allein bedeutet noch kein erfolgreiches Update. Architektur, Inbetriebnahme und Fehlerbehandlung stehen unter [Admin-Web-Updates](admin-web-updates.md); die allgemeinen Betriebsabläufe unter [Betrieb](OPERATIONS.md).

## Optionale Lernpakete

Paketverwaltung erfordert ein Administratorkonto mit frischer Anmeldung. Lokale
ZIPs und konfigurierte neutrale HTTPS-Quellen sind optional. Jede Bereitstellung,
Deaktivierung und Entfernung verlangt gesonderte Bestätigung und wird auditiert.
Private Kopien und Lernstände bleiben erhalten.
[Anleitung mit Lizenzprüfung und Community-Sperre](CATALOG-INTERCHANGE.md#optionale-instanzpakete-und-bestätigte-updates).
