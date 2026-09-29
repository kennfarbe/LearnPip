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

Serverseitige Policies prüfen das aktive Konto, Eigentum, aktive Gruppenmitgliedschaft und aktive Gruppenfreigaben. Eine Gruppenfreigabe einer Frage gibt **keinen** Zugriff auf ihre Medien. `Moderation` und `Admin` werden aus Systemrollen in der Datenbank geprüft; eine Adminrolle erfüllt auch die Moderationspolicy. Diese Policies sind für spätere Verwaltungsendpunkte vorbereitet und eröffnen hier keinen generellen Zugriff auf private Fragen oder Fotos. Vor Schreib- und Upload-Endpunkten müssen deren Berechtigungen ebenfalls ausdrücklich festgelegt und getestet werden.

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
