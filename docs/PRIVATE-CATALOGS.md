# Eigene Kataloge verwalten

Unter **Kataloge und Inhalte** können angemeldete Benutzer ihre privaten Kataloge
anlegen, umbenennen, beschreiben und löschen. Ein Name darf höchstens 120 Zeichen,
eine optionale Beschreibung höchstens 2048 Zeichen enthalten. Namen sind innerhalb
des eigenen Kontos eindeutig. Leerraum am Anfang und Ende wird entfernt.

Eine Frage kann gleichzeitig mehreren privaten Katalogen angehören. Im Editor
stehen die Kataloge unter **Sprache und Katalogzuordnung** als Mehrfachauswahl bereit.
Keine Auswahl bedeutet „ohne Katalog“. In **Katalog bearbeiten → Fragen zuordnen**
lassen sich vorhandene eigene Fragen hinzufügen oder entfernen. Dabei bleiben
andere Zuordnungen, Fragenfassungen und Lernstände erhalten. Es entstehen keine
Kopien der Fragen. Je Frage sind höchstens 100 Katalogzuordnungen möglich.

Die Katalogauswahl beim Lernen berücksichtigt auch zusätzliche Zuordnungen.
Innerhalb der Auswahl bleiben die vorhandene Wiederholungsgewichtung und die
Auswahl einer Variante je Lerninhalt erhalten. Die Fragenliste und die lokale
Exportauswahl filtern ebenfalls nach allen Zuordnungen. Eine Gruppenfreigabe
übernimmt wie bisher die zum Freigabezeitpunkt verfügbaren Fragenfassungen.
Eine Katalogzuordnung veröffentlicht keine Frage und gibt keine Inhalte frei.

Beim Löschen eines Katalogs werden ausschließlich der Katalog und seine Zuordnungen
entfernt. Fragen, Entwürfe, Fassungen und Lernergebnisse bleiben erhalten; Fragen
können anschließend weiterhin über andere Kataloge oder „ohne Katalog“ gefunden werden.
Auch Administratoren können über diese privaten Endpunkte keine fremden Kataloge
oder Fragen bearbeiten. Für Zuordnungsänderungen wird das Rollenrecht `editOwn`
benötigt, für das Anzeigen eigener Fragen `readOwn`.

## API und bestehende Installationen

Alle Endpunkte verwenden die angemeldete Identität; fremde Kataloge oder Fragen
werden nicht als bearbeitbare Ressourcen offengelegt.

| Endpunkt | Verhalten |
| --- | --- |
| `GET /api/v1/catalogs/` | Eigene Kataloge mit Beschreibung und eindeutiger Fragenanzahl |
| `POST /api/v1/catalogs/` | Katalog mit `name` und optionalem `description` anlegen |
| `PUT /api/v1/catalogs/{id}` | Name und Beschreibung ersetzen |
| `DELETE /api/v1/catalogs/{id}` | Katalog löschen, Fragen erhalten |
| `GET /api/v1/catalogs/questions` | Eigene Fragen einschließlich aller `catalogIds`, auch Entwürfe |
| `GET /api/v1/catalogs/{id}/questions` | Fragen des gewählten eigenen Katalogs |
| `PUT /api/v1/catalogs/{id}/questions/{questionId}` | Einzelne Zuordnung idempotent hinzufügen |
| `DELETE /api/v1/catalogs/{id}/questions/{questionId}` | Einzelne Zuordnung idempotent entfernen |

Entwurfsanlage und -speicherung akzeptieren `catalogIds` als vollständige Auswahl.
`[]` entfernt alle Zuordnungen. Fehlt `catalogIds`, wird das bisherige Feld
`catalogId` als Einzelzuordnung verwendet; die frühere Verschiebe-API ersetzt
weiterhin die gesamte Auswahl durch einen Einzelkatalog oder keine Zuordnung.
In Antworten bleibt `catalogId` für ältere Clients erhalten; neue Clients verwenden
`catalogIds`. Alte Clients können zusätzliche Zuordnungen deshalb beim Speichern
ersetzen. Gleichzeitige Einzelzuordnungen werden je Frage serialisiert.

Die Migration `CatalogMultipleMemberships` ergänzt die Beschreibung und die
n:m-Tabelle und übernimmt bestehende `PrivateCatalogId`-Werte. Das alte Feld bleibt
für kompatible Leser erhalten. Paketimporte ordnen auch wiederverwendete Fragen
dem Zielkatalog zu, ohne andere Mitgliedschaften zu entfernen. Ein Zurücksetzen
der Migration entfernt zusätzliche Zuordnungen und Beschreibungen; vor einem
Rollback ist eine Datenbanksicherung erforderlich.
