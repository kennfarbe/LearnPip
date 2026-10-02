# Rollen und Administration (LP-10)

LearnPip unterscheidet die Systemrollen `user` (Standard für aktive Konten), `moderator` und `admin`. Moderierende dürfen ausdrücklich vorgesehene Moderationsaktionen ausführen, erhalten dadurch aber keinen Zugriff auf private Fragen, Medien oder fremde Gruppen. Gruppenrollen (`member`, `leader`) gelten nur innerhalb ihrer jeweiligen Gruppe. Gruppenbesitzende und Gruppenleitende können die Mitgliedschaft dort verwalten, aber keine Systemrollen vergeben.

## Ersten Administrator einrichten

Zunächst über die reguläre Anmeldung das vorgesehene Konto anlegen. Danach in einer vertrauenswürdigen Betriebsumgebung mit Datenbankzugang und dem aktuellen API-Image einmalig ausführen:

```sh
Authentication__BootstrapAdminAccountId=<UUID-des-bestehenden-Kontos> dotnet LearnPip.Api.dll --bootstrap-admin
```

Zuvor die Migrationen ausführen (`dotnet LearnPip.Api.dll --migrate`). Den Bootstrap-Schalter nicht im dauerhaft laufenden Webprozess bereitstellen und keine Konto-ID dauerhaft in dessen Umgebung speichern. Die Einrichtung erfolgt transaktional unter Datenbanksperre und erzeugt einen Audit-Eintrag. Weitere Bootstrap-Versuche werden abgelehnt, selbst wenn Administratorrollen später außerhalb der Anwendung entfernt werden. Die API verhindert das Entfernen des letzten aktiven Administrators.

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
