# EU-Produktreife: Prüfmatrix und Freigabegates

**Arbeitsstand vom 03.10.2026 – offen, keine Rechts- oder Konformitätsbestätigung.**
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

| Issue | Zu prüfender Bereich | Nachweis vor Abnahme | Stand |
| --- | --- | --- | --- |
| #93 | Gesamteinordnung | Je Szenario Verantwortliche, betroffene Normen, Quelle/Datum, Maßnahmen und fachliche Freigabe | Offen |
| #97 | CRA und Produkt-/Release-Sicherheit | Geschäfts-/Herstellerrollenprüfung, Bedrohungsmodell, Supportzeitraum, SBOM, Meldungsweg und gegebenenfalls Konformitätsunterlagen | Offen; technische Scan-Vorarbeit #96 |
| #98 | NIS2 und Betrieb | Betroffenheit nach Tätigkeit/Sektor/Größe, dokumentierte Betriebspflichten und Vorfallübung | Offen |
| #99 | DSGVO, TDDDG und Minderjährige | Datenfluss-/Rollenmatrix, Zwecke/Rechtsgrundlagen, Information, Aufbewahrung, Betroffenenrechte, DSFA-Schwellenprüfung | Offen |
| #100 | Barrierefreiheit | Anwendbarkeit pro Angebot, echte Tastatur-/Screenreader-Abnahme mit Prüfprotokoll, zugängliche Informationen | Offen |
| #101 | DSA/DDG bei öffentlichen Inhalten | Einstufung des tatsächlichen Dienstes, Betreiberinformationen, Melde-/Moderationsablauf | Offen |
| #102 | AI Act und KI-Funktionen | Jede KI-Funktion einzeln, Rollen und Verwendungskontext, nachvollziehbare Einstufung und Nutzerinformation | Offen |
| #103 | Aktivierungshinweise | Reproduzierbare Entscheidungsmatrix zu Funktionen/Kombinationen, Bestätigung und Audit | Offen |

**Entscheidungsprozess:** Für jeden Fall tatsächliche Fakten, zuständige
Person/Stelle, geprüfte Primärquelle, Rechtsstand, Testbeleg, offene Punkte
und Freigabedatum dokumentieren. Unbekannt gilt ausdrücklich nicht als
erfüllt. Rechtliche Einordnungen einschließlich einer etwaigen
Registrierungs-, Melde-, CE- oder Informationspflicht dürfen erst nach
qualifizierter Prüfung als Pflicht der konkret betroffenen Stelle markiert
werden. Neue Betriebsmodelle und Funktionen lösen eine Neubewertung aus.

## Quellen zur weiteren Prüfung (nicht abschließend)

- [Cyber Resilience Act und Rollen](https://digital-strategy.ec.europa.eu/en/policies/cyber-resilience-act)
- [CRA für Open Source](https://digital-strategy.ec.europa.eu/en/policies/cra-open-source)
- [BSI zu NIS2](https://www.bsi.bund.de/DE/Themen/Regulierte-Wirtschaft/NIS-2-regulierte-Unternehmen/nis-2-regulierte-unternehmen_node.html)
- [DSGVO](https://eur-lex.europa.eu/eli/reg/2016/679/oj/deu)
- [Bundesfachstelle Barrierefreiheit](https://www.bundesfachstelle-barrierefreiheit.de/DE/Barrierefreiheitsstaerkungsgesetz/FAQ/faq.html)
- [Digital Services Act](https://digital-strategy.ec.europa.eu/en/policies/digital-services-act)
- [AI Act](https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai)

Diese Quellen sind Ausgangspunkte für die noch ausstehende konkrete Prüfung
und keine Behauptung einer bereits erteilten rechtlichen Freigabe.
