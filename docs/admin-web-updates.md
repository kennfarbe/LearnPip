# Architektur der webbasierten Admin-Updates

**Status:** Implementierung im offenen PR #91; vor produktiver Nutzung müssen der PR gemergt, ein stabiles Release veröffentlicht und der Host-Operator eingerichtet sein. Die Issues #75, #76 und #77 werden gemeinsam behandelt. Diese Dokumentation beschreibt die Implementierung im Feature-Branch, nicht eine bereits allgemein ausgerollte Funktion.

## Sicherheitsgrenzen

Die API- und Webcontainer erhalten weder Docker-Socket noch Host-Shell, sudo-Rechte oder Zugangsdaten des Operators. Die API schreibt nur geprüfte Update-Aufträge in eine private gemeinsame Warteschlange. Ein getrennter Prozess `learnpip-update-operator` läuft unter demselben unprivilegierten Linux-Konto wie der Rootless-Docker-Daemon und verarbeitet diese Aufträge.

Der Operator akzeptiert nur JSON-Aufträge mit einer stabilen SemVer-Version (`vMAJOR.MINOR.PATCH`) und eindeutiger Auftragskennung. Repository, Release-API und Archivquelle sind auf `kennfarbe/LearnPip` festgelegt. Unmittelbar vor der Ausführung werden die Release-Metadaten erneut geprüft; Entwürfe und Vorabversionen werden abgelehnt. Danach ruft der Operator `scripts/install-release.sh update --version vX.Y.Z --yes` auf. Vom Browser übergebene URLs, Pfade oder Befehle werden nicht ausgeführt.

## Herkunft und Integrität

GitHub wird über HTTPS als Bezugsquelle verwendet. Das Installationsskript weist nicht-stabile Tags zurück, gleicht den Release-Tag ab, verwirft unsichere Archivpfade, Links und besondere Dateitypen und verlangt die erwarteten Deployment-Dateien vor der Aktivierung. Eine zusätzliche Signatur- oder Prüfsummenverifikation kann später ergänzt werden; sie ist hier **nicht** als bereits implementiert dargestellt.

## Update-Ablauf

1. Die Administration prüft verfügbare stabile Releases und sieht die Release Notes.
2. Die installierte und die konkrete Zielversion müssen ausdrücklich bestätigt werden. Für Cookie-Zugriffe gelten Origin-/CSRF-Prüfung und eine frische Administratorsitzung.
3. Die API protokolliert einen Auftrag und legt ihn in der Warteschlange ab. Die Datenbanksperre schützt gegen konkurrierende Aufträge.
4. Der Operator prüft Rootless Docker, freien Speicherplatz und die Release-Herkunft und verlangt zunächst eine erfolgreiche Sicherung.
5. Das Installationsskript lädt die versionierten Images, migriert, startet die Dienste neu und prüft deren Gesundheit. Erst nach erfolgreicher Prüfung wird die aktive Release-Verknüpfung umgestellt.
6. Der Operator schreibt eine Statusdatei. Die API importiert den Status und protokolliert das Ergebnis. Fehlgeschlagene Migration oder Healthchecks gelten nie allein wegen heruntergeladener Dateien als Erfolg.
7. Nach einem Neustart fragt der Browser die tatsächlich aktive `LEARNPIP_VERSION` erneut ab.

Warteschlange und Statusdateien liegen außerhalb der API- und Webcontainer; ein API-Neustart darf einen laufenden Host-Auftrag nicht aus der Warteschlange verlieren. Der Operator verwendet eine Dateisperre und atomar ersetzte Statusdateien.

## Sicherung und Wiederherstellung

Vor jedem Web-Update muss eine konsistente PostgreSQL-Sicherung erfolgreich sein. Das Sicherungsmanifest enthält einen SHA-256-Wert und betriebliche Prüfdaten, aber keine Geheimnisse. Konfiguration und Secrets bleiben im gemeinsamen Installationsverzeichnis und müssen zusätzlich über eine externe VM-/Host-Sicherung geschützt werden. Datenbankmigrationen sind nicht automatisch umkehrbar: Das Zurücksetzen des `current`-Symlinks ist **kein** Datenbank-Rollback. Die Wiederherstellung ist eine getrennte, ausdrücklich bestätigte Administrationsaktion nach dem dokumentierten isolierten Restore-Verfahren. Fehlgeschlagene Migrationen und Healthchecks erfordern Prüfung vor einem erneuten Versuch.

## Berechtigungen und Einrichtung

Normale Web-Updates benötigen kein sudo. Für die Erstinstallation können einmalig erhöhte Rechte für Abhängigkeiten, Netzwerkports oder Rootless-Docker-Einrichtung erforderlich sein. Vorhandene Rootful-Installationen müssen für diese Funktion auf Rootless Docker umgestellt werden. Mitgliedschaft in einer Rootful-`docker`-Gruppe oder pauschale sudoers-Freigaben sind **kein** unterstütztes Sicherheitsmodell.

Der Host-Dienst ist in [`deploy/learnpip-update-operator.service`](../deploy/learnpip-update-operator.service) beschrieben. Der Dienst muss unter dem für die Installation zuständigen Benutzer eingerichtet und aktiviert werden. Siehe auch [Administration](ADMINISTRATION.md) und [Release-Installation](RELEASE-INSTALL.md).
