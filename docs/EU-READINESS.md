# EU-Produktreife: Prüfmatrix und Freigabegates

**Prüfstand vom 05.10.2026 – erste fachlich zu prüfende Fassung, keine Rechts- oder Konformitätsbestätigung.**
Diese Übersicht ordnet die in #93 bis #103 angefragten Prüfungen. Sie erfindet
weder eine juristische Einordnung noch einen verantwortlichen Betreiber.
Tatsächliche Nutzung, Umsatzmodell, Rollenteilung, Länder, Datenflüsse und
extern eingeschaltete Funktionen müssen je Einsatz ermittelt und dokumentiert
werden, bevor ein entsprechender Betrieb als rechtlich freigegeben gilt.

## Szenarien getrennt bewerten

| Betriebsmodell | Derzeit zu erfassende Tatsachen | Fachliche Freigabe |
| --- | --- | --- |
| Nichtkommerzielles Community-Projekt | Projektträger, Vertrieb, externe Beiträge, Infrastruktur, Rolle eines möglichen Open-Source-Stewards | Offen |
| Kommerziell vertriebene Software | Tatsächlicher Hersteller, Marktbereitstellung, Produktklassifizierung, Support-/Updateverantwortung, Lizenzkette | Offen |
| Eigener öffentlich zugänglicher Dienst | Vertragsbeziehungen, Verantwortliche, Inhalte/Nutzer, Hosting, Identitäten, Zahlungen | Offen |
| Private LAN-/VPN-Installation | Installierende, Erreichbarkeit, Datenhoheit, Funktionen, Wartung und Backup | Offen |
| Schule oder Unternehmen | Rechtsgrundlage und Zuständigkeiten des Betreibers, Minderjährige/Beschäftigte, externe Dienste | Offen |

Das Aktivieren oder Abschalten einzelner Funktionen ersetzt keine
Grundprüfung für das tatsächliche Betriebsmodell. Eine Softwarefunktion
allein erfüllt keine organisatorische Pflicht. Technische Tests sind kein
juristischer Nachweis.

## Nachweisregister und Abhängigkeiten

Detailnachweise: [CRA](CRA-READINESS.md), [Betrieb/NIS2](NIS2-OPERATIONS.md),
[Datenschutz](privacy/EU-OPERATING-RECORD.md),
[Barrierefreiheit](ACCESSIBILITY-ACCEPTANCE.md),
[öffentliche Dienste](PUBLIC-SERVICE-READINESS.md), [KI](AI-READINESS.md).

Die Tabellen unterscheiden die Abnahme dieser Projektunterlagen von der
Freigabe einer konkreten fremden Instanz. Unbekannte Betreiberangaben werden
nicht ergänzt oder als erfüllt angenommen.

| Issue | Zu prüfender Bereich | Nachweis vor Abnahme | Stand |
| --- | --- | --- | --- |
| #93 | Gesamteinordnung | Je Szenario Verantwortliche, betroffene Normen, Quelle/Datum, Maßnahmen und fachliche Freigabe | Roadmap erstellt; Betreiberfreigaben bleiben einsatzabhängig |
| #97 | CRA und Produkt-/Release-Sicherheit | Geschäfts-/Herstellerrollenprüfung, Bedrohungsmodell, Supportzeitraum, SBOM, Meldungsweg und gegebenenfalls Konformitätsunterlagen | Offen; technische Scan-Vorarbeit #96 |
| #98 | NIS2 und Betrieb | Betroffenheit nach Tätigkeit/Sektor/Größe, dokumentierte Betriebspflichten und Vorfallübung | Bedingte Szenarienprüfung, Betriebsplan und synthetische Übung erstellt |
| #99 | DSGVO, TDDDG und Minderjährige | Datenfluss-/Rollenmatrix, Zwecke/Rechtsgrundlagen, Information, Aufbewahrung, Betroffenenrechte, DSFA-Schwellenprüfung | Betriebsnachweis und Freigabegates erstellt; konkrete Betreiberangaben offen |
| #100 | Barrierefreiheit | Anwendbarkeit pro Angebot, echte Tastatur-/Screenreader-Abnahme mit Prüfprotokoll, zugängliche Informationen | Offen |
| #101 | DSA/DDG bei öffentlichen Inhalten | Einstufung des tatsächlichen Dienstes, Betreiberinformationen, Melde-/Moderationsablauf | Offen |
| #102 | AI Act und KI-Funktionen | Jede KI-Funktion einzeln, Rollen und Verwendungskontext, nachvollziehbare Einstufung und Nutzerinformation | Vorhandene assistierende Zwecke bewertet; neue Bewertungszwecke gesperrt |
| #103 | Aktivierungshinweise | Reproduzierbare Entscheidungsmatrix zu Funktionen/Kombinationen, Bestätigung und Audit | Offen |

**Entscheidungsprozess:** Für jeden Fall tatsächliche Fakten, zuständige
Person/Stelle, geprüfte Primärquelle, Rechtsstand, Testbeleg, offene Punkte
und Freigabedatum dokumentieren. Unbekannt gilt ausdrücklich nicht als
erfüllt. Rechtliche Einordnungen einschließlich einer etwaigen
Registrierungs-, Melde-, CE- oder Informationspflicht dürfen erst nach
qualifizierter Prüfung als Pflicht der konkret betroffenen Stelle markiert
werden. Neue Betriebsmodelle und Funktionen lösen eine Neubewertung aus.

## Amtliche Grundlagen (am 05.10.2026 geprüft)

- [Cyber Resilience Act und Rollen](https://digital-strategy.ec.europa.eu/en/policies/cyber-resilience-act)
- [CRA für Open Source](https://digital-strategy.ec.europa.eu/en/policies/cra-open-source)
- [BSI zu NIS2](https://www.bsi.bund.de/DE/Themen/Regulierte-Wirtschaft/NIS-2-regulierte-Unternehmen/nis-2-regulierte-unternehmen_node.html)
- [DSGVO](https://eur-lex.europa.eu/eli/reg/2016/679/oj/deu)
- [Bundesfachstelle Barrierefreiheit](https://www.bundesfachstelle-barrierefreiheit.de/DE/Barrierefreiheitsstaerkungsgesetz/FAQ/faq.html)
- [Digital Services Act](https://digital-strategy.ec.europa.eu/en/policies/digital-services-act)
- [AI Act](https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai)

Die Detailnachweise nennen Rechtsakte, Artikel, Rollen, Maßnahmen und
Abschlussgrenzen. Eine Entwurfsfassung oder politische Einigung ersetzt
keinen geltenden Änderungsrechtsakt.

## Begründete Arbeitsentscheidungen pro Betriebsmodell

| Szenario | Rollen und Einordnung unter den Annahmen | Zeitpunkt und offene Tatsachen |
| --- | --- | --- |
| Community ohne Monetarisierung | OSS-Ausnahme außerhalb kommerzieller Tätigkeit prüfen; AGPL allein reicht nicht. Ein Steward ist eine gesonderte gesetzliche Rolle. | Vor Finanzierung/Vertrieb Träger und Verantwortung bestimmen; [CRA](CRA-READINESS.md). |
| Kommerzielles Softwareprodukt | Herstellerprüfung einschließlich Support, Klassifizierung und gegebenenfalls CE; Betreiber bleibt für seine Daten verantwortlich. | Vor Angebot Verträge, Vertriebsländer und Supportdauer freigeben. |
| Eigener öffentlicher Dienst | Anbieter/Datenschutz-Verantwortlicher, Hosting-/Plattformrolle prüfen. SaaS nicht pauschal CRA-Produkt nennen; zugehörige Remote-Datenverarbeitungslösung gesondert. | Vor Öffnung Rechtsperson, Kontakt, Empfänger und Moderationsverfahren; [Dienstbewertung](PUBLIC-SERVICE-READINESS.md). |
| Rein private Erprobung | Keine NIS2-Einrichtung allein durch Installation; DSGVO-Haushaltsausnahme nur bei tatsächlich persönlicher/haushaltsbezogener Verarbeitung. | LAN allein genügt nicht; bei fremden Konten oder neuer Nutzung neu bewerten. |
| Schule/Unternehmen | Träger bzw. Unternehmen bestimmt Zwecke, Grundlage und Rollen; Einrichtung statt Lernsoftware nach NIS2 prüfen. | Vor echten Daten Landes-/Schulrecht, Minderjährige, Verträge und zuständige Stellen klären. |

Aktivierte Funktionen und Kombinationen verändern die Bewertung: SMTP/externe
Identitäten brauchen Empfänger-/Transferprüfung; Cloud-KI zusätzlich Zweck- und
DSFA-Prüfung; Schule mit Lernprofilen ist nicht durch die private Testinstanz
freigegeben. Elternübersicht bleibt begrenzt. Öffentliche Inhalte brauchen Rechte
und gegebenenfalls DSA-Verfahren. Kein Schalter hebt Grundpflichten auf.

Bei Verbraucherverträgen [§ 327 BGB](https://www.gesetze-im-internet.de/bgb/__327.html)
und [§ 327f BGB](https://www.gesetze-im-internet.de/bgb/__327f.html) prüfen:
Vertrags-/Widerrufsinformation, Preise, Leistungsumfang und erforderliche Updates;
auch bestimmte Daten-gegen-Leistung-Verträge können erfasst sein.
Bei SaaS [Data Act Kapitel VI](https://eur-lex.europa.eu/eli/reg/2023/2854/oj)
für den konkreten Datenverarbeitungsdienst prüfen: Wechsel, Export,
Übergangsunterstützung und Vertragsbedingungen. Keine pauschale Anwendbarkeit auf
jedes Lernprogramm. Software-, Bild-, Übersetzungs- und Katalogrechte anhand
[LICENSING](LICENSING.md), [CLA-ROLLOUT](CLA-ROLLOUT.md) und
[CATALOG-INTERCHANGE](CATALOG-INTERCHANGE.md) getrennt führen.

### Überprüfbare Abschlussgrenzen

Projektführung verantwortet Produkt/Release, Betreiber die Instanz;
Rechts-/Datenschutzfachstellen entscheiden konkrete ungeklärte Angebote.
Privates Register: Fakten, verantwortliche Rolle/Vertretung, Quellenstand,
Maßnahme, Commit/Test, Restrisiko, Freigabe und nächste Überprüfung.

- #93: Roadmap mit Szenarien, Primärquellen, Verantwortungsrollen und Folgeaufgaben.
- #94/#96: Bereits abgeschlossen; keine erneute Implementierung.
- #95: Frische Anmeldung bei Identitätsänderungen, dazu API-/Callback-Regressionen.
- #97: Kanalprüfung und vollständiges dauerhaftes Release-Nachweispaket bleiben offen.
- #98/#99: Betreiberprüfung und Nachweisverfahren vorhanden; echte Trägerdaten,
  Behördenzugänge, Verträge und gegebenenfalls DSFA bleiben einsatzabhängig.
- #100: Tatsächliche Desktop-/Mobil-Screenreader-Abnahme bleibt offen.
- #101: Allgemeines Meldesystem, Zustellung, Begründung und Beschwerdeweg fehlen.
- #102: Vorhandene assistierende Zwecke sind bewertet; eine spätere Benotung/
  Zulassung erfordert gesonderte Freigabe.
- #103: Admin-Aktivierungsdialog bleibt separater technischer Auftrag.

Offene Punkte sind keine bestandene Prüfung. Neue Rollen oder technische
Funktionen dürfen nicht mit der Abnahme dieser Unterlagen als freigegeben gelten.

## Priorisierte verbleibende Abnahmen

Die Abnahme der vorliegenden Unterlagen ersetzt keine Freigabe unbekannter
Betreiber, Provider oder Angebote. Die folgenden bestehenden Issues führen die
konkreten Abschlusskriterien; kein Punkt wird durch eine bloße Absicht erledigt.

| Priorität / Auslöser | Folgeaufgabe | Zuständige Rolle | Überprüfbarer Abschluss |
| --- | --- | --- | --- |
| P1, vor unterstütztem Produktrelease | [#97](https://github.com/kennfarbe/LearnPip/issues/97) | Projektführung / Release-Verantwortliche | Vertraulichen Kanal tatsächlich prüfen; je Release dauerhaft abrufbare SBOM mit transitiven Versionen, Lizenzen und Artefaktbezug sowie veröffentlichte Support-/Enddaten nachweisen |
| P1, vor öffentlichem Hosting von Nutzerinhalten | [#101](https://github.com/kennfarbe/LearnPip/issues/101) | Projektführung / konkreter Dienstanbieter | Allgemeinen Meldemechanismus, erforderliche Angaben, Eingangs-/Entscheidungsnachricht, Begründung, gegebenenfalls Beschwerde und Missbrauchsschutz mit synthetischen Fällen prüfen |
| P1, vor einer Barrierefreiheitszusage | [#100](https://github.com/kennfarbe/LearnPip/issues/100) | UI-Verantwortliche / tatsächliche Testpersonen | Alle sechs Kernabläufe auf Desktop und Mobilgerät mit Tastatur und realem Screenreader protokollieren; Kontrast, Reflow, Fehler, Status und Touch prüfen; Befunde beheben oder einzeln führen |
| P1, vor Aktivierung neuer externer oder öffentlicher Funktionen | [#103](https://github.com/kennfarbe/LearnPip/issues/103) | Projektführung / Instanzadmin | Funktionsabhängige Voraussetzungen vor Aktivierung anzeigen, Bestätigung und Audit testen; Betreiberfreigabe bleibt erforderlich |

Der Einsatzverantwortliche führt ergänzend das private Betriebsregister:
NIS2-Tatsachen und gegebenenfalls Behördenzugänge, Rechtsgrundlagen und
Providerverträge, erforderliche DSFA, echte Kontaktinformationen und das
Löschregister für Wiederherstellungen. Diese einsatzabhängigen Freigaben sind in
den Detailnachweisen beschrieben und bei jeder wesentlichen Änderung neu zu
prüfen. Eine Abnahme der Projektdokumentation behauptet ihre Durchführung nicht.
