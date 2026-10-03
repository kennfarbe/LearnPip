# Schwachstellenprüfung und Aktualisierung

Diese Prüfung dient der technischen Qualitätssicherung von LearnPip. Die vollständigen Ergebnisse dürfen im öffentlichen GitHub-Actions-Artefakt `dependency-security-reports` erscheinen (Aufbewahrung 14 Tage). **Der Workflow erstellt keine GitHub Security Advisories, beantragt keine CVE-Nummern und reicht keine Schwachstellen bei einer Meldestelle ein.** Solche formellen Prozesse erfolgen nur nach gesonderter Entscheidung des Projektverantwortlichen.

## Automatische Kontrollen

Der Workflow `Dependency and image security audit` läuft jeden Sonntag, manuell und bei relevanten PR-Änderungen. Er prüft direkte und transitive npm- und NuGet-Abhängigkeiten. Bei manuellem und planmäßigem Aufruf prüft er zudem die veröffentlichten Images api, worker und web nach aufgelöstem Digest mit Trivy. Scannerfehler gelten nicht als bestandener Scan.

Für Root und Angular werden jeweils `npm audit --json` und, bei Befunden, ein unverbindlicher `npm audit fix --package-lock-only --ignore-scripts` in einem temporären Verzeichnis ausgeführt. Die Pipeline verändert Paketdateien im Checkout nicht automatisch und verwendet kein `--force`. Auch bei Root-Befunden wird Angular geprüft; auch bei npm-Befunden wird NuGet geprüft. Die vollständigen JSON-Dateien (vor und gegebenenfalls nach Reparaturversuch sowie dessen Ausgabe und NuGet) werden für 14 Tage als GitHub-Actions-Artefakt hochgeladen, damit Paketnamen, Versionen und Abhängigkeitspfade tatsächlich untersucht werden können.

Befunde ab HIGH blockieren weiterhin den Merge. Vorgehen: zuerst sichere Paketupdates und passende Lockfile-Änderungen, danach vollständige Tests und erneute Scans. Besteht keine verfügbare sichere Aktualisierung, eine funktional geeignete Ersatzbibliothek prüfen. Ohne tragfähige Lösung konkrete Rückfrage an den Projektverantwortlichen richten und den PR offen lassen; keine Ausnahmen oder Deaktivierungen ohne ausdrückliche Entscheidung.

## Noch offene Abnahmekriterien

Dies ist ein erster technischer Schritt für #96/#97. Insbesondere fehlen eine vollständige Bestandsaufnahme älterer unterstützter Release-Digests, Release-Freigaberegeln und kontrollierte echte Scannerfehlertests. Die bereits vorhandene SBOM-Buildoption muss anhand verfügbarer Release-Artefakte überprüft werden. Eine CRA- oder NIS2-Konformität wird nicht behauptet.
