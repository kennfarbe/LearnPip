# Schwachstellenprüfung und Aktualisierung

Diese Prüfung dient der technischen Qualitätssicherung von LearnPip. Die vollständigen Ergebnisse dürfen im öffentlichen GitHub-Actions-Artefakten `dependency-security-reports-*` erscheinen (Aufbewahrung 14 Tage). **Der Workflow erstellt keine GitHub Security Advisories, beantragt keine CVE-Nummern und reicht keine Schwachstellen bei einer Meldestelle ein.** Solche formellen Prozesse erfolgen nur nach gesonderter Entscheidung des Projektverantwortlichen.

## Automatische Kontrollen

Der Workflow `Dependency and image security audit` läuft jeden Sonntag, manuell und bei relevanten PR-Änderungen. Er prüft direkte und transitive npm- und NuGet-Abhängigkeiten. Auch bei relevanten PRs prüft er unterstützte veröffentlichte Versionen und deren Laufzeit- sowie Build-Basisimages nach aufgelöstem Digest mit Trivy. Scannerfehler gelten nicht als bestandener Scan.

Für Root und Angular werden jeweils ein **verbindlicher Produktions-Audit** (`npm audit --omit=dev --json`) und ein **informativer Gesamtaudit** (`npm audit --json`, einschließlich Dev-Abhängigkeiten) ausgeführt. Ausschließlich HIGH/CRITICAL-Befunde im Produktions-Abhängigkeitsgraphen verhindern den Merge. Ergebnisse aus dem Gesamtaudit bleiben sichtbar, dürfen die Pipeline aber nicht allein aufgrund der Dev-Befunde blockieren. Fehlerhafte, fehlende oder nicht ausführbare Scans bleiben in beiden Modi blockierend. Bei Produktionsbefunden wird ein unverbindlicher `npm audit fix --package-lock-only --ignore-scripts` in einem temporären Verzeichnis ausgeführt. Die Pipeline verändert Paketdateien im Checkout nicht automatisch und verwendet kein `--force`. Auch bei Root-Befunden wird Angular geprüft; auch bei npm-Befunden wird NuGet geprüft. Die vollständigen JSON-Dateien (vor und gegebenenfalls nach Reparaturversuch sowie dessen Ausgabe und NuGet) werden für 14 Tage als GitHub-Actions-Artefakt hochgeladen, damit Paketnamen, Versionen und Abhängigkeitspfade tatsächlich untersucht werden können.

Befunde ab HIGH im Produktionsgraphen sowie fehlgeschlagene Scans blockieren weiterhin den Merge. Dev-Befunde allein blockieren nicht; der wöchentliche Gesamtaudit und die hochgeladenen Berichte ermöglichen wiederkehrende Update-Prüfungen. Vorgehen: zuerst sichere Paketupdates und passende Lockfile-Änderungen, danach vollständige Tests und erneute Scans. Besteht keine verfügbare sichere Aktualisierung, eine funktional geeignete Ersatzbibliothek prüfen. Ohne tragfähige Lösung konkrete Rückfrage an den Projektverantwortlichen richten und den PR offen lassen; keine Ausnahmen oder Deaktivierungen ohne ausdrückliche Entscheidung.

## Unterstützte Versionen und konkrete Images

`security/supported-releases.json` ist das verbindliche Supportinventar. In dieser frühen Projektphase wird ausschließlich das jeweils neueste stabile Release unterstützt (`latest`); ältere Releases erhalten keine zugesicherte Pflege. Betreiber sollen auf das aktuelle stabile Release aktualisieren. Wird eine ältere Linie weiter gepflegt, muss ihr konkretes `vX.Y.Z`-Tag hier zusätzlich aufgenommen werden; die Tests prüfen auch mehrere unterstützte Releases. Das ist eine technische Supportentscheidung, keine Aussage über gesetzlich erforderliche Supportdauer (#97).

Der wöchentliche, manuelle und relevante PR-Lauf löst jedes unterstützte Release auf einen Commit auf und prüft dessen NuGet-/npm-Graph zusätzlich zum aktuellen main. API-, Worker- und Web-Images, PostgreSQL und Caddy sowie die Build-Basisimages für .NET SDK und Node werden für `linux/amd64` und `linux/arm64` inventarisiert und ausschließlich über konkrete Plattform-Digests gescannt. Fehlende Releases, Plattformen oder Digests blockieren die Prüfung. Zeit, Umfang und Digests liegen im Artefakt `security-inventory` (14 Tage). Trivy ist auf Version `0.74.0`, Actions auf unveränderliche Commit-SHAs, Node auf `24.19.0` und .NET auf `10.0.401` gebunden. Trivy muss eine höchstens 72 Stunden alte Advisory-Datenbank mit Download innerhalb von 24 Stunden nachweisen; fehlende Zeitnachweise sind unvollständig. npm/NuGet fragen bei jedem Lauf die Advisory-Quellen ab; Netzwerk-/Restore-/Scannerfehler bleiben blockierend.

## Befundverwaltung und Freigabe

Das Artefakt `security-ledger` führt Befunde anhand Scanner, Ziel, Paket, betroffener Version und Advisory zusammen. Das logische Ziel bleibt bei einem Digestwechsel desselben Release-Dienstes stabil; der tatsächlich geprüfte Digest wird separat als `source_ref` festgehalten. Es enthält Erst-/Letztfund, Schwere, verfügbare Behebung, Zuständigkeit (`kennfarbe`), Bearbeitungsziel und den ausdrücklich noch manuell zu bewertenden Ausnutzbarkeitsstatus. Wiederholungen erzeugen keine neuen Tickets. Der nächste planmäßige/manuelle Lauf liest den letzten verfügbaren Ledger von main; fehlender historischer Bestand wird ausdrücklich als neue Baseline gekennzeichnet. Die Aufbewahrung beträgt 14 Tage; für längere Betriebsunterbrechungen muss der Projektverantwortliche den Ledger vorher geschützt sichern. Ein Befund wird erst nach vollständigem Scan desselben Ziels ohne diesen Befund als verifiziert behoben markiert. Scannerfehler und veraltete Daten schließen keine Funde.

Der Projektverantwortliche bewertet Ausnutzbarkeit und Auswirkungen: CRITICAL innerhalb eines Tages, HIGH innerhalb sieben Tagen, MEDIUM/MODERATE innerhalb 30 Tagen, übrige innerhalb 90 Tagen triagieren und Maßnahmen festlegen. Das sind interne Bearbeitungsziele, keine gesetzlichen Meldefristen. Bereits bestehende Funde werden beim ersten vollständigen Scan aufgenommen; es gibt keine automatische Bestandsschutz-Ausnahme. Das vereinbarte informative npm-Dev-Verhalten bleibt erhalten. Ausnahmen benötigen vorherige Zustimmung, Begründung und gegebenenfalls Ablaufdatum; der Workflow führt keine Unterdrückungen ein.

Vor Semantic Release muss der wiederverwendbare Sicherheitsworkflow erfolgreich sein. Er prüft Abhängigkeiten und frisch gebaute Laufzeit-Images einschließlich der Compose-Basisdienste und Build-Basisimages; HIGH/CRITICAL-Produktionsfunde oder unvollständige Prüfungen verhindern Release und Image-Publishing. Veröffentlichte Images werden zusätzlich zyklisch erneut geprüft. Keine automatischen Paketupdates, CVE-Veröffentlichungen, Security Advisories oder öffentlichen Exploit-Tickets. Noch nicht veröffentlichte Lücken über den vertraulichen Weg aus SECURITY.md behandeln; Audit-Artefakte nur im vereinbarten Untersuchungsumfang verwenden, niemals Secrets oder private Betriebsdaten beifügen.

## Nachgewiesene Scannerkontrollen

`security-scanner-selftest.py` erstellt in einem temporären Verzeichnis einen ausschließlich synthetischen Lockfile-Graph mit einer bekannten verwundbaren Bibliotheksversion, fragt den echten npm-Auditdienst ab und verlangt einen relevanten Befund. Paketcode wird weder installiert noch ausgeführt. Anschließend erzwingt eine reservierte ungültige Registry-Adresse einen echten Scannerfehler; dieser darf nicht als sauberer Scan gelten. Die CI testet zusätzlich Digest-/Supportfehler, Ledger-Deduplizierung, Wiederholungsprüfung und veraltete Advisory-Daten. Die Kontrollen sind technische Nachweise zu #96; SBOM-/CRA-/NIS2-Organisationspflichten aus #97/#98 bleiben separate Aufgaben.

## Befundaufnahme zu PR #124 (03.10.2026)

Der vollständige Root-npm-Bericht aus GitHub Actions 37138161911 enthält 32 HIGH, null CRITICAL und einen MODERATE-Befund. Das Angular-Projekt hat keine HIGH/CRITICAL-Befunde; NuGet meldet keine anfälligen Pakete. Der automatische Versuch ohne `--force` ändert die Root-Befunde nicht.

Besonders relevant: `semantic-release@25.0.9` enthält das npm-Publishing-Plugin als transitive Abhängigkeit, obwohl LearnPip es in `release.config.cjs` nicht als aktives Plugin benutzt. Diese Kette bringt das vollständige npm-CLI als Abhängigkeit mit. Daneben verwenden `semantic-release` und mehrere Plugins `micromatch`/`braces`. Für die gemeldete HIGH-Schwachstelle von `braces <=3.0.3` ist im GitHub-Advisory GHSA-vfj7-8cjw-p6xm bislang keine gepatchte Version ausgewiesen. npm schlägt pauschal ein Major-Downgrade von `@semantic-release/changelog` auf 5.0.1 vor; das wurde nicht übernommen, weil es weder als sicherer gezielter Fix noch als kompatibel nachgewiesen wurde.

**Entscheidung:** Semantic Release bleibt bestehen. Die derzeitigen reinen Entwicklungsbefunde werden regelmäßig erfasst, blockieren jedoch nicht die Freigabe, sofern sämtliche Produktions-, NuGet- und gegebenenfalls Image-Prüfungen erfolgreich sind. Keine pauschale Deaktivierung von Audits und kein stillschweigendes Downgrade.

## Korrektur der Image-Befunde in PR #143

Die Web-Basis wird auf nginx `1.30.5-alpine3.24` und Node `24.21.0-alpine3.24` aktualisiert; für libexpat und pcre2 werden die ausgewiesenen behobenen Mindestversionen aus derselben Alpine-Linie installiert. Inventarabfragen wiederholen vorübergehende Registry-/Netzwerkfehler begrenzt und bleiben bei endgültigem Fehler unvollständig. Das Datenbankimage bleibt bei PostgreSQL 18 (`18.6-alpine3.24`); der unveränderte gosu-Quellcode aus Commit `6456aaa0f3c854d199d0f037f068eb97515b7513` wird mit Go `1.26.9` neu gebaut. Das beseitigt die veraltete eingebettete Go-Standardbibliothek, ohne PostgreSQL-Datenformat oder Berechtigungswechsel zu ersetzen. Das Datenbankimage wird pro Release zusammen mit API, Worker und Web veröffentlicht. Integration und Wiederherstellungsprüfung verwenden dasselbe Dockerfile; die Sicherung erfasst den tatsächlich konfigurierten Imagenamen. Die Go-Build-Basis wird ebenfalls gescannt.

Der überprüfte aktuelle npm-CLI-Graph (`12.2.0`) enthält weiterhin relevante Befunde, darunter einen ohne ausgewiesene Behebung. Ein pauschales Downgrade oder eine Unterdrückung wird nicht vorgenommen. Die Freigabesperre bleibt bestehen, bis eine tragfähige Lösung oder eine ausdrücklich genehmigte, dokumentierte Ausnahme vorliegt. Befunde in bereits veröffentlichten Digests bleiben im Register erhalten; ein neuer Build verändert diese Digests nicht. Ein vollständiger Scan mit Befunden wird in der Workflow-Anzeige von einem unvollständigen Scan unterschieden.

## Isolation der Abhängigkeitsprüfung

Unterstützte Quellen werden als größenbegrenztes Datenarchiv abgerufen; Pfadtraversal, Links und Spezialdateien werden abgewiesen. Bei PRs wird der tatsächliche PR-Commit zusätzlich zu den unterstützten Releases geprüft. npm- und NuGet-Prüfungen laufen in getrennten, kurzlebigen Containern mit schreibgeschütztem Dateisystem, unprivilegiertem Benutzer und ohne zusätzliche Linux-Capabilities. Quellen und vertrauenswürdige Prüfskripte sind ausschließlich lesbar eingebunden. Der Container erhält weder GitHub-/Runner-/Cache-Zugangsdaten noch Docker-Socket oder Runner-Verzeichnisse; Schreibzugriff besteht ausschließlich auf seinen Berichtordner und den flüchtigen Arbeitsbereich. Damit bleiben auch ausführbare MSBuild-Restore-Schritte vom privilegierten Runner getrennt.

Der Runner übernimmt ausschließlich erwartete, größenbegrenzte reguläre Berichtdateien. Symbolische Links und Spezialdateien aus dem Container werden abgewiesen, bevor Berichte gelesen oder hochgeladen werden. Automatisches npm-Caching im verbliebenen Node-Setup ist ausdrücklich deaktiviert. Scannerfehler und fehlende Berichte bleiben blockierend; die Isolation ändert keine Befund- oder Freigaberegel.

Die aggregierte Auswertung der npm-, NuGet- und Trivy-Berichte erfolgt mit `scripts/security-report.mjs` unter Node.js ohne zusätzliche npm-Abhängigkeiten. Exitcode 0 bedeutet keine HIGH-/CRITICAL-Befunde, 1 bedeutet entsprechende Befunde und 2 einen unvollständigen oder ungültigen Bericht. Die Ausgabe enthält keine einzelnen Schwachstellenkennungen. Andere Hilfsskripte für Inventar, Befundregister und sichere Dateiübernahme verwenden weiterhin Python.

## Bereits veröffentlichte Images

Vom Projektverantwortlichen am 4. Oktober 2026 freigegeben: Bei Pull Requests werden Befunde in bereits veröffentlichten, unveränderlichen Image-Digests vollständig im Scanbericht und Befundregister dokumentiert. Diese Befunde verhindern nicht die Prüfung und Veröffentlichung einer korrigierten Folgeversion. Sie bleiben offen, behalten Verantwortlichen und Behebungsfrist und werden nicht allein durch diese Regel als behoben markiert. Planmäßige und manuelle Prüfungen blockieren weiterhin bei HIGH-/CRITICAL-Befunden in diesen Images.

Ungültige oder unvollständige Scans, fehlende Ziele und veraltete Datenbanken bleiben auch bei Pull Requests blockierend. Neue Release-Kandidaten einschließlich sämtlicher Build-Images bleiben strikt gesperrt, solange HIGH-/CRITICAL-Befunde vorliegen. Für npm-Bibliotheken im Node-Build-Werkzeug ist keine Ausnahme freigegeben.

## Korrigiertes Node-Build-Werkzeug

Der Web-Build ersetzt den mit Node gebündelten npm-Paketmanager durch pnpm 12.9.1. npm und seine Bibliotheken werden aus der verwendeten Werkzeugstufe entfernt; die Prüfung wird nicht ausgenommen. pnpm importiert die eingecheckte `package-lock.json` und installiert anschließend mit unveränderlicher pnpm-Lockdatei ohne Installationsskripte. Die npm-Lockdatei bleibt für die vorhandenen Entwickler- und Audit-Abläufe maßgeblich. Der Angular-Build wird mit pnpm ausgeführt.

Der Kandidaten-Audit prüft die tatsächlich verwendete Docker-Stufe `node-toolchain` für AMD64 und ARM64. Diese Stufe wird mit jedem Release zusätzlich als `build-node-vX.Y.Z` und `build-node-latest` veröffentlicht, einschließlich SBOM. Das Inventar neuer Releases prüft deren konkrete Plattform-Digests. Alte Releases behalten ihre bisherigen Inventarziele.

## Aktualisierung der Container vom 9. Oktober 2026

Der vollständige Kandidatenscan zu PR #176 meldet veraltetes `tiff` im
Web-Laufzeitimage und Go 1.26.8 im Go-Buildimage sowie den eingebetteten
Standardbibliotheken von gosu und Caddy. Die ausgewiesenen behobenen Stände
werden verwendet: `tiff >= 4.7.2-r0` aus derselben Alpine-Linie und Go 1.26.9.
Die Go-Basis in Datenbankbuild und Kandidatenaudit bleibt identisch. Grundlage:
[offizielle Go-Releasehistorie](https://go.dev/doc/devel/release).

`deploy/caddy.Dockerfile` baut die unveränderten Standardmodule von Caddy 2.11.7
mit Go 1.26.9 neu. Ein separates Go-Buildmodul erhält die konkrete Caddy-Version
in den Binär- und SBOM-Metadaten. Das offizielle Runtime-Image bleibt die Basis;
sein Caddy-Binary wird durch den Neubau ersetzt. Lizenzhinweise der Basis bleiben
erhalten. Auch die transitiven Module bleiben im vollständigen Scan erfasst.

Das korrigierte Proxy-Image wird wie API, Worker, Web und Datenbank pro Release
für AMD64 und ARM64 als `kennfarbe/learnpip:proxy-vX.Y.Z` mit SBOM veröffentlicht.
Compose installiert genau das zur gewählten LearnPip-Version gehörende Image.
Der Kandidatenaudit baut dasselbe Dockerfile; das Inventar veröffentlichter
Versionen liest wie bisher die damalige Compose-Konfiguration und deren
unveränderliche Plattform-Digests. Ältere Releases behalten ihre bisherigen
Inventarziele. Der CI-Smoke prüft beide Caddy-Konfigurationen und den tatsächlichen
Start samt Health-Endpunkt ohne externe Netzwerkverbindung.

Keine Befundschwelle, Scanpflicht oder Freigabesperre wird gelockert. Die Behebung
ist erst durch erfolgreiche Neubauten und vollständige Kandidatenscans für beide
Architekturen nachgewiesen; alte veröffentlichte Digests werden nicht umgeschrieben.

## Proxy-Nachbesserung vom 10. Oktober 2026

Die Release-Pipeline nach dem Merge von PR #179 sperrt die beiden neuen
Proxy-Kandidaten wegen einer transitiven Netzwerkbibliothek. Der vollständige
Scanbericht bleibt im zugriffsgeschützten Workflow-Artefakt; individuelle
Schwachstellenangaben werden hier nicht veröffentlicht.

Der Caddy-Neubau verwendet ausdrücklich `golang.org/x/net v0.60.0`. Caddy 2.11.7
und Go 1.26.9 bleiben erhalten. Die
[Modulanforderungen des Upstream-Tags](https://github.com/golang/net/blob/v0.60.0/go.mod)
passen zur verwendeten Go-Linie. Nach dem Cross-Build prüft `go version -m`
die tatsächlich eingebettete Modulversion; eine abweichende Version bricht den
Build ab. Damit wird die Bibliothek im ausgelieferten Binary aktualisiert.

Die Freigabe benötigt weiterhin vollständige Trivy-Kandidatenscans für AMD64 und
ARM64 sowie den integrierten Starttest des Proxys. Es gibt keine zusätzliche
Ignore-Regel und keine Absenkung der bestehenden High-/Critical-Sperre.
