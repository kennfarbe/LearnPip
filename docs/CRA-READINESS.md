# Produktsicherheit, Support und CRA-Nachweise

Prüfdatum: **05.10.2026**. Erste fachlich zu überprüfende Fassung zu #97.
Keine CE-Erklärung; die [Szenarioprüfung](EU-READINESS.md) geht vor.

## Anwendbarkeit und Fristen

Nicht monetarisierte Community-Entwicklung außerhalb kommerzieller Tätigkeit
kann unter die OSS-Ausnahme fallen. Kommerzielle Bereitstellung verlangt eine
gesonderte Herstellerprüfung. AGPL ist keine pauschale Ausnahme. Steward ist
eine eigene gesetzliche Rolle einer juristischen Person mit dauerhafter
Unterstützung entsprechender OSS; einzelne Beitragende sind nicht automatisch
Hersteller oder Steward. Reines Hosting ist getrennt von einem Produkt samt
zugehöriger Remote-Datenverarbeitungslösung zu prüfen.

Amtliche Grundlagen: [CRA, Art. 2, 3, 13, 24, 32 und 71](https://eur-lex.europa.eu/eli/reg/2024/2847/oj),
[Kommission: Open Source](https://digital-strategy.ec.europa.eu/en/policies/cra-open-source).
Hersteller-Meldungen gelten seit **11.09.2026**, Hauptpflichten ab
**11.12.2027**; Steward-Meldungen nach Art. 24 Abs. 3 ab **11.12.2027**.
Bei CRA-Herstellerpflichten ist der Support regelmäßig mindestens fünf Jahre,
bei kürzer erwarteter Nutzung entsprechend kürzer; erwartete Nutzungsdauer und
weitere gesetzliche Kriterien müssen die Entscheidung tragen (Art. 13 Abs. 8).
Die aktuelle technische `latest`-Pflege erfüllt diese Begründung nicht.

## Bedrohungsmodell und Sicherheitsanforderungen

Geschützt werden Sitzungen, Geheimnisse, private Medien, Lernstände,
Sorgeberechtigungsnachweise, Moderation, Release- und Updatekette. Grenzen liegen
zwischen Browser/API, Konto/Konto, API/PostgreSQL, Instanz/Provider sowie
CI/Registry/Installationshost. Bedrohungen umfassen fremde Objektkennungen,
übernommene Sitzungen, schädliche Archive, Provider-Ausgaben und kompromittierte
Abhängigkeiten. Betreiberzugriff auf den Host bleibt außerhalb der normalen API.

| Gefahr | Vorhandene Maßnahme und Nachweis | Verbleibende Grenze |
| --- | --- | --- |
| Zugriff auf fremde Inhalte | Datenbankbasierte Objekt-/Rollenprüfung; `ApiV1Tests`, `VisibilityTests`, `FamilyFlowTests` | Moderationszugriff auf private Fragen ist gesondert privilegiert, nicht gewöhnlicher Lernzugang |
| Kontoübernahme/CSRF | Gehashte Geheimnisse, HttpOnly/Secure-Cookie, Origin-Prüfung, Rate-Limits; `IdentityFlowTests`, `PasswordFlowTests` | Host/Browser und Wiederherstellungsgeheimnis müssen geschützt bleiben |
| Verknüpfung mit alter Sitzung | Höchstens 15 Minuten alte Sitzung; Start, Abschluss und Trennung prüfen; kein Verjüngen durch Rotation | Keine MFA-Zusage; erneut lokal oder beim gewählten Anbieter authentifizieren |
| Archive/Medien | Größen-/Pfad-/Prüfsummenprüfung, vollständiger Import, Bildbereinigung, Eigentumsprüfung; Katalogtests | Keine automatische Rechteprüfung am geistigen Eigentum |
| Externe KI | `off`, Eingabe-/Bildlimits, Quoten, kein Fallback, menschliche Prüfung; `AiProviderTests` | Anbieterbindung und Verträge liegen beim Betreiber |
| Lieferkette/Updates | Gepinnte Actions, unveränderliche Release-Tags, Scans, Container-Digests, Installer-/Integrationstests | Prüfsummen ersetzen keine Signatur; kompromittierter Herausgeber/Registry und Wiederherstellung müssen im Betriebsrisiko stehen |

Sicherheitsanforderungen werden mit der Änderung und dem zugehörigen PR geprüft.
Keine Warnungsunterdrückung oder Befundausnahme ohne bestehende ausdrückliche
Projektentscheidung. Die [Scanregeln](SECURITY-AUDITS.md) bleiben unverändert.

## Release-Nachweise und technische Supportpolitik

Derzeit wird nur das neueste stabile Release gepflegt;
`security/supported-releases.json` ist maßgeblich. Eine ältere Linie hat keine
zugesicherte Pflege. Mit Veröffentlichung einer Folgeversion endet ihre reguläre
technische Unterstützung. Das ist keine Abkürzung für CRA-/Vertragspflichten:
vor kommerzieller Bereitstellung feste Beginn-/Enddaten und die Supportdauer
entscheiden, veröffentlichen und finanziell/personell absichern.

Je Release führt die Projektführung: Tag, Commit, API-/Worker-/Web-/DB- und
Build-Image-Digests je Architektur, Scanzeit, Befund-/Behebungsreferenzen,
Lizenzprüfung und Testlauf. `sbom: true` im Publishing erzeugt maschinenlesbare
Image-SBOM-Attestierungen. Diese müssen am konkreten Digest abrufbar sein;
transitive npm-/NuGet- und OS-Komponenten und fehlende Lizenzangaben sind zu
prüfen. Eine SBOM-Datei ohne Artefaktbezug oder mit unbekanntem Scanumfang ist
kein vollständiger Nachweis. Noch fehlt ein verifiziertes, dauerhaft archiviertes
Gesamtpaket aller Release-Nachweise; dies bleibt Abschlussgrenze von #97.

Die Projektführung überprüft beim Release die Klassifizierung nach Anhängen III/IV.
Eine Lernanwendung wird nicht allein wegen ihrer Anmeldung zu einem wichtigen
Identitätsprodukt. Für eine anwendbare Kategorie folgen technische Dokumentation,
Bewertungsverfahren nach Art. 32, EU-Konformitätserklärung und gegebenenfalls CE
vor Marktbereitstellung. Kein generelles externes OSS-Zertifikat voraussetzen.

## Vertrauliche Bearbeitung und koordinierte Offenlegung

Projektführung übernimmt Triage, Release-Koordination und Betreiberinformation;
eine Vertretung ist im privaten Register zu benennen. Meldeweg:
[SECURITY.md](../SECURITY.md). GitHub Private Vulnerability Reporting muss vor
einem zugesagten verlässlichen Kanal auf tatsächliche Erreichbarkeit geprüft
werden; keine unbestätigte Mailadresse erfinden. Zugangsdaten und Exploits gehören
nicht in öffentliche Tickets. Bestehende öffentliche Audit-Artefakte sind ein
separater, ausdrücklich beschlossener Scannerumfang, kein vertraulicher Meldeweg.

Interne Ziele: Eingang binnen zwei Arbeitstagen bestätigen, binnen fünf
Arbeitstagen Umfang/Version/Schwere zuordnen, dann Fix, Test, Betreiberhinweis und
abgestimmte Offenlegung koordinieren. Ziele sind keine Garantie und verändern
gesetzliche Meldungsfristen nicht. Nachweis: private Fallkennung, Zeitpunkte,
betroffene Digests, Maßnahmen, erneuter Scan, Freigabe und Rückmeldung.

Bei Herstellerpflichten und aktiv ausgenutzter Schwachstelle oder schwerem
Sicherheitsvorfall separate Fristen starten: 24 Stunden Frühwarnung, 72 Stunden
Meldung über die ENISA-SRP; bei Schwachstellen Abschluss binnen 14 Tagen nach
verfügbarer Korrektur, bei Vorfällen binnen eines Monats nach der 72h-Meldung.
Nicht jede CVE ist aktiv ausgenutzt. Zuständiges CSIRT, SRP-Zugang und Vertretung
vorher klären. [Kommission: Meldungen](https://digital-strategy.ec.europa.eu/en/policies/cra-reporting).
Die [synthetische Übung](NIS2-OPERATIONS.md#synthetische-vorfallübung) prüft die
unterschiedlichen Uhren ohne echte Behördenmeldung.
