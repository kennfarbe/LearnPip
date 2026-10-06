# Öffentliche Inhalte: Dienstbewertung und Moderationslücken

Prüfdatum **05.10.2026**, erste fachlich zu überprüfende Fassung zu #101.
Softwarebereitstellung und tatsächlicher Betrieb eines öffentlichen Dienstes
sind verschiedene Rollen. Der heutige private Testbetrieb belegt keinen bereits
rechtskonformen öffentlichen Hostingdienst.

## Bedingte Einordnung und Pflichten

| Szenario | Arbeitsbewertung und zu klärende Fakten |
| --- | --- |
| Private LAN-Instanz ohne öffentlichen Dienst | Keine öffentliche Plattform allein wegen vorhandener Veröffentlichungsfunktion |
| Öffentlich gespeicherte Nutzerfragen/-bilder | Hostingrolle prüfen; Verbreitung an die Öffentlichkeit kann Plattformfunktion sein |
| Schulgruppe mit begrenztem Empfängerkreis | Gruppenfunktion, Betreiber und tatsächlichen Dienst prüfen; nicht pauschal öffentlich oder pauschal ausgenommen |
| Eigene redaktionelle Kataloge ohne Nutzeruploads | Nicht automatisch Hosting fremder Informationen; Anbieter-/Vertrags-/Inhaltsrechte trotzdem prüfen |

[DSA](https://eur-lex.europa.eu/eli/reg/2022/2065/oj): Definitionen Art. 3,
Kontaktstellen Art. 11/12, Bedingungen Art. 14, Hosting-Meldung/Begründung
Art. 16/17. Größen-/Nebentätigkeitsausnahmen ausdrücklich prüfen; kleine
Anbieter sind nicht pauschal von allen Hostingpflichten frei. Bei Plattformen
zusätzlich Art. 19/20, Transparenz und Minderjährigenschutz bewerten.
[§ 5 DDG](https://www.gesetze-im-internet.de/ddg/__5.html) verlangt bei seinem
Anwendungsbereich unmittelbar erreichbare Anbieterinformationen. Rechtsperson,
Adresse, Kontakt und gegebenenfalls weitere Pflichtangaben nicht erfinden.

## Tatsächlicher API-Abgleich

| Anforderung | Vorhanden | Nachgewiesene Lücke vor öffentlichem Hostingbetrieb |
| --- | --- | --- |
| Meldung rechtswidriger Inhalte | Authentifizierter Feedback-Endpunkt mit Gründen `rights`/`privacy`, begrenztem Freitext und offener Meldung je Version | Kein allgemein zugängliches vollständiges Art.-16-Verfahren ohne LearnPip-Konto; Ort, Kontakt/Gutglaubenserklärung und Sonderfälle ergänzen |
| Eingang/Entscheidung | HTTP 201 mit Fall-ID; Moderation speichert Aktion/Begründung | HTTP-Antwort ist keine verlässliche spätere Zustellung an meldende Person; Status-/Mitteilungskanal fehlt |
| Begründung/Rechtsbehelf | Moderationsnotiz vorhanden | Keine vollständige separate Begründungszustellung an Betroffene und Meldende mit Rechtsbehelfsinformation |
| Missbrauch/Datenschutz | Objektberechtigung, Dublettensperre und geschützte Moderationsrolle | Melde-Rate-Limit, datensparsame unabhängige Beschwerdebehandlung und Nachweisfristen vervollständigen |
| Private Inhalte | Eigener Lernzugang bleibt beschränkt; gesonderter Moderationszugriff mit Zweck/Audit | Alte Feedback-Detailrouten nicht als umfassend auditierte Datenschutzlösung ausgeben |

Beleg: `CommunityFeedbackEndpoints.cs`, `QuestionReport`, Moderationsereignisse
und Regressionen in `ApiV1Tests`. Für einen anwendbaren öffentlichen Dienst muss
der Betreiber diese Lücken schließen oder ein geprüftes vollständiges externes
elektronisches Verfahren integrieren. Eine Mailadresse in einem Dokument allein
belegt keine funktionsfähige Zustellung oder Beschwerdebehandlung. #101 bleibt
bis zum entsprechend getesteten Verfahren offen.

## Moderationsverfahren und Nachweise

Für neue Fälle: Eingang bestätigen, genauen Inhalt lokalisieren, nachvollziehbar
rechtlich/fachlich prüfen, erforderliche Maßnahmen begrenzen und begründen,
Betroffene/Meldende über Ergebnis und Rechtsbehelf informieren. Bei dringender
Gefahr zuständige Stellen einbeziehen. Begründung und Kontakt bleiben privat;
öffentliche Transparenzberichte nur aggregiert und ohne Fallakten.

Interne Vorgabe: laufender Fall bis Abschluss plus 90 Tage prüfen; gesetzlich
erforderliche Beschwerdefristen und notwendige Beweissicherung gehen vor.
Längere Aufbewahrung fallbezogen begründen, nicht sämtliche Inhalte unbegrenzt
aufheben. Rollenentzug, Auskunft und Löschung in dieses Verfahren einbeziehen.

Fragen, Fotos, Übersetzungen und amtliche Kataloge haben getrennte Rechteketten.
Eine amtliche Herkunft ist keine automatische freie Bild-/Bearbeitungslizenz.
[Lizenzkonzept](LICENSING.md) und [Katalogformat](CATALOG-INTERCHANGE.md).
Lokaler Export veröffentlicht nichts. Eine Rechtebeanstandung darf weder private
Fotos offenlegen noch als allgemeine Erlaubnis zum Fremdexport dienen.

Verantwortlich: Betreiber entscheidet Dienstrolle und Kontaktverfahren;
Projektführung implementiert nachgewiesene Produktlücken; Moderation bewertet
einzelne Fälle. Erst ein synthetischer Test von Meldung, Bestätigung, Zustellung,
Begründung, Widerspruch und Rechteentzug trägt die technische Abnahme.
