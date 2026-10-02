# Prüfung der Drittanbieter-Lizenzen

Die Bestandsaufnahme vom **29. September 2026** umfasste direkte Abhängigkeiten, das damalige vollständige npm-Lockfile mit 338 Paketen, Docker-Images und Repository-Ressourcen (LP-22). Sie ergab für den damals geprüften Verwendungszweck keine zwingende proprietäre Bezahl-Lizenz. Bei der Weitergabe gebauter Artefakte müssen anwendbare Lizenz- und Urheberrechtshinweise erhalten bleiben.

**Aktualisierung vom 2. Oktober 2026:** In PR #91 wurden unter anderem Angular ESLint, .NET-/StyleCop-/Sonar-Analyzer sowie Markdownlint und CSpell für die CI ergänzt. Daher ist die historische Paketanzahl **keine aktuelle Vollinventur** mehr. Vor dem nächsten produktiven Release ist die vollständige Lizenzaufnahme aus den aktuellen Lockfiles, .NET-Paketversionen und Images erneut zu erzeugen und zu prüfen; eine abgeschlossene neue Vollprüfung wird hier nicht behauptet.

| Komponente | Bisher geprüfte Lizenz beziehungsweise Prüfhinweis | Quelle |
| --- | --- | --- |
| .NET 10, ASP.NET Core, EF Core, Microsoft Extensions und OpenAPI | MIT; Hinweise bei Weitergabe bewahren | [Offizielle Lizenzangaben](https://github.com/dotnet/core/blob/main/license-information.md) |
| Npgsql und PostgreSQL 18 | Jeweilige PostgreSQL-Lizenz; Hinweise erhalten | [Npgsql](https://github.com/npgsql/npgsql/blob/main/Directory.Build.props), [PostgreSQL](https://www.postgresql.org/about/licence/) |
| SkiaSharp und native Linux-Komponenten | MIT sowie Hinweise gebündelter nativer Abhängigkeiten | [SkiaSharp](https://github.com/mono/SkiaSharp/blob/main/LICENSE.md) |
| Angular 22 und zugehörige Build-Werkzeuge | Lizenzen anhand der jeweils tatsächlich installierten Pakete prüfen | [Angular](https://github.com/angular/angular/blob/main/LICENSE) |
| RxJS und TypeScript | Apache-2.0; Hinweise erhalten | [RxJS](https://github.com/ReactiveX/rxjs/blob/master/LICENSE.txt), [TypeScript](https://github.com/microsoft/TypeScript/blob/main/LICENSE.txt) |
| Caddy und offizielles Nginx-Image | Jeweilige Lizenz- und Image-Hinweise berücksichtigen | [Caddy](https://github.com/caddyserver/caddy/blob/master/LICENSE), [Nginx](https://github.com/nginx/nginx/blob/master/LICENSE) |
| xUnit, .NET-Testwerkzeuge und zusätzliche Analyzer | Entwicklungs-/Testabhängigkeiten; aktuelle Paketlizenzen prüfen | Projektdateien und NuGet-Metadaten |
| ESLint, Angular ESLint, Prettier, Markdownlint, CSpell und Wörterbücher | Aktuelle Abhängigkeiten einschließlich Wörterbuchrechten anhand der npm-Metadaten prüfen | `frontend/web/package-lock.json` und CI-Workflow |

Das frühere Lockfile enthielt unter anderem Varianten von Lightning CSS und Browserkompatibilitätsdaten. Die tatsächlichen Lizenzhinweise und gegebenenfalls Pflichten zur Quellenbereitstellung sind bei einer Weitergabe anhand der **aktuellen** Paketversionen festzustellen. Das Lockfile hält die aufgelösten Namen und Versionen fest.

LearnPip-Programmcode unterliegt gesondert der AGPL-3.0-only, siehe [Lizenzübersicht](LICENSING.md). Rechte an hochgeladenen Bildern und öffentlich eingereichten Aufgaben ergeben sich nicht aus dieser Abhängigkeitsliste und müssen separat geprüft werden.
