# Betreiberprüfung, Sicherheitsbetrieb und Vorfallübung

Prüfdatum: **05.10.2026**, erste fachlich zu überprüfende Fassung zu #98.
NIS2 reguliert bestimmte Einrichtungen; LearnPip ist kein Beleg ihrer Betroffenheit.

## Bedingte Entscheidungen

| Einsatz | Arbeitsbewertung | Vor Nutzung auszufüllender Nachweis |
| --- | --- | --- |
| Ausschließlich private Erprobung ohne entgeltlichen Dienst | Keine Einrichtung allein durch den privaten Betrieb | Bei Nutzungsänderung neue Einrichtungsprüfung |
| Schule | Schule allein entscheidet die Einordnung nicht; Träger, öffentliches Recht und weitere Tätigkeiten prüfen | Träger/Rechtsperson, Sektor, Behördenregeln, zuständige Stelle |
| Unternehmen | Seine gesamten einschlägigen Tätigkeiten und Größe prüfen, nicht nur LearnPip | Einrichtungsart, Beschäftigte, Umsatz/Bilanz, Verbund und Ausnahmen |
| Gehosteter Dienst | Bei einschlägiger Tätigkeit etwa Cloud-/Managed-Service-Regeln prüfen; Lern-SaaS nicht automatisch gleichsetzen | Dienstdefinition, Sektor, Größe, Sitz und zuständige Behörde |

Grundlage: [BSIG § 28 und Anlagen](https://www.gesetze-im-internet.de/bsig_2025/__28.html)
und [BSI-Betroffenheitsprüfung](https://www.bsi.bund.de/dok/nis-2-betroffenheitspruefung).
Größenschwellen allein entscheiden nicht; besondere Einrichtungsarten und
Ausnahmen berücksichtigen. Für einen tatsächlichen Betreiber sind Ergebnis,
Eingaben, Datum und fachliche Entscheidung privat zu hinterlegen. Die interaktive
BSI-Prüfung wurde hier nicht als realer fremder Betreiber ausgefüllt.

Registrierung und Meldungen sind nur bei tatsächlicher Betroffenheit Pflichten.
Dann [§ 33 BSIG](https://www.gesetze-im-internet.de/bsig_2025/__33.html) und den
aktuellen BSI-Meldeweg verwenden. Die regelmäßige Prüfung kann durch Einführung
eines neuen Sektors, Diensts, Standorts oder einer geänderten Größe ausgelöst werden.

## Betriebsplan

| Maßnahme | Verantwortungsrolle | Interner Nachweis und Turnus |
| --- | --- | --- |
| Risikoanalyse | Betreiberleitung mit Technik/Datenschutz | Zweck, Bedrohungen, Schutzbedarf und Restgefahren vor Betrieb und jeder wesentlichen Änderung |
| Least-Privilege/MFA | Infrastrukturverantwortliche | Getrennte Adminidentitäten; MFA für Host, VPN, Repository und Registry; LearnPip besitzt keine eigene MFA-Zusage |
| Lieferkette/Patches | Projektführung liefert Fix; Betreiber installiert | Wöchentliche Scanergebnisse, Supportinventar, kritische Befunde sofort triagieren; Tests vor Update |
| Logs/Secrets | Infrastrukturverantwortliche | Keine Anfragekörper, Bilder, Tokens, internen Adressen oder Fallakten in öffentliche Logs; Zugriff und Frist festlegen |
| Backup/Restore | Infrastrukturverantwortliche und Vertretung | Verschlüsselte externe Sicherung mit Schlüssel-Volume; monatliche Wiederherstellungsübung in isolierter Umgebung |
| Notbetrieb | Betreiberleitung | Schreibzugriff stoppen, Provider deaktivieren, Konten/Sitzungen sperren, letzte geprüfte Sicherung isoliert herstellen |
| Schulung | Betreiberleitung | Einführung und jährliche Übung zu Phishing, Meldung, sicheren Exporten und KI-Fehlern |

[Installation](RELEASE-INSTALL.md), [Datenbank/Restore](DATABASE.md),
[Sicherheitsprüfung](SECURITY-AUDITS.md). Die Software allein erfüllt keine
Organisationspflicht und behauptet keine notwendige ISO-27001-Zertifizierung.

## Vorfallablauf

1. Entdeckung mit UTC-Zeit, Umfang und synthetischer/privater Fallkennung erfassen.
2. Betreiberleitung und Vertretung informieren; Spuren geschützt sichern,
   betroffene Verarbeitung eingrenzen. Externe Empfänger nur nach Zuständigkeit.
3. Parallel getrennt bewerten: Datenschutzverletzung, NIS2-erheblicher Vorfall,
   CRA-Produktvorfall/aktive Ausnutzung. Eine Uhr ersetzt keine andere.
4. Bei NIS2-Pflicht 24h-Erstmeldung, 72h-Meldung, gegebenenfalls Zwischenbericht
   und Monatsabschluss nach [§ 32 BSIG](https://www.gesetze-im-internet.de/bsig_2025/__32.html).
   Bei laufendem Vorfall Fortschrittsbericht und anschließend Abschluss.
5. Fix/Rotation/Sperre, isolierter Restore, Zugriffstests und Freigabe dokumentieren.
   Gesetzlich nötige Meldungen nicht bis zur vollständigen Fehleranalyse aufschieben.

Im privaten Betriebsregister sind Leitung, technische Ansprechperson,
Datenschutzstelle, Vertretung, erreichbarer Meldekanal und Behördenzugang zu
benennen. Sie werden nicht als reale Personen oder bestehende Zugänge erfunden.

## Synthetische Vorfallübung

`node scripts/incident-exercise.mjs` nutzt ausschließlich feste erfundene Daten,
keine Netzverbindung, keine echten Meldungen und keine produktive Datenbank.
Es berechnet getrennte CRA-/NIS2-/DSGVO-Fristen und prüft Eskalation bei
verspäteten Schritten. Kalenderfristen werden als Monate behandelt, nicht
pauschal als 30 Tage. Die Übung ist eine konservative UTC-Planung, keine
verbindliche Berechnung gesetzlicher Sonderregeln oder Fristverlängerungen. Ein Befund ohne aktive Ausnutzung wird nicht automatisch
zur CRA-Schwachstellenmeldung gemacht.

Die bestehende `tests/ops/restore-smoke.sh` prüft dagegen den tatsächlichen
isolierten Docker-/PostgreSQL-Restore mit synthetischen Medien und Lernständen.
Beide Prüfungen sind technische Nachweise; keine Behördenteilnahme oder reale
Organisationsübung behaupten. Vor organisatorischer Nutzung muss eine benannte
Vertretung den Ablauf einschließlich tatsächlicher Erreichbarkeit erproben.

Ergebnis der lokalen Ausführung vom 05.10.2026: alle eingebauten Prüfungen
bestanden, null externe Meldungen; [synthetisches Ergebnisprotokoll](evidence/incident-exercise-2026-10-05.json).
Die Repository-CI wiederholt diese Übung.
