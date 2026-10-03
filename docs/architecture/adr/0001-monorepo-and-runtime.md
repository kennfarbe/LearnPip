# ADR 0001: Monorepo und Laufzeitplattform

- Status: Angenommen
- Datum: 2026-09-29

## Kontext

LearnPip soll als selbst betreibbare Anwendung mit API, Hintergrunddienst und mobil nutzbarer Weboberfläche entstehen. Die Teile müssen lokal leicht startbar sein und getrennt gebaut werden können.

## Entscheidung

- Ein gemeinsames Git-Repository enthält Backend, Webclient, Deployment-Dateien, Dokumentation und Tests.
- API und Worker verwenden .NET 10; die Oberfläche verwendet Angular 22 und Node.js 24.
- ASP.NET Core stellt eine versionierbare HTTP-API bereit. Der Worker läuft als eigener Dienst, damit geplante Hintergrundaufgaben später unabhängig skaliert werden können.
- Docker Compose startet lokal den Stack; CI baut und testet Backend und Web getrennt.

## Folgen

- Gemeinsame Pull Requests können API-Verträge und Oberfläche zusammen ändern; getrennte CI-Jobs zeigen Fehler pro Komponente.
- Aktualisierungen von Angular/.NET benötigen passende Runtime- und CI-Versionen. Dependabot und reguläre Wartung halten die Versionen aktuell.
- Der Worker übernimmt inzwischen den Kontolebenszyklus und die dazugehörigen Warnungen.
