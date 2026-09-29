# C1 – Systemkontext

Stand: 29. September 2026 · [C2 – Container](C2-containers.md)

LearnPip ist eine selbst betreibbare Anwendung für private Fragen und kurze
Lernsitzungen. Die C1-Sicht zeigt die Systemgrenze einer Installation und ihre
Beziehungen zu Menschen und externen Diensten.

```mermaid
flowchart LR
    learner["Lernende Person"]
    operator["Betreiberin oder Betreiber"]
    subgraph boundary["Eigene Betriebsumgebung"]
        learnpip["LearnPip · Webanwendung und API"]
    end
    oidc["Externer Identitätsanbieter · optional"]
    smtp["SMTP-Server · optional"]

    learner -->|"Fragen erstellen und lernen · HTTPS"| learnpip
    operator -->|"Installieren und betreiben · Docker Compose"| learnpip
    learnpip <-->|"Anmeldung · OpenID Connect über HTTPS"| oidc
    learnpip -->|"Anmeldecode per E-Mail · SMTP"| smtp
```

| Gegenüber | Beziehung zu LearnPip |
| --- | --- |
| Lernende Person | Nutzt die Weboberfläche für private Entwürfe, Fragen und kurze Lernsitzungen. |
| Betreiberin oder Betreiber | Installiert und aktualisiert die Instanz und konfiguriert Datenbank, Geheimnisse und optionale Dienste. |
| Externer Identitätsanbieter | Nur bei konfiguriertem OIDC: Anmeldung und Rückruf an die API. |
| SMTP-Server | Nur bei konfiguriertem Mailtransport: Die API versendet Anmeldecodes. |

**Systemgrenze:** Weboberfläche, API, Worker, Proxy und Datenbank gehören zur
LearnPip-Installation und erscheinen hier als ein System. Die Datenbank ist
kein externer Dienst dieser Sicht. OIDC und SMTP sind optional.

**Umfang:** Die Sicht beschreibt den implementierten Stand. Geplante native
Apps, öffentliche Fragenpools und externe KI-Dienste sind keine gegenwärtigen
Systembeziehungen. Siehe [API](../API.md), [Identität](../IDENTITY.md) und
[Betrieb](../OPERATIONS.md).
