# Architekturentscheidungen

Die Architektur ist in kurzen, ergänzbaren ADRs dokumentiert. Eine ADR beschreibt den Hintergrund, die Entscheidung und deren Auswirkungen. Ersetzte Entscheidungen werden als abgelöst markiert statt stillschweigend überschrieben.

- [ADR 0001: Monorepo und Laufzeitplattform](adr/0001-monorepo-and-runtime.md)
- [ADR 0002: PostgreSQL und private Inhalte](adr/0002-postgresql-and-private-content.md)

## Laufzeitübersicht

Die erste Installation besteht aus einer ASP.NET-Core-API, einem separaten Hintergrunddienst, einem Angular-Webclient und PostgreSQL. Die Komponenten laufen lokal als Docker-Compose-Stack. Die API und Weboberfläche haben unabhängige Build-Jobs in CI. Die Datenbank ist im Compose-Netzwerk erreichbar, aber nicht auf einen öffentlichen Host-Port gebunden.
