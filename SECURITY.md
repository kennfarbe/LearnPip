# Sicherheitsmeldungen

Bitte melde Sicherheitslücken **nicht in einem öffentlichen Issue oder Pull Request** und veröffentliche keine Zugangsdaten, personenbezogenen Daten oder verwundbare Beispieldateien.

Öffne im Repository die Registerkarte **Security** und nutze **Report a vulnerability**, sofern diese Funktion verfügbar ist. Falls GitHub keine private Meldung anbietet, kontaktiere die Projektverantwortlichen über ihr GitHub-Profil und bitte um einen privaten Meldeweg.

Für die lokale Nachprüfung der automatischen Prüfungen siehe
[Private Schwachstellenprüfung](docs/SECURITY-AUDITS.md). Einzelbefunde gehören nicht in
öffentliche CI-Logs oder Artefakte.

## Geltungsbereich

LearnPip verfügt inzwischen über ein startbares Backend, eine Angular-Weboberfläche, einen Worker, Installations- und Update-Skripte sowie veröffentlichte Release-Artefakte. Bitte melde Sicherheitsprobleme im Quellcode, in Workflows, Docker-Images, Deployment-Dateien, dem Update-Operator und in den offiziell ausgelieferten Komponenten. Gib soweit möglich die **betroffene Version bzw. den Release-Tag** an.

Bitte beschreibe den Fehler, die betroffene Datei oder Version, mögliche Auswirkungen und – falls vorhanden – sichere Schritte zur Reproduktion. Teile Exploits oder sensible Daten nur soweit nötig über einen privaten Kanal.

## Bearbeitung

Die Projektverantwortlichen übernehmen Triage und Release-Koordination; eine Vertretung ist im privaten Betriebsregister zu benennen. Interne Zielzeiten sind eine Eingangsbestätigung binnen zwei und eine erste Triage binnen fünf Arbeitstagen, keine zugesicherte Reaktionsgarantie. Sie prüfen den Bericht und stimmen die nächsten Schritte vertraulich mit der meldenden Person ab. Bitte veröffentliche Details nicht, bevor eine Lösung oder abgestimmte Offenlegung möglich ist.

Supportumfang, vertraulicher Kanalnachweis, koordinierte Offenlegung und bedingte
CRA-Meldungsfristen stehen in [CRA-READINESS](docs/CRA-READINESS.md).
