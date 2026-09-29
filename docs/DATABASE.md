# PostgreSQL-Datenmodell und Sicherung

Der Datenbankstand verwendet PostgreSQL 18 und Entity Framework Core mit dem Npgsql-Provider. Primärschlüssel sind UUIDs, die Anwendung erzeugt sie vor dem Speichern. Zeitwerte sind `timestamp with time zone` und werden als UTC-`DateTimeOffset` geführt. Fremdschlüssel verhindern verwaiste Verknüpfungen. Der Inaktivitätsjob entfernt abgelaufene Konten und ihre persönlichen Daten physisch in einer Transaktion.

## Tabellenübersicht

| Tabelle | Zweck und wichtige Beziehungen |
| --- | --- |
| `Accounts` | Konto ohne verpflichtende E-Mail-Adresse; `LastActivityAtUtc` und `DisabledAtUtc` steuern Inaktivität. |
| `AccountInactivityWarnings` | Einmalige Warnungsansprüche je Konto, Phase und Aktivitätszeitpunkt samt Versandstatus. |
| `ExternalIdentities` | Anbieter und stabiler Subject-Wert einer gewählten Anmeldung; `(Provider, Subject)` ist eindeutig. Ein Konto kann mehrere Anmeldungen verbinden. |
| `Roles`, `AccountRoles` | Rollenbeschreibung nach Geltungsbereich und Zuordnung von Kontorollen; der Verbundschlüssel verhindert doppelte Zuweisung. |
| `Questions`, `QuestionVersions` | Frage gehört einem Konto. Jede veröffentlichte Bearbeitung wird als eigene nummerierte Version gespeichert; Frage und Versionsnummer sind eindeutig. |
| `AnswerOptions` | Antwortoptionen gehören zu einer Frageversion; die Sortierreihenfolge ist pro Version eindeutig. |
| `MediaAssets` | Private Medienmetadaten mit Besitzer, optionaler Frageversion, nicht öffentlichem Speicherschlüssel, MIME-Typ, Länge und Löschmarkierung. Die Datei selbst liegt später in einem privaten Objektspeicher. |
| `StudySessions`, `StudyAttempts` | Lernverlauf gehört einem Konto; jeder Versuch verweist auf die konkrete Frageversion, die beantwortet wurde. |
| `ExamObjectives`, `QuestionObjectives` | Lernziele mit einer n:m-Zuordnung zu Fragen. Ein Lernziel-Code ist eindeutig. |
| `StudyGroups`, `GroupMemberships`, `GroupQuestionShares`, `GroupCatalogShares`, `GroupVersionShares` | Geschlossene Gruppen mit unabhängiger Mitgliedschaft und auf konkrete Fassungen beschränkten Katalogfreigaben. Neue Fassungen werden nicht automatisch sichtbar. |
| `GroupInvitations` | Einladungen mit ausschließlich gehashtem Code, Ablaufzeit, Nutzungsgrenze und Widerruf. |

Die API prüft aktives Konto, Mitgliedschaft und Freigabe bei jedem Zugriff. Für Inhalte gilt standardmäßig privat; `QuestionVersions.Visibility` wird erst durch den Eigentümer für eine konkrete Fassung auf `public` gesetzt. Eine Einladung ist kein Login und gewährt nach dem Beitritt keine zusätzliche Berechtigung; die gespeicherte Mitgliedschaft bleibt bei Ablauf oder Widerruf des Codes bestehen. Gruppen- und öffentliche Bilder werden nur ausgeliefert, wenn ein Inhaltsblock der lesbaren Fassung auf genau dieses Medium verweist; der Eigentümer kann es verwalten.

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

Vor Freigabe einer wiederhergestellten Instanz den Inaktivitätsjob mit `docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml run --rm worker --run-once` ausführen und danach API-Liveness und -Readiness prüfen. Medienbytes liegen in `MediaBlobs` und sind Teil des PostgreSQL-Dumps. Geheimnisse und Schlüssel müssen zusätzlich gesichert werden.

Sicherungen enthalten private Lerninhalte und sind entsprechend zugriffsbeschränkt aufzubewahren. Passwortdateien, Sicherungen und lokale `.env`-Dateien werden nicht ins Repository eingecheckt.

Der produktive Sicherungsbefehl `scripts/backup-prod.sh` löscht lokale Dumps nach 30 Tagen und ist täglich auf dem Host einzuplanen. Externe Kopien müssen nach derselben Frist entfernt werden. Ein Restore kann bereits gelöschte Daten zurückbringen; der einmalige Löschlauf vor API-Freigabe entfernt nach aktuellem Zeitpunkt erneut fällige Konten. Die Kontolöschung entfernt in einer Transaktion Sitzungen, Identitäten, Warnungen, Lernverlauf, Gruppenbezüge, Fragen, Fassungen, private Medien samt Bytes und das Konto. Scheitert ein Schritt, wird die Transaktion zurückgerollt.

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
Der optionale `StudySessions.PlanJson` enthält für kurze Lernsitzungen die
einmalig gemischte Reihenfolge der Frageversionen und Antwortoptionen sowie den
Status `pending`, `answered` oder `skipped`. Bisherige Einzelversuche ohne Plan
bleiben gültig. Ein übersprungener Eintrag hat keinen `StudyAttempt`.

`LearningContents` bündelt eigene Fragevarianten über `Questions.LearningContentId`.
Die Migration legt für vorhandene Fragen zunächst je einen Inhalt an.
`FrequentLearningContents` speichert die persönliche Markierung getrennt vom
Lernstand. `StudyAttempts.WasGuessed` und `ExplanationViewedAtUtc` halten
Unsicherheit und den erstmaligen Erklärungsabruf fest. Der Wiederholungszustand
wird aus den zeitlich sortierten Versuchen und Erklärungsereignissen berechnet,
nicht als veränderliche Quote gespeichert. Dadurch zählen mehrere Varianten
bei der Zielquote als ein Lerninhalt.

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
