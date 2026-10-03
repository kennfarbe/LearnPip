# Architektur

Stand: 2. Oktober 2026. Die Darstellungen unterscheiden den vorhandenen Laufzeit-Stack und die im offenen PR #91 ergänzte Update-Integration von noch offenen Betriebsentscheidungen.

## C4-Sichten

- [C1: Systemkontext](C1-system-context.md) – Nutzende, Betrieb und externe Dienste.
- [C2: Container und Update-Operator](C2-containers.md) – Laufzeitteile, Datenspeicher und gesicherte Update-Warteschlange.

## Architekturentscheidungen

Die Architektur wird durch ergänzbare ADRs dokumentiert. Eine ADR enthält Kontext, Entscheidung und Folgen; abgelöste Entscheidungen bleiben nachvollziehbar gekennzeichnet.

- [ADR 0001: Monorepo und Laufzeitplattform](adr/0001-monorepo-and-runtime.md)
- [ADR 0002: PostgreSQL und private Inhalte](adr/0002-postgresql-and-private-content.md)

## Laufzeit und Qualitätssicherung

LearnPip verwendet Caddy, eine ASP.NET-Core-API (.NET 10), einen getrennten Worker, einen Angular-22-Webclient und PostgreSQL 18. Private Bilddaten werden derzeit ebenfalls in PostgreSQL gespeichert. In Produktion ist die Datenbank nur über das interne Compose-Netz erreichbar. Die lokale Entwicklungsdatenbank bindet an `127.0.0.1`.

Die CI prüft .NET mit SDK-, StyleCop- und Sonar-Analyzern, Angular mit ESLint und Prettier, Markdown mit Markdownlint/CSpell sowie die Installer- und Restore-Abläufe. Die bekannten Ausnahmen für bestehenden .NET-Code stehen in [`.editorconfig`](../../.editorconfig); neue Warnungen werden grundsätzlich als Fehler behandelt.
