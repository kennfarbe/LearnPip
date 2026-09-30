# LearnPip

**Little steps, lasting knowledge.**  
*Jeden Tag ein bisschen schlauer.*

LearnPip ist ein geplantes Open-Source-Projekt für kurze, regelmäßige Lerneinheiten. Lernende sollen eigene Fragen erstellen, in kleinen Schritten üben und ihren Fortschritt nachvollziehen können. Der Quellcode und die Entwicklung finden öffentlich in diesem Repository statt.

> **Projektstatus:** Konzept- und Aufbauphase. Das Repository enthält inzwischen ein startbares technisches Grundgerüst, aber noch keine nutzbare Lernfunktion. Die hier beschriebenen Produktfunktionen sind Ziele, keine bereits verfügbaren Funktionen.

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

Die Code-Lizenz erteilt keine Rechte an hochgeladenen Aufgaben, Antworten, Erklärungen, Fotos oder anderen Nutzerinhalten. Öffentliche Inhalte benötigen eine gesonderte, verständliche Freigabe und Lizenzentscheidung.

## Technischer Ansatz

Geplant ist ein gemeinsames Repository mit getrennten Projekten und Docker-Images für eine ASP.NET-Core-API auf .NET 10, einen Hintergrunddienst und ein Angular-Webfrontend. PostgreSQL soll die Daten speichern. Die Anmeldung soll OpenID Connect für externe Identitätsanbieter unterstützen; weitere Zugangswege für die persönliche Nutzung sind vorgesehen. Eine versionierte API soll später auch von einer mobilen App genutzt werden können.

Der technische Aufbau mit [C1-Systemkontext, C2-Containerübersicht und ADRs](docs/architecture/README.md) beschreibt die Architektur. Den lokalen Stack kannst du mit der [Entwicklungsanleitung](docs/DEVELOPMENT.md) starten; für eine veröffentlichte Version gibt es die [Installation und Aktualisierung aus einem Release](docs/RELEASE-INSTALL.md), für Proxmox die [Schritt-für-Schritt-Installation](docs/PROXMOX.md) und für den laufenden Betrieb die [Betriebsanleitung](docs/OPERATIONS.md). Das [Datenmodell und die Wiederherstellung](docs/DATABASE.md) sind ebenfalls dokumentiert. Der [API-v1-Vertrag](docs/API.md) beschreibt Endpunkte und Berechtigungen; [Konten und Identitätswege](docs/IDENTITY.md) erläutert die Anmeldung.

Die [optionalen KI-Betriebsarten](docs/AI_PROVIDERS.md) sind standardmäßig deaktiviert und beschreiben Provider, Datenweg und Kostenlimits vor einer Anfrage.

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

Der **Programmcode dieses Projekts** wird unter **GNU AGPL-3.0-only** lizenziert. Die Lizenz gilt für die Software, die im Repository als Teil des Programmcodes gekennzeichnet ist. Die [Datei LICENSE](LICENSE) nennt die Lizenz und verweist auf ihren vollständigen offiziellen Text. Das AGPL-Netzwerk-Copyleft gilt nach den Bedingungen der Lizenz auch bei Nutzung einer modifizierten Version über ein Netzwerk.

Eine **optionale individuelle kommerzielle Lizenz** kann für Code angeboten werden, für den die Projektleitung die dafür nötigen Rechte besitzt. Die AGPL erlaubt auch kommerzielle Nutzung unter ihren Bedingungen; eine Firma benötigt daher nicht allein wegen entgeltlicher Nutzung eine Zweitlizenz. Der offizielle Projektcode soll weiterhin öffentlich unter AGPL verfügbar bleiben. Rechte an fremden Beiträgen können erst dann in eine zusätzliche Lizenz einbezogen werden, wenn sie ausdrücklich und wirksam eingeräumt wurden; der CLA-Prozess ist in [Issue #3](https://github.com/kennfarbe/LearnPip/issues/3) vorgesehen.

Die [Lizenzübersicht](docs/LICENSING.md) grenzt Programmcode von Dokumentation, Marke, Abhängigkeiten und Nutzerinhalten ab. Aufgaben, Antworten, Fotos und andere Nutzerinhalte werden durch die Code-Lizenz nicht automatisch an LearnPip lizenziert. Eine öffentliche Inhaltslizenz muss separat gewählt und vor einer Veröffentlichung angezeigt werden.

Du möchtest mithelfen? Sieh dir die [offenen Issues](https://github.com/kennfarbe/LearnPip/issues) an und diskutiere eine Idee dort, bevor du größere Änderungen beginnst. Eine ausführliche Beitragsanleitung folgt mit Issue #3. Für Sicherheitsmeldungen wird ein eigener Meldeweg in [Issue #4](https://github.com/kennfarbe/LearnPip/issues/4) eingerichtet.

## Lizenzhinweis für spätere gehostete Installationen

Das Repository enthält noch keine lauffähige Anwendung. Sobald LearnPip über ein Netzwerk betrieben werden kann, soll die Oberfläche einen gut sichtbaren Link zum passenden Quellcode-Stand und zur Lizenz enthalten. Der Link muss zur Version passen, die der Betreiber tatsächlich einsetzt.

[Datenexport, Selbstlöschung und Missbrauchsschutz](docs/DATA-RIGHTS.md)
