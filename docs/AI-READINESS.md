# KI-Zweckbestimmung, Rollen und aktueller Gesetzesstand

Prüfdatum **05.10.2026**, erste fachlich zu überprüfende Fassung zu #102.
Bewertet werden die vorhandenen Funktionen, nicht ein erfundener zukünftiger
Benotungsdienst. KI ist optional und standardmäßig `off`.

## Einzelzwecke und Arbeitsbewertung

| Anwendungsfall | Tatsächlicher Iststand und Abgrenzung | Entscheidung vor Verwendung |
| --- | --- | --- |
| Fotoentwurf | Gewählter Zuschnitt, strukturierter Vorschlag, richtige Antwort nicht vorausgewählt, manueller privater Entwurf | Lernassistenz; keine eigenständige Entscheidung über eine Person. Bildrechte und personenbezogene Daten prüfen |
| Lösungshinweise | Menschlich geprüfte Hinweise; zweite deterministische Rechenroutine deckt nur bestimmte Formeln ab | Fachliche Korrektheit prüfen; kein allgemeiner Wahrheitsnachweis |
| Übersetzung | Allgemeine Textgenerierung möglich; UI-Übersetzungen sind gepflegte Texte, kein automatischer KI-Übersetzungsdienst | Eine spätere automatisierte Veröffentlichung separat kennzeichnen und prüfen |
| Lernsteuerung | Wiederholungslogik verwendet richtige/falsche Antworten und feste Regeln | Nicht allein wegen Anpassung als KI-System voraussetzen; Systemdefinition und Zweck vor KI-Ersatz erneut prüfen |
| Prognose | Statistische lokale Schätzung im Prüfungsplan, kein Prüfungsbescheid | Keine Zusage tatsächlicher Prüfungsreife; bei KI-Integration neu bewerten |
| Leistungsbewertung/Zulassung | Keine automatische Schulnote, Zugangserlaubnis oder Zulassungsentscheidung implementiert | Neue solche Zwecke vor Einführung gesondert fachlich/rechtlich prüfen; kein bloßer Konfigurationsschalter |

Arbeitsbewertung: persönliche Lernassistenz ist nicht automatisch Hochrisiko.
In Bildungseinrichtungen können Bewertung von Lernergebnissen, Steuerung des
Lernprozesses, Zugang oder Zuweisung unter Anhang III Nr. 3 fallen. Ein Mensch
im Ablauf beseitigt die Einordnung nicht automatisch. Art. 6-Ausnahmen verlangen
eine dokumentierte Prüfung des tatsächlichen Einflusses; Profiling gesondert
beachten. Hersteller/Anbieter des konkreten Systems, Modellanbieter und Betreiber
der Instanz können unterschiedliche Rollen haben. Änderungen der Zweckbestimmung
oder wesentliche Veränderungen können Rollen verschieben.

## Verbindlicher Zeitplan statt alter Entwurfsannahmen

Geprüft: [AI Act 2024/1689](https://eur-lex.europa.eu/eli/reg/2024/1689/oj),
geltende [Änderungsverordnung 2026/1744](https://eur-lex.europa.eu/eli/reg/2026/1744/oj),
[konsolidierter Lesetext vom 27.07.2026](https://eur-lex.europa.eu/eli/reg/2024/1689/2026-07-27).
Die Änderung ist kein bloßer Omnibus-Entwurf; authentische Rechtsakte sind
maßgeblich, die Konsolidierung dient dem Lesen.

Art. 113: allgemeine Anwendung ab 02.08.2026; Kapitel I/II grundsätzlich seit
02.02.2025. Geänderte Hochrisikofristen für Kapitel III Abschnitte 1–3:
Anhang III ab **02.12.2027**, Art. 6 Abs. 1/Anhang I ab **02.08.2028**,
mit der gesetzlichen Ausnahme für Art. 6 Abs. 5. Art. 50 und besondere
Übergänge prüfen, nicht auf das spätere Hochrisikodatum verschieben.
Weitere Übergangsregeln gelten fallabhängig für ältere Systeme/Modelle.

Unzulässige Manipulation, Ausnutzung besonderer Verletzlichkeit und
Emotionserkennung in Bildungseinrichtungen sind nicht als LearnPip-Funktion
einzuführen; gesetzliche Tatbestände/Ausnahmen gesondert prüfen. Gamification
muss ohne Druck, Benachteiligung oder verdeckte Leistungsentscheidung bleiben.

## Transparenz, Datenschutz und menschliche Prüfung

Empfänger, Betriebsart, Limits und Einzelbestätigung zeigt
[AI_PROVIDERS](AI_PROVIDERS.md). [PHOTO_DRAFT](PHOTO_DRAFT.md) trennt Erkennung,
Unsicherheit, Prüfung und Speicherung. KI-Ausgaben bleiben korrigierbar; ungültige
Ausgaben werden abgelehnt; kein stiller Wechsel zu einem Cloud-Anbieter.
Konten, Lernstatistik und fremde Medien werden nicht automatisch ausgewählt.

Je Provider tatsächliches Modell, Version/Änderung, Zweck, Region, Empfänger,
Aufbewahrung und Trainingsbedingungen dokumentieren. Eine lokale Installation
ist nicht automatisch frei von Datenschutzrisiken. Die Anwendung startet keinen
Trainingsauftrag; Providerverträge müssen trotzdem ungewollte Trainingsnutzung
und Drittlandtransfers ausschließen bzw. rechtlich absichern. Keine unbekannte
Cloud als angeblich datenschutzkonform voreinstellen. [Betriebsnachweis](privacy/EU-OPERATING-RECORD.md).

Betreiber schulen Mitarbeitende zu Grenzen, personenbezogenen Eingaben,
Halluzinationen, Bildrechten und Korrektur. Falsche Hinweise werden fachlich
berichtigt; bei wiederkehrenden Fehlern Provider/Modus deaktivieren und Vorfall
privat erfassen. Quoten begrenzen Versuche, nicht automatisch Geldbeträge.

## Freigabe bei neuen Bildungsentscheidungen

Vor einer neuen Bewertungs-/Zulassungsfunktion muss die Projektführung Zweck,
Rolle und Einstufung dokumentieren; Betreiber/Fachstellen müssen Datenbasis,
Minderjährigenschutz und menschliche Aufsicht prüfen. Bei einschlägigem Hochrisiko
folgen Risikomanagement, Datenqualität, technische Dokumentation, Logging,
Aufsicht, Konformitäts-/Registrierungsmaßnahmen und gegebenenfalls
Grundrechtefolgenabschätzung. Eine DSFA ersetzt diese Nachweise nicht.

Die heutige Abnahme ist auf die beschriebenen assistierenden Zwecke begrenzt.
Sie erteilt keine Freigabe für automatische Noten, Zugang oder Zulassung.
`AiProviderTests`, `SolutionVerifierTests` und API-Regressionen prüfen technische
Grenzen; echte Providerregistrierung und Rechtsgrundlage bleiben Betreiberaufgabe.
