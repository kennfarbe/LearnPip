# C2 – Container und Update-Operator

Stand: 2. Oktober 2026 · [C1 – Systemkontext](C1-system-context.md)

Diese Sicht zeigt den Produktions-Stack aus [`deploy/compose.prod.yaml`](../../deploy/compose.prod.yaml) einschließlich der Update-Integration aus dem noch offenen PR #91. Der lokale Entwicklungs-Stack verwendet dieselben Kernteile mit lokal gebundenen Ports.

```mermaid
flowchart LR
    person["Lernende Person / Administration · Browser"]
    oidc["OIDC-Anbieter · optional"]
    smtp["SMTP-Server · optional"]
    github["GitHub Releases"]

    subgraph host["LearnPip-Host (Rootless Docker)"]
        operator["Update-Operator · eigener Benutzer-Dienst"]
        queue["Private Update-Warteschlange und Status"]
        subgraph instance["Docker-Compose-Stack"]
            proxy["Caddy · Reverse Proxy"]
            web["Web · Angular 22 / Nginx"]
            api["API · ASP.NET Core / .NET 10"]
            db[("PostgreSQL 18 · persistente Daten und Medien")]
            worker["Worker · .NET 10"]
            migrate["Migration · einmaliger API-Aufruf"]
        end
    end

    person -->|"HTTPS"| proxy
    proxy -->|"Seiten / statische Dateien"| web
    proxy -->|"/api/* /signin-oidc"| api
    api -->|"EF Core / Npgsql"| db
    migrate -->|"Schemaänderung"| db
    worker -->|"Kontolebenszyklus"| db
    worker -->|"Warnungen · SMTP"| smtp
    api <-->|"OIDC · HTTPS"| oidc
    api -->|"Anmeldecodes · SMTP"| smtp
    api -->|"validierter Auftrag"| queue
    operator <-->|"Auftrag / Status"| queue
    operator -->|"Release-Metadaten"| github
```

| Komponente | Verantwortung | Zugriff und Persistenz |
| --- | --- | --- |
| Caddy (`proxy`) | TLS und Weiterleitung an API bzw. Web | Standardports 80/443, keine Anwendungsdaten |
| Web (`web`) | Responsive Angular-Oberfläche und PWA | API-Aufruf durch Browser über dieselbe Herkunft |
| API (`api`) | Anmeldung, Berechtigungen, Fragen, Medien, Lernfunktionen und Update-Auftragsannahme | Datenbank sowie eng begrenzter Queue-Schreib- und Status-Lesezugriff; kein Docker-Socket |
| PostgreSQL (`db`) | Konten, private Inhalte und Bilddaten | Persistentes Volume, keine öffentlichen Datenbankports |
| Worker (`worker`) | Kontoinaktivität, Warnungen und geplante Arbeiten | Interne Datenbankverbindung, optional SMTP |
| Migration (`migrate`) | Schemaänderungen vor Inbetriebnahme | Einmaliger Job |
| Update-Operator (außerhalb von Compose) | Geprüftes Release als Rootless-Docker-Benutzer aktualisieren | Private Warteschlange/Status im gemeinsamen Host-Verzeichnis |

Der Webcontainer greift nicht selbst serverseitig auf die API zu; JavaScript im Browser verwendet den Reverse Proxy. Das Produktionsnetzwerk trennt `frontend` und `private`. API und Operator besitzen **keine gemeinsame Shell- oder Docker-Socket-Berechtigung**. Der Operator erfordert eine gesonderte Host-Einrichtung; PR #91 allein aktiviert keine produktive Installation. Siehe [Update-Architektur](../admin-web-updates.md), [Betrieb](../OPERATIONS.md) und [Datenbank](../DATABASE.md).
