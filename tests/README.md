# Tests und Qualitätssicherung

Die Projekte enthalten automatisierte Backend-, Web- und Betriebstests. Tests dürfen keine produktive oder gemeinsam genutzte Datenbank verändern. Die CI verwendet für Backend-Integrationstests eine eigene PostgreSQL-Testinstanz.

## Backend

Die .NET-10-Solution wird im Release-Modus getestet. Die zentralen Einstellungen in [`Directory.Build.props`](../Directory.Build.props) aktivieren die SDK-Analyzer, StyleCop mit deutscher Dokumentationskultur (`stylecop.json`) und `SonarAnalyzer.CSharp`. `TreatWarningsAsErrors` ist aktiviert. Die bestehenden, dokumentierten Ausnahmen in [`.editorconfig`](../.editorconfig) bilden den bislang noch nicht bereinigten Altbestand ab und müssen bei einer späteren Aufräumaktion schrittweise reduziert werden.

## Frontend und Markdown

Der Web-CI-Job verwendet `npm ci`, `npm run format:check`, `npm run lint`, `npm run test:pwa` und den Angular-Build. Warnungen im ESLint-Lauf führen zum Fehlschlag; Angular-Build-Warnungen werden ebenfalls als Fehler bewertet.

Ein eigener Markdown-CI-Job verwendet Markdownlint und CSpell mit `de-DE` für die deutsche Projekt-Einführung. Der automatisch von Semantic Release erzeugte `CHANGELOG.md` wird von der manuellen Markdown-Prüfung ausgenommen. Weitere Tests prüfen Installationsablauf, Sicherung und isolierte Wiederherstellung.
