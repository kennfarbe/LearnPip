# Architektur

## C4-Sichten

- [C1: Systemkontext](C1-system-context.md) – Menschen, LearnPip und optionale externe Dienste.
- [C2: Container](C2-containers.md) – Laufzeitteile und ihre Verbindungen.

Die Diagramme beschreiben den implementierten Stand und werden bei Änderungen
an Laufzeitteilen oder externen Beziehungen aktualisiert.

## Architekturentscheidungen

Die Architektur ist in kurzen, ergänzbaren ADRs dokumentiert. Eine ADR beschreibt den Hintergrund, die Entscheidung und deren Auswirkungen. Ersetzte Entscheidungen werden als abgelöst markiert statt stillschweigend überschrieben.

- [ADR 0001: Monorepo und Laufzeitplattform](adr/0001-monorepo-and-runtime.md)
- [ADR 0002: PostgreSQL und private Inhalte](adr/0002-postgresql-and-private-content.md)

## Laufzeitübersicht

Die Installation besteht aus einem Reverse Proxy, einer ASP.NET-Core-API,
einem separaten Hintergrunddienst, einem Angular-Webclient und PostgreSQL.
Die Komponenten laufen als Docker-Compose-Stack. Die API und Weboberfläche
haben unabhängige Build-Jobs in CI. In Produktion ist die Datenbank nur im
internen Compose-Netzwerk erreichbar; lokal bindet ihr Entwicklungsport an
`127.0.0.1`.
