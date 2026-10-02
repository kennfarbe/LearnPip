# Releases

LearnPip verwendet eine gemeinsame Version für API, Worker und Web. Nach einem Merge auf `main`
laufen Repository- und Markdown-Prüfungen, Backend-Tests mit .NET-/StyleCop-/Sonar-Analyzern, Web-Build mit Angular ESLint sowie Installer- und Restore-Smoke-Tests. Analyzer- und Lint-Warnungen werden im aktiven Regelprofil als Fehler behandelt. Anschließend berechnet
semantic-release aus den Conventional Commits die nächste Version, aktualisiert `CHANGELOG.md`,
legt den Tag `vX.Y.Z` an und erstellt das GitHub Release.

Nur wenn tatsächlich ein neuer stabiler Release-Tag entstanden ist, startet danach der
`docker-publish`-Job. Er baut die drei Laufzeitkomponenten direkt aus diesem unveränderlichen Tag
und veröffentlicht Multi-Arch-Images (`linux/amd64`, `linux/arm64`) in Docker Hub:

- `kennfarbe/learnpip:api-vX.Y.Z` und `api-latest`
- `kennfarbe/learnpip:worker-vX.Y.Z` und `worker-latest`
- `kennfarbe/learnpip:web-vX.Y.Z` und `web-latest`

Produktive Installationen verwenden die versionierten Tags. Dadurch laufen API, Worker und Web
immer aus demselben Release und ein späteres `latest` verändert eine laufende Installation nicht.

## Versionsregeln

| Änderung | Beispiel | Folge |
| --- | --- | --- |
| Feature | `feat: add family overview` | Minor-Version |
| Fehlerkorrektur | `fix: reject expired invitation` | Patch-Version |
| Breaking Change | `feat!: replace API contract` | Major-Version |
| Dokumentation/CI/Wartung | `docs: explain setup` | allein kein Release |

## Einmalige Einrichtung

Die bestehende Release-GitHub-App benötigt weiterhin nur die bereits dokumentierten Contents-Rechte.
Zusätzlich muss unter **Settings → Secrets and variables → Actions** das Secret
`DOCKERHUB_TOKEN` existieren. Es sollte ein Docker-Hub Access Token für den Benutzer
`kennfarbe` mit den zum Pushen von `kennfarbe/learnpip` nötigen Rechten sein. Das Docker-Hub-
Passwort wird nicht in GitHub gespeichert.

Der Workflow verwendet gepinnte offizielle Docker-Actions für Login, QEMU, Buildx und Build/Push.
Die Images werden erst nach dem Semantic Release gebaut; normale Pull Requests und Commits ohne
neuen Release veröffentlichen nichts nach Docker Hub.

## Pull Requests mergen

Pull Requests werden standardmäßig mit **Create a merge commit** zusammengeführt. Squash-Merges sind für den Release-Prozess nicht erforderlich.

Jeder Commit eines Pull Requests muss bereits einen Conventional-Commit-Betreff besitzen, zum Beispiel `feat(ops): add release installer`, `fix(web): correct login redirect` oder `docs: explain setup`. Die Repository-Policy prüft diese Commit-Betreffzeilen vor dem Merge.

Der zusätzliche GitHub-Merge-Commit mit einem Betreff wie `Merge pull request ...` ist unproblematisch: Semantic Release betrachtet die vollständige Commit-Historie seit dem letzten Release-Tag und erkennt die darin enthaltenen `feat:`-, `fix:`- und Breaking-Change-Commits. Dadurch bleiben die einzelnen Entwicklungs-Commits erhalten und bestimmen gemeinsam die nächste Release-Version.

Der Pull-Request-Titel bleibt ebenfalls im Conventional-Commit-Format, damit die Änderung in GitHub eindeutig klassifiziert ist; für die Versionsberechnung bei einem normalen Merge sind jedoch die einzelnen Commits maßgeblich.
