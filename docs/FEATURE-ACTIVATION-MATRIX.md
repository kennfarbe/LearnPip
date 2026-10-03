# Funktionsabhängige Aktivierung – Prüf- und Hinweisentwurf

**Status: konzeptionelle Matrix, noch nicht als Admin-Dialog implementiert.**
Dieses Dokument konkretisiert #103 und die Abhängigkeiten #93 bis #102.
Die endgültigen Hinweise hängen vom tatsächlichen Betreiber, Zielland,
Betriebsmodell und der Rechtsprüfung ab. Keine Funktion wird durch diese
Dokumentation aktiviert oder als konform bezeichnet.

| Option | Vor Aktivierung technisch/fachlich zu prüfen | Bedienablauf und Nachweis |
| --- | --- | --- |
| Lokales pseudonymes Konto | Wiederherstellungskennung, Zuordenbarkeit, Verlust-/Missbrauchsfolgen, Backup und Betreiberinformationen | Funktionsbeschreibung; keine Zusage völliger Anonymität |
| E-Mail-Anmeldung | SMTP, E-Mail-Verifizierung, Dienstleister/Empfänger, Geheimnisse, Daten- und Aufbewahrungsmodell | Admin bestätigt Konfiguration und Betreiberinformationen |
| Passwort-Admin (#94) | Einmaliges Setup, sicherer Secret-Transport, Rate-Limit, Wiederherstellung, letzte Adminrolle | Kein fester/leer eingesetzter Standard und kein offener Reset |
| Externe Identitäten (#95, #104–#107) | Pro Anbieter Registrierungen, minimal nötige Scopes, Redirects, OIDC/OAuth-Unterschiede, Datenflüsse, Abschaltbarkeit | Anbieter separat auswählbar, ausdrücklich ohne automatisches E-Mail-Merging |
| Lokale KI | Modell, Betreiber, Eingabe-/Bildschutz, Lizenz und Korrekturmöglichkeit | Vor Verwendung verständliche KI-Kennzeichnung |
| Cloud-KI | Zielprovider und Verarbeitung, etwaige Transferbedingungen, Quota, schutzbedürftige Inhalte und KI-Einstufung | Cloud bleibt standardmäßig aus, bewusste Bestätigung |
| Öffentliche Fragen und Community-Pakete | Moderation, Meldesystem, Quellen-/Medienlizenzen, Betreiberrolle, Verbreitungsrechte | Keine automatische Veröffentlichung durch lokalen Export |
| Moderation aller privaten Fragen (#119/#120) | Zweckbindung, Audit, Zugriffsdaten, Datenschutzinformation und sorgfältige Rollenvergabe | Gesonderte Warnung für Fremd-Privatinhalte; keine pauschale Konto-/Lernstandberechtigung |
| Eltern-/Schulbetrieb | Verknüpfungsumfang, Minderjährigenkonzept, Schulverantwortung und Zugriffsgrenzen | Betreiberentscheidung und Prüfung dokumentieren |
| Gehosteter oder kommerzieller Dienst | Vertrags-/Verbraucher- und Betreiberpflichten, Updates, Support, Barrierefreiheit und Einsatzprofil | Getrenntes Freigabegate vor Angebot |

Für jede zukünftige Einstellung sind eine differenzierte Vorschau der
tatsächlich hinzugewonnenen Rechte, eine ausdrückliche bewusste
Bestätigung, eine serverseitige Kontrolle und ein nachvollziehbares Audit
vorzusehen. Betreiberhinweise ersetzen keine technische
Zugriffsbegrenzung. Erst ein UI-/API-/Testnachweis erlaubt #103 zu
schließen. Kritische Änderungen an Nutzer-/Moderationsrechten dürfen
nicht versehentlich über einen Sammelschalter erfolgen.
