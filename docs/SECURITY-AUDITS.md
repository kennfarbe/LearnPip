# Private Schwachstellenprüfung – Betriebs- und Umsetzungsstand

Diese Anleitung ergänzt [SECURITY.md](../SECURITY.md) und Issue #96. **Keine Sicherheitsbefunde oder CVE-Details in öffentlichen Issues, Pull Requests, CI-Protokollen oder Artefakten veröffentlichen.**

## Automatische Kontrollen

Der zusätzliche Workflow Private security audit prüft jeden Sonntag und auf manuelle Auslösung sowie bei relevanten PR-Änderungen die direkten und transitiven npm- und NuGet-Abhängigkeiten. Die planmäßige und manuelle Ausführung ermittelt den letzten stabilen GitHub-Release-Tag und prüft die drei veröffentlichten Docker-Komponenten api, worker und web jeweils nach tatsächlichem aufgelösten Image-Digest mit Trivy. Ein nicht zugängliches Image, Netzwerk-/Scannerfehler oder unlesbarer Bericht gilt ausdrücklich **nicht** als erfolgreiche Prüfung.

Die Scanner verwenden festgelegte Action-SHAs und Scanner-Versionen, minimale Repository-Leserechte und keine Projekt-Secrets. Bei Abhängigkeits-/Image-Befunden ab HIGH schlägt die Prüfung fehl. Der Report-Parser gibt ausschließlich einen aggregierten Zustand aus; rohe Berichte verbleiben auf dem kurzlebigen Runner und werden nicht hochgeladen. Er belegt nicht, dass die zugrunde liegenden Advisories dauerhaft privat sind. GitHub-Actions-Logs des öffentlichen Repositories sind grundsätzlich ein öffentlicher Bereich und dürfen daher keine Schwachstellendetails enthalten.

## Vertrauliche Nachprüfung durch Projektverantwortliche

Nach einem roten Lauf auf einem vertrauenswürdigen, lokal geschützten System und ohne öffentliche Terminal-Mitschnitte durchführen:

- npm im Repository-Wurzelverzeichnis und im Verzeichnis frontend/web: npm audit --package-lock-only --json
- NuGet nach dotnet restore backend/LearnPip.sln: dotnet list backend/LearnPip.sln package --vulnerable --include-transitive --format json
- Image-Scan: den im Workflow aufgelösten veröffentlichten Digest verwenden und lokal mit derselben Trivy-Version und aktuellen Advisory-Daten prüfen; alle drei Komponenten getrennt.
- Relevanz anhand tatsächlich ausgelieferter Version, Ausnutzbarkeit, Abhängigkeitspfad und verfügbarer Behebung feststellen. Sichere Updates oder Ersatzkomponenten separat testen und mit einem erneuten Scan bestätigen.
- Interne Befunde nur im geschützten Verantwortlichenkanal dokumentieren. Für externe Meldungen privaten Vulnerability-Reporting-Weg aus SECURITY.md nutzen; keine automatische CVE-Veröffentlichung. Keine Befundunterdrückung oder Scanner-Ausnahme ohne vorherige ausdrückliche Entscheidung des Projektverantwortlichen.

## Noch offene Abnahmekriterien

Dieser Workflow ist **ein erster technischer Schritt**, kein abgeschlossener Nachweis für #96/#97. Insbesondere fehlen noch vollständige Inventarisierung und Scans **aller** unterstützten älteren Release-Digests, eine dauerhafte datengeschützte Ablage von Scanberichten, definierte Triage-/SLA-Verantwortung und Release-Freigaberegeln, ein kontrollierter Beispielbefund-/Scannerfehler-Test auf dem echten Runner sowie der sichere Patchprozess. Die bereits aktivierte SBOM-Option bei Docker-Builds muss zudem auf tatsächlich verfügbare Release-Artefakte und Komponentenabdeckung überprüft werden. Keine behauptete CRA- oder NIS2-Konformität.

Stand der Erstfassung: 03.10.2026.
