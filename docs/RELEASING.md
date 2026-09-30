# Releases

LearnPip verwendet eine gemeinsame Version für API, Worker und Web. Nach einem Merge auf `main`
laufen zuerst Repository-Prüfungen, Backend-Tests und Web-Build. Nur wenn alle erfolgreich sind,
berechnet semantic-release aus den Conventional Commits die nächste Version, ergänzt die
[CHANGELOG.md](../CHANGELOG.md), schreibt sie nach `main`, legt den Tag `vX.Y.Z` an und erstellt
ein GitHub Release mit den Versionshinweisen, dem Installationsarchiv samt SHA-256 und
den versionierten Images für API, Worker und Web in GHCR. Es wird kein npm-Paket veröffentlicht.

| Änderung | Beispiel | Folge |
| --- | --- | --- |
| Feature | `feat: add family overview` | Minor-Version |
| Fehlerkorrektur | `fix: reject expired invitation` | Patch-Version |
| Breaking Change | `feat!: replace API contract` mit `BREAKING CHANGE:` im Commit-Text | Major-Version |
| Dokumentation, CI, Wartung | `docs: explain setup`, `ci: validate pull requests` | allein kein Release |

Die PR-Titel und Commit-Betreffzeilen prüft CI. Bei Squash-Merges muss der geprüfte PR-Titel
als Squash-Commit-Betreff erhalten bleiben. Bei Merge-Commits bleiben die Feature-Commits im
Verlauf. Die erste Veröffentlichung ohne vorhandenen Versionstag startet bei `v1.0.0`; vor dem
ersten Merge sollte diese Festlegung bewusst bestätigt werden. Danach keine Tags/Releases manuell
für dieselbe Versionsreihe erstellen. Es gibt zunächst keinen Prerelease-Kanal.

## Einmalige Einrichtung in GitHub

1. Einen dedizierten Release-Bot mit einem rotierbaren, eng berechtigten Fine-grained Token
   (**Contents: Read and write** für nur dieses Repository) einrichten.
2. In der `main`-Ruleset nur dem dedizierten Release-Bot einen Bypass für den automatisch
   erzeugten `CHANGELOG.md`-Commit erlauben. Rulesets können keinen Bypass auf eine einzelne
   Datei begrenzen: den Bot deshalb nur für Releases verwenden.
3. Das Token als Secret `RELEASE_TOKEN` unter **Settings → Secrets and variables → Actions**
   hinterlegen. Bei einem fehlenden oder nicht zum Push berechtigten Token scheitert der
   Release-Job sichtbar; Build-Checks bleiben davon unabhängig. Tokens werden nie in PR-Jobs geladen.
4. Der Bot benötigt auch das Recht, Tags und GitHub Releases zu erstellen. Das Workflow-Token
   benötigt `packages: write` für GHCR; die drei Images in GitHub Packages einmalig auf
   öffentliche Lesbarkeit stellen. Falls weitere
   Tag-Rulesets aktiv sind, `v*` entsprechend freigeben. Ein Testmerge mit Conventional Commit
   prüft den kompletten Ablauf.

Der Release-Commit enthält `[skip ci]`, damit dessen erneuter Push keine Endlosschleife auslöst.
Die Changelog-Datei wird nur bei einer tatsächlich ermittelten neuen Version ergänzt. GitHub
Releases enthalten das [Installationspaket](RELEASE-INSTALL.md) und verweisen auf die gebauten
Images. Datenbank, Zertifikate und geheime Konfiguration sind bewusst installierte Zustandsdaten
und werden durch Betreiber-Backups gesichert.
