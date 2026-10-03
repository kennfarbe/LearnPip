# Schwachstellenprüfung und Aktualisierung

Diese Prüfung dient der technischen Qualitätssicherung von LearnPip. Die vollständigen Ergebnisse dürfen im öffentlichen GitHub-Actions-Artefakt `dependency-security-reports` erscheinen (Aufbewahrung 14 Tage). **Der Workflow erstellt keine GitHub Security Advisories, beantragt keine CVE-Nummern und reicht keine Schwachstellen bei einer Meldestelle ein.** Solche formellen Prozesse erfolgen nur nach gesonderter Entscheidung des Projektverantwortlichen.

## Automatische Kontrollen

Der Workflow `Dependency and image security audit` läuft jeden Sonntag, manuell und bei relevanten PR-Änderungen. Er prüft direkte und transitive npm- und NuGet-Abhängigkeiten. Bei manuellem und planmäßigem Aufruf prüft er zudem die veröffentlichten Images api, worker und web nach aufgelöstem Digest mit Trivy. Scannerfehler gelten nicht als bestandener Scan.

Für Root und Angular werden jeweils `npm audit --json` und, bei Befunden, ein unverbindlicher `npm audit fix --package-lock-only --ignore-scripts` in einem temporären Verzeichnis ausgeführt. Die Pipeline verändert Paketdateien im Checkout nicht automatisch und verwendet kein `--force`. Auch bei Root-Befunden wird Angular geprüft; auch bei npm-Befunden wird NuGet geprüft. Die vollständigen JSON-Dateien (vor und gegebenenfalls nach Reparaturversuch sowie dessen Ausgabe und NuGet) werden für 14 Tage als GitHub-Actions-Artefakt hochgeladen, damit Paketnamen, Versionen und Abhängigkeitspfade tatsächlich untersucht werden können.

Befunde ab HIGH blockieren weiterhin den Merge. Vorgehen: zuerst sichere Paketupdates und passende Lockfile-Änderungen, danach vollständige Tests und erneute Scans. Besteht keine verfügbare sichere Aktualisierung, eine funktional geeignete Ersatzbibliothek prüfen. Ohne tragfähige Lösung konkrete Rückfrage an den Projektverantwortlichen richten und den PR offen lassen; keine Ausnahmen oder Deaktivierungen ohne ausdrückliche Entscheidung.

## Noch offene Abnahmekriterien

Dies ist ein erster technischer Schritt für #96/#97. Insbesondere fehlen eine vollständige Bestandsaufnahme älterer unterstützter Release-Digests, Release-Freigaberegeln und kontrollierte echte Scannerfehlertests. Die bereits vorhandene SBOM-Buildoption muss anhand verfügbarer Release-Artefakte überprüft werden. Eine CRA- oder NIS2-Konformität wird nicht behauptet.

## Befundaufnahme zu PR #124 (03.10.2026)

Der vollständige Root-npm-Bericht aus GitHub Actions 37138161911 enthält 32 HIGH, null CRITICAL und einen MODERATE-Befund. Das Angular-Projekt hat keine HIGH/CRITICAL-Befunde; NuGet meldet keine anfälligen Pakete. Der automatische Versuch ohne `--force` ändert die Root-Befunde nicht.

Besonders relevant: `semantic-release@25.0.9` enthält das npm-Publishing-Plugin als transitive Abhängigkeit, obwohl LearnPip es in `release.config.cjs` nicht als aktives Plugin benutzt. Diese Kette bringt das vollständige npm-CLI als Abhängigkeit mit. Daneben verwenden `semantic-release` und mehrere Plugins `micromatch`/`braces`. Für die gemeldete HIGH-Schwachstelle von `braces <=3.0.3` ist im GitHub-Advisory GHSA-vfj7-8cjw-p6xm bislang keine gepatchte Version ausgewiesen. npm schlägt pauschal ein Major-Downgrade von `@semantic-release/changelog` auf 5.0.1 vor; das wurde nicht übernommen, weil es weder als sicherer gezielter Fix noch als kompatibel nachgewiesen wurde.

**Entscheidung noch offen:** Die derzeitige stabile Semantic-Release-Kette ist nicht allein durch sichere Paketupdates vollständig bereinigt. Ein Austausch des Release-Orchestrators muss alle bestehenden Abläufe (Conventional Commits, automatische Versionsnummer, `CHANGELOG.md`, Git-Tag, GitHub-Release und zugehörige Docker-Image-Tags) mit Integrationstests nachweisen. Keine Audit-Ausnahmen und kein stillschweigendes Downgrade. PR bleibt bis zu einer bestätigten Lösung offen.
