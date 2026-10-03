# Separater Moderationszugriff auf private Fragen

Der normale GET-Endpunkt zum Lernen wird **nicht** für Moderatoren geöffnet. Die neue API nutzt die bestehende serverseitige Moderationsrolle; Administratoren erfüllen diese Richtlinie ebenfalls. Normale Benutzer erhalten keinen Zugriff auf die Moderationsrouten.

- POST /api/v1/moderation/questions/browse mit `{"reason":"konkreter Prüfzweck","page":0}` listet maximal 50 aktuelle Fragenfassungen pro Seite, ohne Benutzerprofil, Lernstände, Gruppenkennungen oder Zugangsdaten. Die Suche wird als administrative Auditaktion erfasst.
- POST /api/v1/moderation/questions/{versionId}/inspect mit `{"reason":"konkreter Prüfzweck"}` lädt eine konkrete Fassung inklusive Inhalt zur fachlichen Moderation und schreibt **vor erfolgreicher Antwort** ein Auditereignis mit Akteur, Version, Zweck und Zeitpunkt.
- Beide Aufrufe erfordern eine Begründung von 10 bis 500 Zeichen und durchlaufen die bestehende Rollen- und Rate-Limit-Prüfung. Der Zugriff ist ausdrücklich ein Moderationsvorgang; ein solcher Zugriff erteilt **keine** Rechte zum Export oder zur Veröffentlichung von Fremdinhalten.
- Der normale Besitzer-/Gruppen-/Freigabezugang bleibt unverändert. Rollenentzug wird bei jeder Anfrage aus der Datenbank geprüft.

## Noch nicht erfüllt

Dies ist eine **Teilumsetzung** der Issues #119 und #120 und keine Aktivierung der frei konfigurierbaren Rechte. Insbesondere fehlen ein entsprechender gesonderter deutscher Moderationsarbeitsplatz im Frontend, vollständige Besitzer-/Fremdbearbeitung und Löschung ohne eingehende Meldung, einheitliche Objektberechtigungen für Import/Export, Medien/Bulk-Aktionen, ein revisionssicheres Berechtigungskonzept je Rolle sowie die ausdrückliche Datenschutz-/Betreiberbestätigung beim Aktivieren des Zugriffs auf private Fragen. Die #119-Standardmatrix wird nicht als abgeschlossene Umsetzung ausgegeben.

Die bisherigen Feedback-Moderationsendpunkte bleiben aus Kompatibilitätsgründen unverändert; ihre Audit- und Zugriffslogik muss bei der Gesamtüberarbeitung einbezogen werden.
