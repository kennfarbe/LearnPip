# API v1 und Berechtigungen

Die fachlichen Lese-Endpunkte liegen unter `/api/v1`. Weboberfläche und spätere App nutzen denselben HTTP-Vertrag. Das generierte OpenAPI-Dokument ist unter `/openapi/v1.json` abrufbar; mit laufender API kann es beispielsweise so gespeichert werden:

```sh
curl --fail http://localhost:8080/openapi/v1.json -o openapi-v1.json
```

Die OpenAPI-Datei beschreibt den aktuellen Vertrag. Für einen späteren Client kann sie als Eingabe eines OpenAPI-Generators dienen. Die Gesundheitsendpunkte `/health/live` und `/health/ready` gehören nicht zur fachlichen API.

| Endpunkt | Zugriff | Inhalt |
| --- | --- | --- |
| `GET /api/v1/questions?page=1&pageSize=20` | Aktives Konto | Nur eigene, nicht gelöschte Fragen |
| `GET /api/v1/questions/{id}` | Eigentümer oder Mitglied einer Gruppe mit aktiver Freigabe | Letzte Frageversion ohne Antwortoptionen |
| `GET /api/v1/groups/{id}/questions?page=1&pageSize=20` | Gruppenbesitzer oder Mitglied | Nur aktive, ausdrücklich für diese Gruppe freigegebene Fragen |
| `GET /api/v1/media/{id}` | Ausschließlich Eigentümer | Private Metadaten ohne Speicherschlüssel oder Datei-URL |

Antworten mit Daten haben ein `data`-Feld. Listen verwenden `data.items`, `data.page`, `data.pageSize` und `data.total`. `page` liegt zwischen 1 und 100000; `pageSize` zwischen 1 und 100. Ungültige Werte führen zu HTTP 400 mit einem Validierungsproblem. Fehler werden als `application/problem+json` ausgegeben; die Problemantwort enthält eine `traceId`. Für fehlende oder nicht lesbare Ressourcen kommt HTTP 404, damit private IDs nicht bestätigt werden. Ohne gültige Anmeldung kommt HTTP 401.

Die API akzeptiert lokal ausgegebene, widerrufbare Bearer-Sitzungen und ein geschütztes Browser-Cookie. Der Server speichert nur Token-Hashes und ordnet jede Anfrage einem vorhandenen, aktiven LearnPip-Konto zu. E-Mail-Codes und OIDC verbinden zusätzliche Identitätswege mit diesem Konto. Ein externer OIDC-`sub` wird nicht als Konto-ID interpretiert; ein frei gesetzter HTTP-Header gewährt keinen Zugriff. Einzelheiten stehen unter [Konten und Identitätswege](IDENTITY.md).

Serverseitige Policies prüfen das aktive Konto, Eigentum, aktive Gruppenmitgliedschaft und aktive Fassungsfreigaben. Eine Katalogfreigabe hält die zu diesem Zeitpunkt neuesten veröffentlichten Fassungen fest. Neue Fassungen und spätere Fragen bleiben privat, bis die Leitung den Katalog erneut ausdrücklich freigibt. Gruppenbilder sind nur lesbar, wenn genau die Fassung mit der Bildreferenz freigegeben ist; Medienverwaltung bleibt dem Eigentümer vorbehalten. `Moderation` und `Admin` werden aus Systemrollen in der Datenbank geprüft; eine Adminrolle erfüllt auch die Moderationspolicy. Diese Rollen eröffnen keinen generellen Zugriff auf private Fragen oder Fotos.

## Geschlossene Gruppen (LP-19)

| Endpunkt | Berechtigung und Wirkung |
| --- | --- |
| `GET /api/v1/groups/`, `POST /api/v1/groups/` | Eigene Gruppen auflisten bzw. Gruppe erstellen. |
| `POST /api/v1/groups/{id}/invitations` | Eigentümer oder Gruppenleitung erstellt einen Code (Ablauf innerhalb von 30 Tagen, 1 bis 1000 Nutzungen); Klartext nur in dieser Antwort. |
| `DELETE /api/v1/groups/{id}/invitations/{invitationId}` | Leitung widerruft den Code sofort. |
| `POST /api/v1/groups/join` | Angemeldetes Konto tritt mit gültigem Code bei; Ablauf, Widerruf und Nutzungsgrenze werden unter Datenbanksperre geprüft. |
| `GET /api/v1/groups/{id}/members`, `DELETE /api/v1/groups/{id}/members/{accountId}` | Mitglieder sehen die Liste; Leitung entfernt Mitglieder, Mitglieder können selbst austreten. |
| `GET /api/v1/groups/{id}/catalogs`, `PUT/DELETE /api/v1/groups/{id}/catalogs/{catalogId}` | Mitglieder sehen Freigaben; Leitung friert aktuelle Fassungen eigener Kataloge ein (wiederholtes `PUT` ergänzt neue Fassungen) oder hebt die eigene Freigabe auf. |
| `GET /api/v1/groups/{id}/questions` | Nur Mitglieder und Eigentümer sehen veröffentlichte Fragen freigegebener Kataloge. |

Der Gruppencode ist kein persönliches Anmeldegeheimnis. Der Server speichert seinen SHA-256-Hash; nach dem Beitritt wird der Code für die Mitgliedschaft nicht mehr benötigt. Ablauf und Widerruf sperren nur neue Beitritte. Der Gruppenbeitrag einer entfernten Person ist sofort unzugänglich.

## Fassungsrechte (LP-20)

Jede veröffentlichte Fassung beginnt im Zustand `private`. `PUT /api/v1/questions/{id}/versions/{number}/visibility` erlaubt dem Eigentümer nur `{"visibility":"private"}` zum Zurückziehen. Eine öffentliche Freigabe erfordert die unten beschriebene Moderation. Die anonymen Endpunkte `GET /api/v1/public/questions`, `GET /api/v1/public/questions/{id}/versions/{number}` und `GET /api/v1/public/media/{id}/content` liefern nur genehmigte Fassungen und Bilder, die in genau diesen Fassungen als Inhaltsblock referenziert sind. Unverknüpfte oder nur in einer privaten Fassung referenzierte Medien bleiben privat. Die Antwort enthält `visibility`, `license`, `source` und `authorAttribution` je Fassung.

## Öffentliche Einreichung und Moderation (LP-21)

| Endpunkt | Zweck |
| --- | --- |
| `GET /api/v1/questions/{id}/versions/{number}/submission-preview` | Eigentümer erhält private Inhaltsvorschau samt einmaligem, 15 Minuten gültigem Vorschautoken. |
| `POST /api/v1/questions/{id}/versions/{number}/submission` | Reicht genau diese Fassung mit Vorschautoken, Wahl aus `CC BY 4.0`, `CC BY-SA 4.0`, `CC0 1.0`, Urheberangabe, Rechtebestätigung, Bildrechtebestätigung und Alterserklärung ein. Antwort `pending` oder `minor_hold`; keine Veröffentlichung. |
| `GET /api/v1/questions/{id}/versions/{number}/submission` | Eigentümer sieht Status und Moderationsnotiz. |
| `GET /api/v1/moderation/submissions/`, `GET /{versionId}`, `GET /{versionId}/media/{mediaId}` | Nur Moderator/Admin: Warteschlange und private, auf diese Einreichung begrenzte Text- und Bildvorschau. |
| `POST /api/v1/moderation/submissions/{versionId}/decision` | Nur Moderator/Admin: `approve`, `reject` oder `changes_requested` mit dokumentierten Prüfschritten für Korrektheit, Bildrechte, persönliche Daten und Dubletten. Freigabe benötigt alle vier Bestätigungen; eigene Einreichungen können nicht selbst geprüft werden. |

Einreichungen mit Alterserklärung `minor` bleiben in `minor_hold`; Freigabe ist bis zu einem gesondert gestalteten Verfahren technisch gesperrt. Moderatorinnen können sie ablehnen oder zur Überarbeitung zurückgeben. Die Migration setzt bisher direkt öffentlich geschaltete Fassungen auf `private`, weil ihnen die Moderationsentscheidung fehlt. Für bereits betriebene Instanzen ist diese Änderung vor dem Upgrade einzuplanen.

Der Integrationstest verwendet eine temporäre PostgreSQL-Datenbank und eine ausschließlich im Testprojekt definierte Authentifizierung. Er prüft HTTP 401/404, Eigentümerzugriff, Gruppenfreigabe, das Verbergen des Speicherschlüssels, Paging-Validierung, Systemrollen und das generierte OpenAPI-Dokument. Der Test-Header ist nicht Teil der produktiven API.

## Private Bilder (LP-12)

`POST /api/v1/media/` akzeptiert `multipart/form-data` mit `file` (JPEG oder PNG,
maximal 5 MiB), `altText` (1–300 Zeichen) und optional `questionVersionId` einer
eigenen, nicht gelöschten Frage. Die angegebene MIME-Art muss zum dekodierten
Bild passen. Maximal 4096 × 4096 Pixel und 16 Megapixel sind erlaubt. Die API
dekodiert das Bild und kodiert die Pixel erneut, sodass EXIF-Ortsdaten und andere
Metadaten nicht gespeichert werden. Das Ergebnis liefert ID, MIME-Art,
Bildbeschreibung und gespeicherte Bytegröße.

`GET /api/v1/media/{id}` liefert Metadaten; `GET /api/v1/media/{id}/content`
liefert die Bildbytes nur für das aktive Besitzerkonto mit `Cache-Control:
private, no-store`. Fehlende und fremde IDs ergeben beide 404.
`DELETE /api/v1/media/{id}` entfernt Datensatz und Bildbytes. Die PostgreSQL-
Implementierung speichert Bytes in `MediaBlobs` mit kaskadierender Löschung; die
Schnittstelle `IPrivateMediaStore` kann später einen privaten Objektspeicher
anbinden. Die Storage-Key-Werte werden nicht über die API ausgegeben.

## Veröffentlichte Fragen und Bewertung (LP-13)

`POST /api/v1/questions/` veröffentlicht Version 1 einer eigenen Frage.
`POST /api/v1/questions/{id}/versions` veröffentlicht eine neue, unveränderliche
Fassung; parallele Veröffentlichungen derselben Frage werden in der Datenbank
serialisiert. Beide Endpunkte erwarten beispielsweise:

```json
{
  "selectionMode": "multiple",
  "subject": "Biologie",
  "topic": "Pflanzen",
  "language": "de",
  "source": "Eigene Frage",
  "license": "CC-BY-4.0",
  "prompt": [{"kind":"text","text":"Welche Aussagen treffen zu?"}, {"kind":"image","mediaId":"<eigene-medien-uuid>"}],
  "explanation": [{"kind":"text","text":"Beide Aussagen stimmen."}],
  "answers": [
    {"isCorrect":true,"blocks":[{"kind":"text","text":"A"}]},
    {"isCorrect":true,"blocks":[{"kind":"image","mediaId":"<eigene-medien-uuid>"}]},
    {"isCorrect":false,"blocks":[{"kind":"text","text":"C"}]}
  ]
}
```

Blöcke sind in Array-Reihenfolge geordnet. `text` benötigt Text, `image` eine
eigene aktive Medien-ID mit Bildbeschreibung; Mischformen pro Block sind nicht
erlaubt. Pro Abschnitt sind bis zu 20 Blöcke und pro Frage 2–8 Antworten möglich.
`single` hat exakt eine richtige Antwort, `multiple` mindestens zwei. Fach, Thema,
Sprache, Herkunft und Lizenz gehören zur Version und bleiben bei späteren
Änderungen nachvollziehbar. Bilder in veröffentlichten Fassungen können nicht
separat gelöscht werden, damit historische Inhalte erhalten bleiben.

`GET /api/v1/questions/{id}/versions/{number}` liefert dem Besitzer die
vollständige Fassung einschließlich der richtigen Antwortmenge. Die allgemeine
Fragenabfrage bleibt auf ihre bisherige Zusammenfassung beschränkt.
`POST /api/v1/questions/{id}/attempts` nimmt `versionId` und
`selectedOptionIds` entgegen. Die Auswahl muss genau aus den Optionen dieser
Version stammen, ohne Duplikate. Die Bewertung vergleicht die gesamte Menge;
Teiltreffer sind falsch. Die Antwort enthält nach dem Versuch die richtige Menge,
und die gespeicherte `StudyAttempt` verweist auf die unveränderte Version. Die
gewählten Optionen werden einzeln in `StudyAttemptSelections` gespeichert.

## Private Kataloge und Editorentwürfe (LP-14)

`GET/POST /api/v1/catalogs/`, `PUT/DELETE /api/v1/catalogs/{id}` und
`GET /api/v1/catalogs/{id}/questions` verwalten ausschließlich die Kataloge
des angemeldeten Kontos. Das Löschen eines Katalogs löst seine Zuordnungen;
Fragen und ihre Fassungen bleiben erhalten. Ein Katalogname ist pro Konto
eindeutig und auf 120 Zeichen begrenzt. `description` ist optional und darf
höchstens 2048 Zeichen enthalten.

`POST /api/v1/questions/drafts` und `PUT /api/v1/questions/{id}/draft`
speichern `{ "content": <QuestionPublishRequest>, "catalogIds": ["uuid", "uuid"] }`.
Die vollständige Auswahl ersetzt alle bisherigen Zuordnungen; ein leeres Array
entfernt sie. Fehlt `catalogIds`, wird `catalogId` weiterhin als Einzelzuordnung
verwendet. Entwurfsantworten enthalten beide Felder. Mehrfachzuordnungen duplizieren
weder die Frage noch ihren Lernfortschritt.
Unvollständige Entwürfe sind erlaubt (bis 64 KiB JSON) und erzeugen keine
veröffentlichte Fassung. `GET /api/v1/questions/drafts` listet nur eigene
Entwürfe; `GET /api/v1/questions/{id}/draft` liest einen einzelnen. Mit
`PUT /api/v1/questions/{id}/catalog` und `{ "catalogId": null | "uuid" }`
ordnet man auch bereits veröffentlichte eigene Fragen um.

Erst `POST /api/v1/questions/{id}/publish` validiert den Entwurf vollständig
und erzeugt die nächste unveränderliche Fassung. Auch eine veröffentlichte
Fassung bleibt standardmäßig privat und wird damit nicht automatisch für
andere Konten freigegeben. Änderungen am Entwurf verändern die bisherige
Fassung nicht.

`GET /api/v1/catalogs/questions` liefert alle eigenen Fragen einschließlich
`catalogIds` ohne zusätzliches Exportrecht. Einzelzuordnungen lassen sich mit
`PUT/DELETE /api/v1/catalogs/{id}/questions/{questionId}` idempotent ändern;
alle anderen Zuordnungen bleiben bestehen. Dafür ist `editOwn` erforderlich.
Details zu Migration, Grenzen und älteren Clients:
[Eigene Kataloge verwalten](PRIVATE-CATALOGS.md).

## Kurze Lernsitzungen (LP-15)

`POST /api/v1/learning/sessions/` mit `{ "catalogId": null | "uuid", "count": 5 }`
startet eine Sitzung mit bis zu zehn eigenen veröffentlichten Fragen (neueste
Fassung je Frage). Ein gewählter Katalog berücksichtigt sämtliche
Mitgliedschaften; ohne Katalog werden alle eigenen Fragen verwendet. Die
Fragenfolge und die Antwortoptionen werden für jede Sitzung einmal zufällig
gemischt und bleiben beim erneuten Laden in derselben Reihenfolge. Entwürfe
werden nicht berücksichtigt. Die Antwort enthält die Sitzungs-ID, Zähler und
die aktuelle Frage ohne Kennzeichnung richtiger Antworten.

`GET /api/v1/learning/sessions/{id}` lädt die aktuelle Frage und den Fortschritt.
`POST /api/v1/learning/sessions/{id}/answer` mit
`{ "selectedOptionIds": ["uuid"] }` prüft die gesamte gewählte Menge und
liefert danach die korrekten IDs sowie optional eine kurze und die vollständige
Erklärung. Bei Einzelantwort ist genau eine Auswahl zulässig.
`POST /api/v1/learning/sessions/{id}/skip` geht ohne Bewertung zur nächsten
Frage. Übersprungene Fragen erzeugen keinen `StudyAttempt`. Beantwortete oder
übersprungene Fragen können innerhalb der Sitzung nicht wiederholt werden.

## Adaptiver Wiederholungsplan (LP-16)

`GET /api/v1/learning/review` liefert pro **Lerninhalt** die zugeordneten
Frage-IDs, sichere Antwortserie, Fälligkeit, Anzahl Antworten, Rateversuche
und Erklärungsabrufe. Die Zielquote ist `masteredContents / totalContents`:
Ein Inhalt gilt nach drei aufeinanderfolgenden sicheren Antworten als
beherrscht. Varianten desselben Inhalts erhöhen den Nenner nicht.

Jede eigene Frage erhält zunächst einen eigenen Lerninhalt. Mit
`PUT /api/v1/learning/questions/{id}/content` und `{ "contentId": "uuid" }`
können eigene Fragevarianten einem vorhandenen eigenen Inhalt zugeordnet
werden. Die Planung wird anschließend aus den bisherigen Versuchen neu
berechnet. Eine Sitzung wählt höchstens eine Variante je Inhalt und bevorzugt
fällige sowie persönlich markierte Inhalte. Antwortoptionen bleiben gemischt.

`POST /api/v1/learning/sessions/{id}/answer` nimmt zusätzlich das optionale
`wasGuessed` an. Die Rückmeldung enthält `attemptId` und `contentId`.
`POST /api/v1/learning/sessions/{id}/explanation` mit
`{ "attemptId": "uuid" }` erfasst den erstmaligen Abruf der ausführlichen
Erklärung zum eigenen Versuch. Ohne hinterlegte Erklärung ist er nicht
möglich. Ein richtiges sicheres Ergebnis verlängert die Frist zunächst um
1, dann 3, 7 und ab der vierten sicheren Antwort um 14 Tage. Falsch,
geraten oder ein späterer Erklärungsabruf setzt die sichere Serie zurück und
setzt die nächste Wiederholung auf einen Tag nach diesem Ereignis. Der
Zustand wird chronologisch aus gespeicherten Ereignissen rekonstruiert;
übersprungene Fragen ändern ihn nicht.

`PUT/DELETE /api/v1/learning/contents/{id}/often-for-me` pflegt die
persönliche Merkliste. Sie beeinflusst nur die Auswahlpriorität, weder die
berechnete Fälligkeit noch die Zielquote. Die API gibt ausschließlich eigene
Inhalte und Versuche zurück.

## Fortschritt und Rückmeldung (LP-17)

`GET /api/v1/learning/progress` liefert ausschließlich den eigenen Lernweg:
beherrschte Inhalte je Fach und Thema, Inhalte mit einer sicheren Antwort
nach einer früheren Unsicherheit, Lerntage, Mitmachpunkte und vier gleitende
Sieben-Tage-Zeiträume. Fragevarianten zählen pro Thema als ein Lerninhalt.
Eine kurze Einheit zählt, wenn sie abgeschlossen ist und mindestens eine
Antwort enthält. Bloßes Überspringen erhöht weder Punkte noch Einheiten.

Mitmachpunkte entstehen aus beantworteten Inhalten: 1 für einen ehrlich
markierten Rateversuch, 2 für eine andere Antwort, 1 zusätzlich für eine
sichere richtige Antwort und 1 für eine abgerufene Erklärung. Pro Lerninhalt
und UTC-Tag zählen höchstens 4 Punkte; wiederholte Klicks auf Varianten
summieren sich nicht. Es gibt weder Zeitbonus noch einen Tages-Streak.
Eine Pause löscht keine Punkte oder Ergebnisse. Wochenwerte sind ein Blick
auf Aktivität, keine Pflicht oder Leistungsbewertung.
