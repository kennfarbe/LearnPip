# CLA – Einführungs- und Rechtsprüfliste

**Erstfassung V0.1 vom 03.10.2026 – Entwurf, nicht aktiv.** Die hier verlinkten
[Einzel-](CLA-INDIVIDUAL-DRAFT.md) und
[Organisationsvereinbarungen](CLA-ENTITY-DRAFT.md) sind **keine geprüften,
unterschriftsreifen Verträge**. Diese Liste ersetzt keine anwaltliche Beratung.
Es darf keine Vertragsannahme oder automatische Rechtefreigabe aus den Entwürfen
abgeleitet werden. Issue [#3](https://github.com/kennfarbe/LearnPip/issues/3)
bleibt offen, bis sämtliche Abnahmekriterien tatsächlich erfüllt sind.

## Vor jeder Aktivierung rechtlich und organisatorisch zu klären

- [ ] Qualifizierte juristische Prüfung anhand der tatsächlich betroffenen Rechtsordnungen, der geplanten Verwertung und der konkreten Projektträgerschaft; Erstfassung gegebenenfalls überarbeiten und erneute Freigabe festhalten.
- [ ] Den **wirklichen** Rechteempfänger und ggf. Rechtsform, vertretungsberechtigte Person, ladungsfähige Anschrift und Kontakt vollständig ermitteln. Aus dem GitHub-Benutzernamen folgt keine Rechtsidentität.
- [ ] Vertragssprache, eventuell anwendbares Recht, zulässigen Gerichtsstand, Vertragsabschluss und Beweisführung rechtlich festlegen. Keine Gerichtsstands- oder Rechtswahlklausel ins Blaue hinein aufnehmen.
- [ ] Individuelle und Organisations-CLA einschließlich Arbeits-/Dienstverhältnis, Miturheber, Auftraggeber, externe Dienstleister, Rechtekette, Vertretungsbefugnis, Urheberpersönlichkeitsrechte, Nutzungsarten und erforderlicher Einwilligung in Unterlizenzierung prüfen.
- [ ] Grenzen der Lizenzierung **unbekannter Nutzungsarten** und etwaige Formerfordernisse prüfen; Entwurf schließt diese derzeit ausdrücklich aus. Zwingende Vergütungs-, Auskunfts-, Rückrufs- und Verbraucherrechte je nach Werkart und Beteiligten prüfen, statt pauschale Verzichtsklauseln zu verwenden.
- [ ] Patentregelung mit fachkundiger Beratung überprüfen; insbesondere keine Schutzrechte eines Arbeitgebers, Dritter oder Konzernunternehmens fingieren.
- [ ] Geeignetes Verfahren für minderjährige Beitragende samt erforderlicher Zustimmung/Vertretung erst nach rechtlicher Prüfung vorsehen; bis dahin keine externen Codebeiträge Minderjähriger annehmen.
- [ ] Vor Unterzeichnung die dauerhafte AGPL-Veröffentlichung des in offiziellen Versionen enthaltenen Programmcodes sowie den parallelen zusätzlichen kommerziellen Lizenzweg klar erklären; fremde Komponenten und Fragenkataloge lizenzrechtlich abgrenzen.
- [ ] Gesonderte und vollständige Datenschutzhinweise für CLA-Daten erstellen: Verantwortlicher, Zwecke, Rechtsgrundlagen, erhobene Identifikatoren und Signaturnachweise, Empfänger/Dienstleister, etwaige Übermittlungen, Aufbewahrung und Betroffenenrechte.
- [ ] Welche Nachweise für wer, wann, welche **exakte Vertragsfassung** und welchen **konkreten Beitragsumfang** akzeptiert hat, datensparsam und zugriffsbeschränkt dokumentieren; Lösch- und Aufbewahrungsfristen erst nach Prüfung festlegen.
- [ ] Endgültige, versionierte Dokumente und Datenschutzinformationen **vor** jeder Annahme bereitstellen; bestehende Entwürfe nicht nachträglich als bereits unterzeichnet behandeln.

## Technische Einführung / verpflichtendes Merge-Gate

- [ ] Dienst zur CLA-Annahme (zum Beispiel CLA Assistant) erst nach Prüfung von Anbieter, Berechtigungen, Datenverarbeitung und Signaturnachweis auswählen und gesondert einrichten; hier wird **keine Installation oder Konfiguration behauptet**.
- [ ] Den Dienst ausschließlich auf die rechtlich **freigegebene** Vertragsfassung konfigurieren, ausdrücklich **nicht** auf die Entwurfsdateien.
- [ ] Auf dem tatsächlich verwendeten Hauptbranch eine verpflichtende erfolgreiche CLA-Statusprüfung plus Pull-Request-Review einstellen und einen unbeabsichtigten Bypass ausschließen.
- [ ] Tests dokumentieren: unbekannte externe Person vor Zustimmung blockiert; nach wirksamer Zustimmung korrekt zugeordnet; weitere Commits und geänderte Vertragsfassung geprüft; Organisation und konkrete berechtigte Personen korrekt zugeordnet; fehlende Arbeitgeber-/Drittrechte nicht stillschweigend freigegeben; Bots/Projektverantwortliche gesondert berücksichtigt.
- [ ] Die Akzeptanz eines allgemeinen CLA-Textes ersetzt **nicht** die projektbezogene Prüfung von Copyright, Drittkomponenten, Lizenzen, Patenten und Herkunft im PR.
- [ ] CONTRIBUTING, README und LICENSING erst dann von „Entwurf“ auf „aktiv“ umstellen, wenn Freigabe, Datenschutzinformationen, unterschriebene/akzeptierte Endfassung und Testprotokoll nachweisbar sind.

**Abnahmestopp:** Externer Code darf bis dahin nicht in das offizielle Projekt gemergt werden. Dies ist keine Behauptung, dass GitHub selbst bereits technisch gegen alle Merge-Wege gesichert ist.

## Zu überprüfende Primärquellen (keine pauschale Aussage über Einzelfallgeltung)

- [§§ 29, 31, 31a, 32, 34, 35, 69a Abs. 5 und 69b UrhG](https://www.gesetze-im-internet.de/urhg/): Urheberrecht, einfache Rechte, bekannte/unbekannte Nutzungen, Vergütung, Übertragung/Unterlizenzierung, Software in Arbeitsverhältnissen.
- [§§ 106–108, 306 und 307 BGB](https://www.gesetze-im-internet.de/bgb/): Minderjährige, Teilunwirksamkeit und AGB-Kontrolle; § 306 Abs. 3 (unzumutbare Härte) ausdrücklich beachten.
- [DSGVO, insbesondere Art. 13](https://eur-lex.europa.eu/eli/reg/2016/679/oj/deu): Informationspflichten bei Signatur- und Identitätsdaten.
- [eIDAS-Verordnung, insbesondere Art. 25](https://eur-lex.europa.eu/eli/reg/2014/910/oj/deu): Beweiswert elektronischer Signaturen; **besondere gesetzliche Formerfordernisse** bleiben gesondert zu prüfen.
- [Vollständiger offizieller Lizenztext AGPL-3.0](https://www.gnu.org/licenses/agpl-3.0.html): Open-Source-Lizenz des offiziellen Programmcodes.

Die Auflistung ist ein Ausgangspunkt für die erneute Prüfung, keine abschließende rechtliche Bewertung und keine Aussage, dass alle Normen in jeder Konstellation gleichermaßen anwendbar wären.
