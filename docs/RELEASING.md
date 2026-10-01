# Releases

LearnPip verwendet eine gemeinsame Version für API, Worker und Web. Nach einem Merge auf `main`
laufen zuerst Repository-Prüfungen, Backend-Tests und Web-Build. Nur wenn alle erfolgreich sind,
berechnet semantic-release aus den Conventional Commits die nächste Version, ergänzt die
[CHANGELOG.md](../CHANGELOG.md), schreibt sie nach `main`, legt den Tag `vX.Y.Z` an und erstellt
ein GitHub Release mit Versionshinweisen. Der automatisch bereitgestellte Quellcode des Tags
enthält Compose, Dockerfiles und alle [Installationsschritte](RELEASE-INSTALL.md). Container
werden beim Betreiber lokal gebaut. Es werden weder npm-Pakete noch versionierte GHCR-Images
veröffentlicht; dadurch bleibt der Speicherbedarf auf GitHub gering.

| Änderung | Beispiel | Folge |
| --- | --- | --- |
| Feature | `feat: add family overview` | Minor-Version |
| Fehlerkorrektur | `fix: reject expired invitation` | Patch-Version |
| Breaking Change | `feat!: replace API contract` mit `BREAKING CHANGE:` im Commit-Text | Major-Version |
| Dokumentation, CI, Wartung | `docs: explain setup`, `ci: validate pull requests` | allein kein Release |

Die PR-Titel und Commit-Betreffzeilen prüft CI. Bei Squash-Merges muss der geprüfte PR-Titel
als Squash-Commit-Betreff erhalten bleiben. Bei Merge-Commits bleiben die Feature-Commits im
Verlauf. Die erste Veröffentlichung ohne vorhandenen Versionstag startet bei `v1.0.0`; diese
Festlegung vor dem ersten Merge bewusst bestätigen. Es gibt zunächst keinen Prerelease-Kanal.
Versionstags und GitHub Releases danach nicht manuell für dieselbe Reihe erstellen.

## Einmalige Einrichtung in GitHub

1. Unter den GitHub-Entwicklereinstellungen eine eigene **GitHub App** für Releases anlegen.
   Nur **Contents: Read and write** für `kennfarbe/LearnPip` gewähren und die App genau
   in diesem Repository installieren. Die App muss Tags und GitHub Releases erstellen dürfen.
2. In der `main`-Ruleset unter **Bypass list** nur diese installierte App mit **Always allow**
   aufnehmen. **For pull requests only** genügt nicht für den automatisch geschriebenen
   `CHANGELOG.md`-Commit. GitHub kann den Bypass nicht auf eine einzelne Datei begrenzen;
   die App deshalb nur für Releases verwenden.
3. Die **Client ID** als Repository-Variable `RELEASE_APP_CLIENT_ID` und den heruntergeladenen
   privaten App-Schlüssel als Repository-Secret `RELEASE_APP_PRIVATE_KEY` unter
   **Settings → Secrets and variables → Actions** hinterlegen. Der Workflow erzeugt daraus
   nur bei erfolgreichen Builds auf `main` ein kurzlebiges Installationstoken für dieses
   Repository. Ohne diese Werte scheitert der Release-Job sichtbar; PR-Jobs erhalten sie nicht.
4. Falls weitere Tag-Rulesets aktiv sind, `v*` für die App freigeben. Ein Testmerge mit
   Conventional Commit prüft den vollständigen Ablauf.

Der Release-Commit enthält `[skip ci]`, damit dessen erneuter Push keine Endlosschleife auslöst.
Die Changelog-Datei wird nur bei einer tatsächlich ermittelten neuen Version ergänzt.
