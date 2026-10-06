# Barrierefreiheit: Anwendung und tatsächliche Abnahme

Prüfdatum **05.10.2026**, erste fachlich zu überprüfende Fassung zu #100.
Automatisierte Checks und Projektfreigabe sind keine behauptete Rechtskonformität.

## Anwendbarkeit

Die [Bundesfachstelle, FAQ zu Lernmedienplattformen](https://www.bundesfachstelle-barrierefreiheit.de/DE/Barrierefreiheitsstaerkungsgesetz/FAQ/faq.html)
unterscheidet interaktive Lernangebote von E-Books und dem Verkauf von Leistungen.
Ein reines privates interaktives Angebot ist nicht allein als Lernplattform
BFSG-pflichtig. Verkauf an Verbraucher oder angebotene E-Books können zu einer
anderen Beurteilung führen. BFSG/BFSGV, Ausnahmen und konkrete Leistung prüfen.
Für öffentliche Stellen gelten eigene Bundes-/Landesregeln, nicht automatisch
dieselbe BFSG-Einordnung. Schulträger und Land sind vor Freigabe zu bestimmen.

Technische Grundlage: [WCAG 2.2](https://www.w3.org/TR/WCAG22/) für die Produktarbeit.
Die einschlägige EN 301 549 und deren Fundstelle getrennt festhalten:
[ETSI-Register](https://portal.etsi.org/webapp/WorkProgram/Frame_WorkItemList.asp?butHarmonized=Search&butPredefined=Search&optDisplay=100&qDIRECTIVE=2016%2F2102%2FEU&qHARMONIZED=True&qSORT=DIRECTIVES)
weist V3.2.1 als im Amtsblatt zitiert aus; V4.1.1 ist veröffentlicht und am
08.09.2026 an die Kommission geliefert. Eine veröffentlichte neue Norm ist nicht
automatisch schon harmonisiert. Vor Rechtsfreigabe die für den Fall geltende
Amtsblattfassung sowie BFSGV bzw. öffentliche Anforderungen abgleichen.

## Vorhandene und fehlende Nachweise

`frontend/web/tests/ui` prüft Produktionsbuild mit Fixtures, Fokus/Tastatur,
mobiles Menü, zugängliche Namen und Axe-Befunde. Die fünf Dokumentationsbilder
besitzen Alternativtexte und einen Quellenfingerabdruck; Änderungen verlangen
Neugenerierung und visuelle Prüfung gemäß [UI-PREVIEW](UI-PREVIEW.md).
Solche Tests sprechen nicht mit einem tatsächlichen Screenreader.

**Noch nicht durchgeführt:** reale NVDA-/VoiceOver-Abnahme auf Desktop und
Mobilgerät. Deshalb bleibt #100 offen, auch wenn die Projektführung die
fertigen technischen/dokumentarischen Ergebnisse abnimmt. Kein ausgefülltes
Geräte-, Personen- oder Datumsprotokoll erfinden.

## Ausführbares Abnahmeprotokoll

Nur synthetische Konten/Inhalte verwenden. Prüfer, Datum, Commit, Gerät,
OS/Browser/Screenreader mit Version, Sprache und Ergebnis privat bzw.
datensparsam dokumentieren. Desktop: NVDA mit Firefox oder VoiceOver mit Safari;
Mobil: VoiceOver/Safari auf iOS oder TalkBack/Chrome auf Android.

| Kernablauf | Tatsächlich auszuführende Schritte | Erwartete Beobachtung |
| --- | --- | --- |
| Konto/Anmeldung | Neues Konto, einmaliges Geheimnis, erneuter Login, falscher Code und deaktivierter Anbieter | Geheimnis verständlich angekündigt; Fehler zugeordnet; keine unerreichbaren Anbieter |
| Wiederherstellung | Sitzung verlassen, korrekt/falsch wiederherstellen, Rotation nach erneuter Anmeldung | Fokus und Status hörbar; Fehler nicht nur über Farbe; kein Verlust nach unbemerktem Wechsel |
| Frageneditor | Nur Tastatur: Frage, Antworten, Bild/Alttext, Entwurf, Bereichswechsel abbrechen | Labels/Blockreihenfolge korrekt; Dialogfokus bleibt erreichbar; Speichern meldet Ergebnis |
| Lernen | Antwort auswählen, prüfen, Rückmeldung anhören, Hinweis nutzen, Sitzung wechseln/fortsetzen | Antwortzustand, Fortschritt und Ergebnis verständlich; Fokus springt nicht unkontrolliert |
| Kataloge/Import/Export | Auswahl/Filter, fehlerhaftes Paket, Vorschau, Rechtebestätigung und Download | Fehler/Vorschau zugeordnet; Checkbox eindeutig; keine Auslösung ohne Bestätigung |
| Einstellungen | Sprache, Hell/Dunkel/System, Kontoexport, Familienzugriff und Löschbestätigung | Zustände hörbar, gefährliche Aktionen verständlich; Tastatur und Touch erreichen alle Felder |

Zusätzlich: Tab/Umschalt+Tab/Enter/Leertaste/Escape, sichtbarer Fokus,
200/400 Prozent Zoom, 320 CSS-Pixel Reflow, Kontrast, Touch-Ziele und
`prefers-reduced-motion` prüfen. Informative Bilder haben inhaltlichen Alttext;
dekorative Bilder sind entsprechend ausgeblendet. Ausklappbereiche, Live-Status
und Formfehler in beiden Sprachen anhören, nicht allein DOM-Attribute prüfen.

Jeder Befund erhält Priorität, Ablauf, reproduzierbare Schritte, Soll/Ist,
Korrektur-Commit und Wiederholungsprüfung. Blockierende Login-/Lernfehler
verhindern Abnahme. Falls rechtlich erforderlich: konkrete erreichbare
Barrierefreiheitsinformation, Feedback-/Kontaktweg und gegebenenfalls Erklärung
des Betreibers bereitstellen. Ein leerer Platzhalter ist keine veröffentlichte
Erklärung. [Arbeitsbereiche](UI-WORKSPACES.md).
