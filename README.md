# LearnPip

**Little steps, lasting knowledge.**  
*Jeden Tag ein bisschen schlauer.*

LearnPip ist ein geplantes Open-Source-Projekt für kurze, regelmäßige Lerneinheiten. Lernende sollen eigene Fragen erstellen, in kleinen Schritten üben und ihren Fortschritt nachvollziehen können. Der Quellcode und die Entwicklung finden öffentlich in diesem Repository statt.

> **Projektstatus:** Konzept- und Aufbauphase. Dieses Repository enthält derzeit noch keine lauffähige Anwendung. Die hier beschriebenen Funktionen sind Ziele, keine bereits verfügbaren Produktfunktionen.

## Für wen ist LearnPip gedacht?

LearnPip richtet sich an Menschen, die Schulstoff, Ausbildungsthemen oder Prüfungsinhalte in ihrem eigenen Tempo üben möchten. Später sollen auch Lehrende und Gruppenverantwortliche gemeinsame Fragen und Lernräume verwalten können. Die erste Version konzentriert sich auf den persönlichen Lernbereich.

Der geplante Kernablauf lautet:

1. **Frage erstellen:** Eine eigene Frage mit Antwort und Erklärung anlegen.
2. **Kurz üben:** In einer kleinen Lerneinheit Fragen beantworten und die Lösung verstehen.
3. **Fortschritt sehen:** Erkennen, was schon klappt und was noch Übung braucht.

## Geplanter Umfang der ersten nutzbaren Version

Der erste Meilenstein für Nutzerinnen und Nutzer (MVP) soll Folgendes ermöglichen:

- ohne verpflichtende E-Mail-Adresse beginnen und einen Zugang wiederherstellen;
- eigene Textfragen und Bildfragen als private Inhalte anlegen;
- mit Einzel- und Mehrfachantworten üben, deren Reihenfolge variiert;
- Lösungen und Erklärungen einsehen und den eigenen Lernfortschritt verfolgen;
- die Weboberfläche auch auf einem Smartphone verwenden;
- LearnPip mit Docker Compose und PostgreSQL selbst betreiben.

Automatische KI-Auswertung, Gruppen, öffentliche Fragenkataloge und Prüfungssimulationen gehören **nicht** zum ersten nutzbaren Umfang. Auch ohne KI soll LearnPip verwendbar sein. Die konkrete Umsetzung und Reihenfolge stehen in den [GitHub-Issues](https://github.com/kennfarbe/LearnPip/issues) und [Meilensteinen](https://github.com/kennfarbe/LearnPip/milestones).

## Inhalte und Sichtbarkeit

Die folgenden Bereiche sind für spätere Ausbaustufen geplant; aktuell gibt es noch keine Freigabe- oder Veröffentlichungsfunktion:

| Bereich | Geplantes Verhalten |
| --- | --- |
| Private Fragen und Fotos | Nur für die berechtigte Person sichtbar; keine automatische Veröffentlichung. |
| Geschlossene Gruppen | Für Mitglieder der jeweiligen Gruppe verfügbar, mit eigenen Rollen und Freigaben. |
| Öffentlicher Fragenpool | Nur nach ausdrücklicher Freigabe und Prüfung von Inhalt und Nutzungsrechten zugänglich. |

Die Lizenz für Programmcode regelt nicht automatisch die Rechte an hochgeladenen Aufgaben, Bildern oder anderen Nutzerinhalten. Für öffentliche Inhalte ist eine gesonderte, verständliche Lizenzentscheidung vorgesehen.

## Technischer Ansatz

Geplant ist ein gemeinsames Repository mit getrennten Projekten und Docker-Images für eine ASP.NET-Core-API auf .NET 10, einen Hintergrunddienst und ein Angular-Webfrontend. PostgreSQL soll die Daten speichern. Die Anmeldung soll OpenID Connect für externe Identitätsanbieter unterstützen; weitere Zugangswege für die persönliche Nutzung sind vorgesehen. Eine versionierte API soll später auch von einer mobilen App genutzt werden können.

Der technische Aufbau wird in den zugehörigen Issues konkretisiert; derzeit ist noch keiner dieser Dienste implementiert.

## Roadmap

| Phase | Ziel |
| --- | --- |
| M0 – Projektregeln | Mission, Lizenzgrenzen, Beiträge, Sicherheit und Datenschutzgrundlagen klären. |
| M1 – Technisches Fundament | Entwicklungsumgebung, API, Datenbank, Anmeldung und Container aufbauen. |
| M2 – Nutzbares MVP | Private Fragen, Lerneinheiten und Fortschritt umsetzen. |
| M3 – Zusammenarbeit | Gruppen und einen moderierten öffentlichen Fragenpool ergänzen. |
| M4 – Prüfungsvorbereitung | Lernziele, Simulationen und gezieltes Üben ermöglichen. |
| M5 – Optionale KI | Fotoerkennung und Lösungshilfen mit wählbarer KI-Betriebsart ergänzen. |
| M6 – Ausbau | Benachrichtigungen, weitere Zugänge und Betriebsfunktionen ausbauen. |

Der [Issue-Tracker](https://github.com/kennfarbe/LearnPip/issues) enthält die einzelnen Arbeitspakete und ihren aktuellen Stand. Die Phasen sind eine geplante Reihenfolge, keine Zusage für Veröffentlichungstermine.

## Open Source und Beiträge

**LearnPip soll dauerhaft Open Source bleiben.** Für den Programmcode ist die **GNU AGPL-3.0** vorgesehen. Eine Lizenzdatei und die genauen Lizenzgrenzen werden in [Issue #2](https://github.com/kennfarbe/LearnPip/issues/2) eingerichtet. Bis diese Lizenz im Repository liegt, darf aus dieser Absicht keine bereits erteilte Open-Source-Lizenz für den vorhandenen Inhalt abgeleitet werden.

Daneben ist eine **optionale individuelle kommerzielle Lizenz** für Unternehmen vorgesehen, die andere Lizenzbedingungen benötigen. Die AGPL erlaubt ihrerseits auch kommerzielle Nutzung unter ihren Bedingungen; eine Firma muss also nicht allein wegen einer entgeltlichen Nutzung eine Zweitlizenz erwerben. Der offizielle Projektcode soll öffentlich unter AGPL verfügbar bleiben, auch wenn eine separate Firmenlizenz vergeben wird.

Für angenommene externe Codebeiträge ist eine Contributor License Agreement (CLA) als Rechteeinräumung geplant: Beitragende behalten ihr Urheberrecht und räumen der Projektleitung die Rechte ein, ihre Beiträge sowohl unter AGPL als auch unter individuellen kommerziellen Lizenzen anzubieten. Der rechtlich geprüfte Text und der Beitragsprozess sind Gegenstand von [Issue #3](https://github.com/kennfarbe/LearnPip/issues/3). Bitte vor der Annahme fremder Codebeiträge diese Regelung abschließen.

Du möchtest mithelfen? Sieh dir die [offenen Issues](https://github.com/kennfarbe/LearnPip/issues) an und diskutiere eine Idee dort, bevor du größere Änderungen beginnst. Eine ausführliche Beitragsanleitung folgt mit Issue #3. Für Sicherheitsmeldungen wird ein eigener Meldeweg in [Issue #4](https://github.com/kennfarbe/LearnPip/issues/4) eingerichtet.
