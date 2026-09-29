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
