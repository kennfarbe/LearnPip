# Datenschutz-Betriebsnachweis und Information für Lernende

Prüfdatum **05.10.2026**, erste fachlich zu prüfende Fassung zu #99.
Die Rollen und Zwecke sind pro tatsächlicher Instanz auszufüllen. Dies ist ein
Nachweis-/Informationstext, keine erfundene Datenschutzerklärung eines Anbieters.

## Rollen, Datenflüsse und Rechtsgrundlagen

| Vorgang | Daten/Zweck/Empfänger | Rolle und vor Betrieb zu prüfende Grundlage |
| --- | --- | --- |
| Private Selbstnutzung | Eigene Fragen, Bilder, Antworten und Geräte-/Zugangsdaten in der eigenen Instanz | Persönlich/haushaltsbezogene Ausnahme nur bei tatsächlichem Umfang; ein LAN allein belegt sie nicht |
| Gehostetes Lernkonto | Pseudonym, optionale E-Mail, Anmeldeidentitäten, Lerninhalte und Statistik beim Betreiber/Hoster | Betreiber bestimmt Zwecke; Hoster gegebenenfalls Auftragsverarbeiter. Art. 6-Grundlage je Zweck, nicht pauschal Einwilligung |
| Schule/Unternehmen | Lernende, Gruppenzugang und freigegebene Inhalte beim Träger/Unternehmen | Trägerverantwortung, Landes-/Schulrecht bzw. Beschäftigtendatenschutz prüfen; Auftragnehmer nach tatsächlicher Rolle |
| E-Mail und externe Anmeldung | Verifizierte Adresse bzw. Provider-/Subject-Schlüssel; gewählter Mail-/Identitätsanbieter | Empfänger, eigene/auftragsbezogene Rolle, Verträge und Transfer je Anbieter bestimmen |
| Familienübersicht | Nach geprüfter Verknüpfung aggregierter Fortschritt und Ziele | Kein Zugriff auf private Fragen oder Einzelantworten; Sorgeberechtigung außerhalb der Software prüfen |
| KI-Text/Foto | Nur ausdrücklich gewählte Eingabe/Zuschnitt zum konfigurierten Modell | Anbieter, Speicher-/Trainingsbedingungen, Rechtsgrundlage, AV-Vertrag und Transfer vor Aktivierung |
| Moderation | Inhalt, Version, Meldung und Prüfzweck für gesondert Berechtigte | Moderationszweck, Zugriffsumfang, Information und Audit festlegen; privilegierter Zugriff bleibt personenbezogen |

Gesetzliche Grundlage: [DSGVO](https://eur-lex.europa.eu/eli/reg/2016/679/oj),
insbesondere Art. 6, 8, 12–22, 28, 32–35 und 44 ff. Ein Pseudonym ist nicht
automatisch anonym. Art. 8 betrifft einwilligungsbasierte, direkt angebotene
Dienste für Kinder, kein allgemeines Verbot unter 16 Jahren. Zielland und
abweichende nationale Altersgrenze prüfen; Schule braucht ihre eigene Grundlage.

## Datenarten, Fristen und tatsächliche Umsetzung

| Daten | Iststand | Betriebsentscheidung vor organisatorischem/öffentlichem Einsatz |
| --- | --- | --- |
| Sitzung | 30 Tage, Widerruf bei Logout/Rotation/Deaktivierung; Tokens nur gehasht | Zugriff auf Backups und technische Sitzungsmetadaten begrenzen |
| E-Mail-Code | 10 Minuten gültig, einmalig, höchstens fünf Fehlversuche, HMAC | Gültigkeit ist keine Zusage physischer Löschung aller Challenge-Zeilen |
| Konto/Inhalte/Medien/Lernstand | Inaktivität: Deaktivierung nach 90 Tagen, Löschung nach weiteren 90; Moderation/Admin ausgenommen | Privilegierte Konten gesondert überprüfen; gelöschte Fragen/alte Versionen nicht als bereits physisch entfernt behaupten |
| Backup | Lokale Datenbankdumps laufen nach 30 Tagen aus | Externe Kopien und Schlüsselbackups mit gleicher Frist und Zugriffskontrolle führen |
| Moderations-/Adminaudit | Akteur, Objekt, Aktion, Zweck und Zeitpunkt vorhanden | Vorgabe: 90 Tage nach abgeschlossenem Vorgang überprüfen/löschen, längere notwendige Beweissicherung begründen; automatische Fristbereinigung noch nicht nachgewiesen |
| Technische Logs | Keine bewusste Inhalts-/Tokenprotokollierung; Host/Proxy konfigurieren | Vorgabe: sieben Tage Standard, begründete Sicherheitsfälle maximal 30 Tage; tatsächliche Logrotation vor Freigabe prüfen |
| Providerdaten | Nicht durch LearnPip löschbar; keine Providerfristen erfunden | Konkrete Aufbewahrung, Unterauftragnehmer, Region, Löschung und Trainingsnutzung vertraglich klären |

Die letzten beiden Fristvorgaben sind überprüfbare Betreiberanforderungen,
keine Behauptung einer bereits vorhandenen automatischen Bereinigung.
Für sofortige Kontolöschung muss ein privates Löschregister verhindern, dass
ein Restore jüngerer Backups ein gelöschtes Konto wieder produktiv freigibt.
Ein Inaktivitätslauf allein erkennt diese freiwillige Löschung nicht zuverlässig.

## Betroffenenrechte und Nachweise

Der [JSON-Export](../DATA-RIGHTS.md) enthält eigene Daten, keine Geheimnisse;
Bilder sind separat authentifiziert abrufbar. [ApiV1Tests](../../tests/LearnPip.Data.Tests/ApiV1Tests.cs)
prüfen Zugriff und Löschung, [AccountLifecycleTests](../../tests/LearnPip.Data.Tests/AccountLifecycleTests.cs)
Inaktivität und abhängige Daten; [FamilyFlowTests](../../tests/LearnPip.Data.Tests/FamilyFlowTests.cs)
begrenzten Familienzugriff. Verifizierung einer E-Mail und Änderung eigener
Fragen unterstützen Berichtigung; falsche Altersgruppen brauchen administrative
Prüfung. Ein Export allein erfüllt nicht jedes Auskunftsrecht, etwa Empfänger,
Rechtsgrundlagen und Speicherdauer. Anfragen daher zusätzlich beim Betreiber
prüfen, Identität angemessen nachweisen und Fristen nach Art. 12 beachten.

Widerruf freiwilliger KI-/Erinnerungsnutzung und Familienverknüpfung wirkt für
zukünftige Verarbeitung. Bereits rechtmäßig verteilte öffentliche Kopien können
nicht technisch zurückgeholt werden; rechtliche Lösch-/Informationspflichten
sind trotzdem gesondert zu bewerten. [Familienverfahren](../FAMILY.md).

## Verständliche Information

Vor Kontoanlage muss der konkrete Betreiber seine Identität, erreichbaren
Kontakt, gegebenenfalls Datenschutzstelle, Zwecke/Grundlagen, Empfänger,
Fristen, Transfers, Rechte und zuständige Aufsicht veröffentlichen. Für
optionale Anbieter die Information vor der Auswahl bereitstellen. Keine
Adresse oder Vertragszusage aus dieser Vorlage ungeprüft übernehmen.

**Für Erwachsene:** LearnPip speichert deinen Zugang und deine Lerninhalte in
der gewählten Instanz. E-Mail, externe Anmeldung und KI sind zusätzliche Wege.
Private Inhalte sind nicht automatisch öffentlich. Besonders berechtigte
Moderierende können Inhalte nach einem gesonderten Prüfverfahren einsehen.
Du kannst deine Daten exportieren und dein Konto löschen; sichere vorher das
Wiederherstellungsgeheimnis. Bereits verteilte Kopien sind außerhalb der Instanz.
Der Betreiber erklärt dir, welche Funktionen er anbietet und an wen Daten gehen.

**Für Kinder:** Deine Fragen und Bilder bleiben zuerst privat. Teile keine
Adresse, Passwörter oder Bilder anderer Personen. Wenn du ein Bild an eine KI
senden willst, prüfe zuerst mit einer erwachsenen Person, ob das erlaubt ist.
Eine KI kann falsch antworten. Ein Elternzugang sieht nur die freigegebene
Zusammenfassung, nicht alle deine Antworten oder privaten Bilder. Frage deinen
Betreiber, wenn du etwas ändern, herunterladen oder löschen möchtest. Teile
deinen geheimen Zugang nicht mit deiner Klasse. Eine Einladung allein beweist
nicht, dass jemand deine sorgeberechtigte Person ist.

## Storage, Anbieter und DSFA-Schwellenprüfung

Iststand im Web: HttpOnly-Sitzung, OIDC-Korrelation/Nonce, lokale Sprach-/Theme-
Wahl, SessionStorage-Lernsitzungskennung und statischer PWA-Cache. Keine
Lern-API-Antworten im Service-Worker-Cache. [§ 25 TDDDG](https://www.gesetze-im-internet.de/ttdsg/__25.html)
trennt Einwilligung von unbedingt notwendigen Vorgängen für einen ausdrücklich
gewünschten Dienst. Pro Speicherzweck Notwendigkeit prüfen; nicht jeden
LocalStorage automatisch als notwendig behandeln. Kein pauschaler Banner
ersetzt diese Prüfung; zusätzliches Tracking benötigt eine neue Entscheidung.

Die Schwellenprüfung beurteilt Minderjährige, Umfang, Bewertung/Profilbildung,
Überwachung, Verknüpfung und neue Technik. Private Erprobung ist kein Nachweis
für eine Schule mit vielen Kinderkonten. Bei systematischer Lernprofilbildung,
umfangreicher Minderjährigenverarbeitung oder KI-basierter Bewertung: vor
Start Art. 35/Listen der zuständigen Aufsicht prüfen und bei voraussichtlich
hohem Risiko DSFA mit Datenschutzstelle durchführen. Offene hohe Restrisiken
nicht allein mit einer UI-Bestätigung freigeben. Fallentscheidung, Alternativen,
Maßnahmen und Freigabe werden im privaten Betreiberregister dokumentiert.

Bei Cloud-/Identitäts-/SMTP-Anbietern je Konfiguration Region,
Unterauftragnehmer, Art. 28-Vertrag bzw. eigene Verantwortlichkeit, Art. 44 ff.,
Transferbewertung und Lösch-/Trainingsbedingungen erfassen. Technische
Übermittlungsbestätigung ist nicht automatisch die datenschutzrechtliche
Einwilligung. LearnPip startet keinen Trainingsauftrag; fremde Providerbedingungen
können dennoch Training erlauben und sind vor Nutzung zu prüfen.

## Datenschutzverletzungen

Betreiberleitung/Datenschutzstelle und Vertretung dokumentieren Kenntniszeit,
Art, betroffene Kategorien/Umfang, Risiken, Sicherungsmaßnahmen und Entscheidung.
Art. 33: grundsätzlich binnen 72 Stunden an die zuständige Datenschutzaufsicht,
außer eine Verletzung führt voraussichtlich nicht zu einem Risiko; Gründe
dokumentieren. Art. 34: bei hohem Risiko Betroffene ohne unangemessene
Verzögerung informieren, gesetzliche Ausnahmen prüfen. Auftragnehmer informieren
Verantwortliche ohne unangemessene Verzögerung. CRA/NIS2 separat bewerten;
[Vorfallübung](../NIS2-OPERATIONS.md#synthetische-vorfallübung).
