# PostgreSQL-Datenmodell und Sicherung

Der erste Datenbankstand verwendet PostgreSQL 18 und Entity Framework Core mit dem Npgsql-Provider. Primärschlüssel sind UUIDs, die Anwendung erzeugt sie vor dem Speichern. Zeitwerte sind `timestamp with time zone` und werden als UTC-`DateTimeOffset` geführt. Fremdschlüssel verhindern verwaiste Verknüpfungen; Konten und nutzerbezogene Inhalte werden zunächst logisch über `DeletedAtUtc` gelöscht.

## Tabellenübersicht

| Tabelle | Zweck und wichtige Beziehungen |
| --- | --- |
| `Accounts` | Konto ohne verpflichtende E-Mail-Adresse; kann externe Identitäten, Rollen, Fragen und Lernsitzungen besitzen. Löschung wird über `DeletedAtUtc` markiert. |
| `ExternalIdentities` | Anbieter und stabiler Subject-Wert einer gewählten Anmeldung; `(Provider, Subject)` ist eindeutig. Ein Konto kann mehrere Anmeldungen verbinden. |
| `Roles`, `AccountRoles` | Rollenbeschreibung nach Geltungsbereich und Zuordnung von Kontorollen; der Verbundschlüssel verhindert doppelte Zuweisung. |
| `Questions`, `QuestionVersions` | Frage gehört einem Konto. Jede veröffentlichte Bearbeitung wird als eigene nummerierte Version gespeichert; Frage und Versionsnummer sind eindeutig. |
| `AnswerOptions` | Antwortoptionen gehören zu einer Frageversion; die Sortierreihenfolge ist pro Version eindeutig. |
| `MediaAssets` | Private Medienmetadaten mit Besitzer, optionaler Frageversion, nicht öffentlichem Speicherschlüssel, MIME-Typ, Länge und Löschmarkierung. Die Datei selbst liegt später in einem privaten Objektspeicher. |
| `StudySessions`, `StudyAttempts` | Lernverlauf gehört einem Konto; jeder Versuch verweist auf die konkrete Frageversion, die beantwortet wurde. |
| `ExamObjectives`, `QuestionObjectives` | Lernziele mit einer n:m-Zuordnung zu Fragen. Ein Lernziel-Code ist eindeutig. |
| `StudyGroups`, `GroupMemberships`, `GroupQuestionShares` | Späterer Gruppenbereich mit Rollen, Mitgliedschaften und widerrufbaren Freigaben. Diese Tabellen schalten keine Gruppenfunktion frei; es gibt noch keinen Gruppen-Endpunkt. |

Die Zuordnung von Rollen und Freigaben ersetzt keine Berechtigungsprüfung in der API. Für Inhalte gilt standardmäßig privat. `PrivateMediaCatalog` liest nur Medien des übergebenen Kontos und blendet gelöschte Medien aus. Es gibt in diesem Grundgerüst keine öffentliche Medienabfrage und keine öffentliche URL. Eine spätere Gruppenabfrage muss Mitgliedschaft und aktive Freigabe prüfen.

## Migrationen

Migrationen liegen im Projekt `backend/src/LearnPip.Data/Migrations`. Die lokale Compose-Entwicklung führt sie vor dem Start der API über einen separaten einmaligen `migrate`-Dienst aus. Für einen kontrollierten Betrieb ist eine explizite Migrationsstufe mit vorheriger Sicherung und Freigabe vorgesehen; API-Startanfragen führen keine Schemaänderungen aus.

Neue Migration erstellen:

```sh
dotnet tool restore
dotnet ef migrations add NameDerAenderung \
  --project backend/src/LearnPip.Data/LearnPip.Data.csproj \
  --startup-project backend/src/LearnPip.Api/LearnPip.Api.csproj \
  --output-dir Migrations
```

Die Migrationstests erstellen eine neue, temporäre PostgreSQL-Datenbank, wenden alle Migrationen an, speichern Daten, wenden die Migrationen erneut an und prüfen anschließend, dass die vorhandenen Daten erhalten sind. Zusätzlich prüfen sie, dass die Medienabfrage nur Dateien des berechtigten Kontos liefert. Die Testdatenbank wird am Ende gelöscht.

## Sicherung und Wiederherstellung

Vor Migrationen oder Wartung zuerst eine aktuelle PostgreSQL-Sicherung außerhalb des Repositorys erstellen. Der lokale Stack muss mit `./scripts/dev-up.sh` gestartet und `deploy/.env` vorhanden sein.

```sh
umask 077
mkdir -p "$HOME/learnpip-backups"
backup_file="$HOME/learnpip-backups/learnpip-$(date -u +%Y%m%dT%H%M%SZ).dump"
docker compose --env-file deploy/.env --file deploy/compose.yaml \
  exec -T db sh -c 'pg_dump --format=custom --username "$POSTGRES_USER" "$POSTGRES_DB"' \
  > "$backup_file"
```

Wiederherstellung in eine leere oder eigens dafür vorbereitete Datenbank; sie überschreibt die Zieldatenbank. Vorher Web/API anhalten und die Zieldatenbank prüfen:

```sh
docker compose --env-file deploy/.env --file deploy/compose.yaml stop api worker
docker compose --env-file deploy/.env --file deploy/compose.yaml \
  exec -T db sh -c 'pg_restore --clean --if-exists --no-owner --username "$POSTGRES_USER" --dbname "$POSTGRES_DB"' \
  < "$backup_file"
docker compose --env-file deploy/.env --file deploy/compose.yaml up --detach
```

Nach der Wiederherstellung API-Liveness und -Readiness prüfen sowie einen gezielten Lesezugriff im privaten Testkonto durchführen. Für einen echten Betrieb müssen zusätzlich die privaten Mediendateien und die Verschlüsselungs- beziehungsweise Schlüsselverwaltung separat gesichert und gemeinsam mit der Datenbank wiederherstellbar sein. Medien-Objektspeicher ist noch nicht implementiert; diese Sicherungsstrecke ist vor dem ersten produktiven Foto-Upload zu ergänzen und zu testen.

Sicherungen enthalten private Lerninhalte und sind entsprechend zugriffsbeschränkt aufzubewahren. Passwortdateien, Sicherungen und lokale `.env`-Dateien werden nicht ins Repository eingecheckt.

## Private Bilddaten (LP-12)

Die Tabelle `MediaBlobs` speichert sanitisierte JPEG-/PNG-Bytes zu genau einem
`MediaAsset`. Diese PostgreSQL-Implementierung ist der erste private Speicheradapter.
Die API liefert Bytes nur nach Besitzerprüfung aus; beim Löschen des Assets entfernt
der Fremdschlüssel mit `ON DELETE CASCADE` die Bytes in derselben Transaktion.
Datenbanksicherungen enthalten damit auch alle privaten Bilder.

## Fragefassungen (LP-13)

`QuestionVersions` enthalten Auswahlmodus, Fach, Thema, Sprache, Herkunft, Lizenz
und Veröffentlichungszeitpunkt. `QuestionContentBlocks` ordnen Text und private
Bildreferenzen in Frage, Erklärung und jede Antwort ein. Veröffentlichungen fügen
eine neue Versionsnummer hinzu und bearbeiten keine alte Fassung.
`StudyAttemptSelections` speichert jede gewählte Antwort-ID pro Versuch zusätzlich
zum Korrektheitswert und der Version. Eine spätere Lösungsänderung verändert damit
weder die ursprüngliche Lösung noch die damalige Auswahl oder Bewertung.

## Entwürfe und private Kataloge (LP-14)

`QuestionDrafts` enthält einen bearbeitbaren Entwurf je Frage als begrenztes
JSON-Dokument. Er kann unvollständig und dauerhaft unveröffentlicht bleiben.
`PrivateCatalogs` gehören jeweils einem Konto; `Questions.PrivateCatalogId`
ordnet eine Frage optional einem Katalog zu. Beim Löschen des Katalogs wird
die Zuordnung auf `NULL` gesetzt und die Frage nicht gelöscht. Veröffentlichte
Fassungen verbleiben unverändert in `QuestionVersions`.
