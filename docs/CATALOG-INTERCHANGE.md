# Austauschformat für eigenständige LearnPip-Kataloge

Status: **Entwürfe 0.1.0 und 0.2.0, noch kein freigegebenes stabiles Format**. Dieses Dokument und die JSON-Schemas legen den öffentlichen Vertragsentwurf fest. Ein Offline-Reader, ein lokaler Writer und ein privater API-/Browserimport mit Vorschau und Datenbanktransaktion sind vorhanden. Ausgewählte eigene Originalfragen und unveränderte importierte Fragen sind lokal exportierbar; ein Test zwischen zwei unabhängigen Datenbanken ist vorhanden. Vollständige Einzelnachweise für bearbeitete Importfragen, kontrollierte Updates und stabile Formatmigrationen bleiben offen. Ein öffentliches stabiles 1.0.0 darf erst mit entsprechend getesteten Readern und Golden-Files veröffentlicht werden. Siehe Issues #108, #114, #115, #116 und #118.

## Trennung und Versionierung

- format_id ist derzeit org.learnpip.catalog.zip; andere Containerformate erhalten eine neue Kennung.
- schema_version (SemVer) versioniert ausschließlich die Syntax und Semantik des Austauschformats. Die unveränderlich zu archivierenden Schemas liegen unter schemas/catalog/0.1.0/ (später je stabiler Version getrennt).
- catalog_version bezeichnet unabhängig davon eine Inhaltsfassung, source_revision den nachgewiesenen Quellenstand, exporter_app_version ist nur informativ. Ein LearnPip-Update darf keine künstliche Katalogversion erzeugen.
- package_id ist ein dauerhaft stabiler Namespace für das Katalogprodukt; Fragen tragen innerhalb und außerhalb des Archivs unveränderliche, global qualifizierte IDs. Inhaltliche Änderungen erzeugen eine neue catalog_version, keine neuen IDs derselben Fragen.
- Alte **stabile** Reader bleiben unterstützt; unbekannte Versionen werden vor Mutation abgewiesen, nicht als alte interpretiert. Erweiterungen und Migrationen müssen Originaldateien, Quellen, Rechte, IDs, Lösungen und Medien erhalten. Es gibt keine implizite Abwärtskonvertierung oder stillen Datenverlust. Vor einer zukünftigen stabilen Veröffentlichung sind Golden-File-, Roundtrip- und Migrations-Tests verpflichtend.

## Archivvertrag

Das ZIP beginnt zwingend mit manifest.json als erstem ZIP-Eintrag und enthält am Archivwurzelpfad genau eine manifest.json (UTF-8), eine questions.json (UTF-8), LICENSES.md, NOTICE und ATTRIBUTION sowie bei Bedarf Dateien unter media/. Medien-Dateiendungen sind auf .png, .jpg, .jpeg, .webp, .gif, .svg, .txt und .pdf begrenzt (eine Dateiendung ist kein Beweis für sichere Inhalte; Importer müssen Inhalte weiter prüfen). Keine ausführbaren Dateien, externen URLs als Paketinhalt, Symlinks, absolute Pfade, Backslashes, Traversal-Komponenten oder mehrfachen Pfadnamen. Die Manifest-Datei selbst steht nicht in files, da sie ihren eigenen Hash nicht zirkelfrei enthalten kann. Jeder andere Eintrag wird in files mit Byte-Länge und SHA-256 verzeichnet. Die medienbezogenen Lizenzen und Nachweise sind in questions.json je Verwendung enthalten; LICENSES.md, NOTICE und ATTRIBUTION liefern zusätzlich menschenlesbare Nachweise.

Vor der Verarbeitung werden Dateinamen normalisiert und auf Einzigartigkeit geprüft, Archive auf Dateianzahl, dekomprimierte Gesamtgröße, Einzeldateigröße und Kompressionsverhältnis begrenzt, Hashes geprüft und unbekannte Versionsnummern abgelehnt. ZIP-Inhalte dürfen keine Logik oder Skripte ausführen. Integritätsprüfung ist **keine** Authentifizierung des Herausgebers: Eine optional spätere Signatur braucht ein gesondertes Vertrauensmodell.

Das JSON-Schema befindet sich unter schemas/catalog/0.1.0/. Es definiert Pflichtfelder und Formate; zusätzliche semantische Regeln gelten:

- Jede Frage-ID muss einmalig sein; Antwort-IDs müssen je Frage eindeutig sein und sämtliche correct_answer_ids müssen existieren.
- Sämtliche referenzierten Medien müssen enthalten und gehasht sein; nicht referenzierte Dateien werden zurückgewiesen.
- Für Originale ist ein realer Rechteinhaber samt Attribution anzugeben; abgeleitete und wörtlich übernommene Werke benötigen einen konkreten Quellenlink und Revisionsstand sowie, soweit bearbeitet, einen Änderungsvermerk.
- Die Lizenz jeder Frage und jedes Mediums ist **eigenständig** anzugeben. Die Manifestlizenz ist eine Übersichtsangabe und überschreibt Einzellizenzen niemals. Fremde Daten werden nicht durch die AGPL der Anwendung umlizenziert. Rechteklärung und Lizenzkompatibilität müssen vor Community-Veröffentlichung gesondert erfolgen.
- Weder Konten noch E-Mail-Adressen, Gruppen, Zugangsdaten, Lernstände, private Admin-Daten oder fremde Kommentare gehören zum Paketformat.

## Produkt- und Repositorygrenzen

Ein LearnPip-Release enthält keine realen Fragenkataloge. Die zwei vorgeschlagenen externen Katalog-Repositories benötigen unabhängige Release-/Lizenz-/Review-Prozesse. Die Anwendung muss ohne sie und ohne Internet installierbar bleiben. Der Paketimport soll Datei-Upload ohne GitHub ermöglichen und standardmäßig zunächst privat erfolgen; öffentliche Freigaben sind ein separater Vorgang. Aktualisierungen dürfen persönliche Lernstände nicht an die unveränderlichen Originalfragen koppeln oder löschen. Ein wiederholter Import muss nach vollständiger Vorabvalidierung idempotent sein.

## Weitere Abnahmeschritte vor Freigabe als 1.0.0

1. Die vorhandenen Reader und Validatoren systematisch gegen den gesamten Schema-Vertrag und seine semantischen Regeln prüfen.
2. Rechte- und Objektprüfungen im Backend, datensparsamen Filterexport, Konfliktvorschau, Transaktionen und Idempotenz implementieren.
3. Synthetische Golden-Pakete inklusive Bild, Attributions- und Lizenzvarianten und Sonderzeichen dauerhaft versionieren. Kein offizieller oder fremder Fragenbestand im App-Repository.
4. Integration auf zwei voneinander unabhängigen Installationen sowie ein Upgrade von alter zu neuer App-Version testen; neue Versionen nur mit expliziter verlustfrei geprüfter Zielversion abwärts exportieren.
5. Jede stabile Schema-Version und ihren Reader dauerhaft in CI testen. Für neue Major-Versionen Migration und unverändertes Originalarchiv nachweisen.

Der Reader extrahiert keine Archive auf das Dateisystem und führt keine Paketinhalte aus. Er akzeptiert nur Entwurfsversion 0.1.0; ein unbekanntes Schema führt zu einer Ablehnung vor Ausgabe von Paketdaten. Die Einführung dieses **Entwurfs** schließt die genannten Issues ausdrücklich noch nicht.

## Lokale Weitergabe eines vollständigen Entwurfspakets

`scripts/read-catalog.py` liest nur zuvor vollständig geprüfte ZIP-Pakete. `scripts/write-catalog.py` schreibt einen solchen vollständigen Snapshot in eine neue ZIP-Datei, berechnet die Dateiprüfsummen erneut und validiert das Ergebnis vor dem atomaren Austausch der Zieldatei. Die Ursprungsdatei bleibt unverändert. Tests prüfen Fragen, Lösungen, Medien und Attributionsdateien nach einem erneuten Einlesen. Der Writer ist kein Export privater Daten aus LearnPip: Filter, Rollenrechte, Lizenzentscheidungen, Freigabe sowie Import in die Datenbank bleiben in eigenen Aufgaben offen. Die Version 0.1.0 bleibt ein Entwurf.

## Dauerhafte Vertragsbeispiele

Unter `tests/fixtures/catalog/0.1.0/frozen.json` liegt ein eigenständiges synthetisches Beispiel des Entwurfsstands 0.1.0. Der Vertragstest erzeugt daraus lokal ein ZIP, prüft den alten Stand mit dem aktuellen Reader und führt einen erneuten Schreib-/Lesevorgang einschließlich der Lösungen und Nachweistexte durch. Die Datei darf bei Formatänderungen nicht stillschweigend angepasst werden; zukünftige Formatstände erhalten eigene Beispiele. Dieser Test ist ein erster Kompatibilitätsschutz, aber noch kein vollständiger stabiler Golden-Archivbestand mit Medien und Migrationen nach #118.

Unter `tests/fixtures/catalog/0.1.0/provenance.json` liegt ein zweites eigenständiges synthetisches Beispiel mit einer bearbeiteten Frage, eigener Quellenrevision und einer gesondert lizenzierten Mediendatei. Der Vertragstest prüft, ob Lizenz- und Herkunftsangaben der Frage und des Mediums getrennt bleiben, die Medienbytes erhalten bleiben und die ursprüngliche Datei nicht verändert wird. Die Beispieldaten und die verlinkten Adressen sind ausschließlich synthetisch; sie belegen keine Rechte an realen Fremdinhalten. Vollständige stabile Golden-Archive und Migrationstests bleiben offen.

### Fest gespeichertes ZIP-Testpaket

`tests/fixtures/catalog/0.1.0/golden.zip` ist ein dauerhaft eingechecktes Archiv mit einem synthetischen PNG-Bild, Umlauten, getrennten Frage-/Bildlizenzen, Herkunftsnachweisen und einer informativen älteren Exporterkennung. Anders als die JSON-Beispiele wird dieses Archiv im Test nicht neu erzeugt. Der aktuelle Reader muss dessen tatsächliche Bytes weiterhin akzeptieren. Vollständiger und selektiver Export werden erneut gelesen; Fragen, richtige Antworten, Bildbytes, Nachweise und Versionsangaben müssen erhalten bleiben. Nur die Byte-Länge und Prüfsumme einer neu serialisierten JSON-Datei dürfen sich ändern. Optionale Dateiangaben wie `media_type` bleiben erhalten.

`contract-lock.json` hält SHA-256-Werte dieses Archivs, beider JSON-Beispiele und der beiden archivierten Schemas fest. Der bestehende verpflichtende CI-Katalogtest prüft diese Werte sowie die Vollständigkeit des Bestands. Änderungen am Format erhalten einen neuen Versionsordner und eigene Testpakete; vorhandene Archive und Schemas werden nicht zur Anpassung an einen neuen Reader umgeschrieben. Die Sperrdatei ist ein Regressionsschutz, keine Herausgebersignatur.

Unbekannte Schema-Versionen melden sowohl die empfangene als auch die unterstützte Version. Nicht standardkonforme JSON-Zahlen (`NaN`, `Infinity`, `-Infinity`) werden abgewiesen. Ein fehlerhafter Export ersetzt keine bereits vorhandene Zieldatei und hinterlässt keine temporären ZIP-Dateien.

Lokale Prüfung:

```bash
python3 -m unittest discover -s tests/ops -p 'test_catalog_contract.py' -v
python3 scripts/validate-catalog.py tests/fixtures/catalog/0.1.0/golden.zip
```

Dieser Bestand sichert weiterhin den **Entwurf 0.1.0**. Vor einem stabilen 1.0.0 fehlen insbesondere vollständige Schema-Prüfung, instanzweite Paketverwaltung und Instanztests. Eine Migration zwischen stabilen Major-Versionen wird erst mit einem tatsächlich freigegebenen Nachfolgeformat implementiert und geprüft; eine bloße Änderung der Versionsnummer wäre kein Migrationsnachweis. #118 bleibt bis zur vollständigen Abnahme offen.

## Unbekannte Felder im Entwurf

Der Offline-Validator weist zusätzliche, nicht im Schema 0.1.0 definierte Felder in Manifest, Fragen, Antworten, Lizenz- und Herkunftsangaben sowie Medien zurück. Damit werden Daten nicht stillschweigend ignoriert. Zukünftige Erweiterungen benötigen einen ausdrücklich versionierten Vertrag; der derzeitige Reader und Writer dürfen unbekannte Versionen weiterhin nicht als alte Fassung behandeln. Eine vollständige automatische JSON-Schema-Prüfung ist damit noch nicht ersetzt.

Auch das Wurzelobjekt von `questions.json` darf im Entwurf 0.1.0 ausschließlich `questions` enthalten. Zusätzliche Metadaten wie private Kennungen oder Exportinformationen werden nicht stillschweigend übernommen, sondern bereits bei der Offline-Prüfung zurückgewiesen. Eine spätere Erweiterung benötigt einen ausdrücklich neuen Formatvertrag.

## Selektive Offline-Auswahl

`scripts/select-catalog.py` stellt `select_from_file(datei, frage_ids)` bereit. Die Quelldatei wird vor jeder Auswahl vollständig validiert. Der Helfer übernimmt ausschließlich ausdrücklich gewählte Fragen in ihrer bisherigen Reihenfolge, behält deren Antwort- und Herkunftsangaben sowie die vollständigen Nachweisdateien und entfernt nicht mehr referenzierte Mediendateien. `scripts/write-catalog.py` erzeugt daraus ein neues geprüftes Paket mit aktualisierten Hashes. Leere oder unbekannte Frage-IDs werden zurückgewiesen, die Ursprungsdatei bleibt unverändert.

Der technische JSON-Helfer ist keine Berechtigungsgrenze. Der Datenbankexport
prüft Eigentum und aktive Konten API-seitig; Auswahl, Vorschau und bestätigter
Browserdownload stehen im Fragen-Arbeitsbereich bereit (siehe unten).

## Privater ZIP-Import in der Anwendung

Unter **Kataloge und Inhalte > Fragenpaket importieren** kann ein angemeldetes Konto eine lokale ZIP-Datei hochladen. Die Vorschau zeigt Originaltitel, Inhalts-/Formatversion, Quellenstand, Sprache, Themen, Fragen- und Medienanzahl sowie die Lizenz- und Attributionstexte. Erst die ausdrückliche Bestätigung der privaten Nutzungsrechte legt einen neuen privaten Katalog mit lernbaren Fragen an. Es erfolgt kein Download aus externen Quellen und keine Veröffentlichung.

Die API prüft Pfade, doppelte ZIP-/JSON-Einträge, Größen und Kompressionsverhältnis, Dateiprüfsummen, Pflichtfelder, Datentypen und Wertebereiche des archivierten 0.1.0-Vertrags sowie Antworten, Medienverweise und getrennte Herkunftsnachweise. Der Nachfolgeentwurf 0.2.0 wird durch ausdrücklich zusätzliche Blockprüfungen eingelesen. Unbekannte Felder oder Schema-Versionen werden abgewiesen. Die Importbestätigung enthält die SHA-256-Prüfsumme der Vorschau; eine zwischenzeitlich geänderte Datei benötigt eine neue Vorschau. Sämtliche Datenbankänderungen erfolgen in einer Transaktion mit einer Kontensperre, die auch die vorhandenen Bildkontingente schützt.

Paketkennung und Inhaltsvergleich verhindern doppelte Importe auch bei gleichzeitigem Bestätigen. Geänderte JSON-Formatierung oder ZIP-Kompression allein erzeugen keinen neuen Katalog. Ein geänderter Inhalt, Quellen-/Lizenztext oder Paketstand wird als Konflikt abgewiesen. Bestehende Fragen, Entwürfe und Lernstände werden niemals durch einen Reimport überschrieben. Das Entfernen des Katalogordners lässt Fragen und Importidentität bestehen und löst keinen erneuten Import aus.

Die unveränderte Originaldatei und die Zuordnung externer Frage-IDs zu lokalen Fragen werden mit dem Eigentümerkonto gespeichert. Unter **Importierte Originalpakete** kann ausschließlich dieses Konto die Originaldatei herunterladen. Sie enthält unverändert sämtliche Medienbytes, Metadaten, Quellen und Einzellizenzen. Spätere Änderungen der Lernfragen sind darin nicht enthalten; dies ist noch kein Export selbst erstellter oder bearbeiteter Fragen nach #115. Angezeigte Themen und Attribution können für die Kurzansicht gekürzt sein, die Originalangaben bleiben im Paket erhalten.

### Unterstützte Inhalte und Grenzen

- Format: weiterhin **Entwürfe 0.1.0 und 0.2.0**. Ein stabiler Formatvertrag wird damit nicht behauptet.
- Für die Lernanzeige: gültige, nicht animierte JPEG-/PNG-Bilder bis 5 MiB und 4096 × 4096 Pixel; Bildbeschreibung maximal 300 Zeichen. Anzeigebilder werden wie vorhandene Uploads neu kodiert, damit Metadaten nicht unbeabsichtigt mit angezeigt werden. Das nur privat herunterladbare Originalarchiv bleibt bytegenau erhalten und kann solche Metadaten weiterhin enthalten.
- Fragetext, Erklärung und einzelne Antworttexte: derzeit maximal 4000 Zeichen entsprechend dem vorhandenen Inhaltsblockmodell; Inhaltslizenz-Identifier maximal 120 Zeichen. Andere gültige Paketmedien, größere Inhalte und nicht speicherbare Nullzeichen werden mit verständlichem Fehler vollständig abgewiesen, niemals still weggelassen.
- Pro Konto maximal 20 Originalpakete mit zusammen 100 MiB; bestehende Bildkontingente von 100 Bildern/100 MiB gelten zusätzlich. Die Datenbank benötigt auch Platz für die entpackten Lernfragen und bereinigten Anzeigebilder. Das Backup muss die neue Tabelle `CatalogPackageImports` einschließen; vollständige PostgreSQL-Sicherungen tun dies automatisch.
- Originalpakete unterliegen dem Lebenszyklus des Eigentümerkontos. Ein Katalogordner allein löscht sie nicht.

### API und Prüfung

- `POST /api/v1/catalog-packages/preview`: Multipart-Datei `file`, keine Datenbankänderung.
- `POST /api/v1/catalog-packages/import`: dieselbe Datei, `archiveSha256` aus der Vorschau und `rightsConfirmed=true`.
- `GET /api/v1/catalog-packages/`: ausschließlich eigene Importmetadaten.
- `GET /api/v1/catalog-packages/{id}/original`: eigener Originaldownload ohne Cachefreigabe.

Tests prüfen den produktiven Reader am unveränderten Golden-ZIP, bildbezogene Rechteangaben, Inhaltsvergleich trotz anderer JSON-Formatierung, manipulierte Dateien, Versions-/Schemafehler, Vorschau-/Bestätigungsbindung, fehlende Zustimmung, gleichzeitigen Reimport, Konflikte, erhaltene eigene Entwürfe und fremde Zugriffe. Browserprüfungen sichern ausdrückliche Zustimmung, gesperrte Konflikte, zurückgesetzte Vorschauen bei einer neuen Dateiauswahl sowie die mobile Darstellung. Die neue Datenbankmigration ist additiv und lässt bestehende Fragen unverändert.

Instanzweite Administrationspakete, Setup-Auswahl, kontrollierte Updates/Entfernung, vollständiger Fragenexport aus Konten, stabile Formatmigrationen und der Abnahmetest auf zwei frischen unabhängigen Installationen bleiben als weitere Abnahmeschritte in #108, #110, #115 und #118 offen.

## Lokaler Export eigener Fragen

Unter **Fragen > Fragen auswählen und exportieren** die gespeicherten Inhalte
aktualisieren. Alle eigenen Fragen, einen Katalog oder einzelne Fragen wählen;
Fach, Thema, Sprache und Textsuche lassen sich kombinieren. Die Filterauswahl
ersetzt die bisherigen Häkchen; einzelne Häkchen ergänzen oder entfernen Fragen.
Der Server prüft jede ausgewählte Fragenkennung auf Eigentum und Nichtlöschung.
Gruppenfreigaben oder eine Moderatorenrolle erlauben keinen fremden Massenexport.

Gespeicherte Entwürfe haben Vorrang; andernfalls wird die neueste gespeicherte
Fassung verwendet. Nicht gespeicherte Editoränderungen sind nicht enthalten.
Titel und Herausgeber werden ausdrücklich eingegeben. Sie stehen im ZIP und
werden nicht automatisch aus Kontonamen oder E-Mail-Adressen übernommen.

Eigene Originaltexte und eigene Originalbilder erhalten getrennte Lizenz-,
Rechteinhaber- und Attributionsangaben. `LicenseRef-Private` ist die Voreinstellung
für ausschließlich private Nutzung durch berechtigte Empfänger, keine offene
Lizenz und keine Community-Freigabe. Eine andere Lizenz benötigt eine bewusste
Angabe einschließlich Lizenztext oder Lizenzverweis. Die Bestätigung bezieht
sich ausdrücklich auf Originalrechte und Exportrechte. Technische Validierung
belegt keine Rechteinhaberschaft oder Lizenzkompatibilität.

Unveränderte Importfragen behalten sämtliche originalen Einzellizenzen,
Herkunftsdaten, Themen, Zielstufen, Quellenrevisionen und Originalmedienbytes.
Bestehende Lizenzen eigener Fragen müssen mit der ausdrücklichen Exportangabe
übereinstimmen; es gibt keine automatische Umetikettierung. Fragen mit externen
Quellenadressen und bearbeitete Importfragen werden bis zur vollständigen
Erfassung ihrer gesonderten Bearbeitungs-/Quellennachweise abgewiesen. Ein freier
Herkunftshinweis eigener Originale bleibt als `source_note` erhalten; er ist kein
Ersatz für den Nachweis einer Drittquelle. Die Originalarchive bleiben separat
herunterladbar. Vorhandene Lizenz- und Attributionsnachweise werden erhalten,
auch wenn ein Originalpaket einen weiter gefassten Hinweistext mitliefert.

**Exportvorschau prüfen** erzeugt und validiert das vollständige ZIP in einem
konsistenten Datenbank-Lesesnapshot. Anzahl, Themen, Medien, Größe und sämtliche
Lizenznachweise werden angezeigt. Der Download verlangt danach eine ausdrückliche
Rechtebestätigung und die SHA-256-Prüfsumme dieser Vorschau. Änderungen an Fragen,
Auswahl oder Metadaten erfordern eine neue Vorschau. Es wird kein Exportdatensatz
angelegt und keine Veröffentlichung oder Netzwerkverbindung ausgelöst.

Der Download enthält ausschließlich die ausgewählten Frageinhalte mit den
benötigten Medien und Nachweisen. Konten, Wiederherstellungsgeheimnisse,
Lernstände, Gruppencodes, fremde Kommentare und Verwaltungsdaten werden nicht
abgefragt oder serialisiert. Eine lokal erzeugte Fragenkennung trägt den Namespace
`learnpip-question:`; er enthält eine Inhaltskennung und keine Kontokennung.
Ausgewählte Quell-IDs bestimmen eine reproduzierbare Paketkennung; unveränderte
Inhalte ergeben identische ZIP-Bytes. ZIP-Zeitstempel sind dafür fest definiert.

Auf Instanz B dieselbe Datei unter **Fragen > Fragen importieren** auswählen.
Bilder in Fragen, Antworten und Erklärungen behalten ihre Reihenfolge und
Alternativtexte. Der Import bleibt privat. Überlappende Auswahlen verwenden
unveränderte importierte Quellfragen erneut, einschließlich identischer Bildbytes
bei anderen Archivpfaden. Neue Fragen landen in einem neuen privaten Katalog;
bereits vorhandene bleiben in ihren bisherigen Katalogen. Konflikte sperren das
gesamte Paket. Bereits bearbeitete oder gelöschte Quellfragen werden weder ersetzt
noch als zweite Kopie angelegt. Ein unverändertes bereits installiertes
Originalpaket bleibt auch nach eigenen Änderungen idempotent.

Grenzen: maximal 10000 Fragen je Export und in der Auswahlübersicht, ZIP bis
25 MiB; die bestehenden Bildgrenzen gelten weiter. Überschreitungen werden vor
dem Import abgewiesen. Es gibt keinen verlustbehafteten Teilexport und keine
automatische Herabstufung auf 0.1.0. Stabile Formatmigrationen und die umfassende
Moderationsmatrix bleiben die getrennten Anforderungen von #118 und #119.

| API | Zweck |
| --- | --- |
| GET `/api/v1/catalog-exports/questions` | Ausschließlich eigene gespeicherte Fragen für die Auswahl |
| POST `/api/v1/catalog-exports/preview` | Vollständige ZIP-Validierung und Nachweisvorschau, ohne Schreiboperation |
| POST `/api/v1/catalog-exports/download` | Bestätigter Download derselben Inhalte mit `rightsConfirmed` und `previewSha256` |

## Blockformat 0.2.0 und Kompatibilität

0.1.0 bleibt unverändert archiviert und wird weiterhin importiert. Der ausdrücklich
neue Entwurf 0.2.0 ergänzt die vollständigen Inhaltsblöcke des Fragenmodells:

| Feld | Bedeutung |
| --- | --- |
| `subject`, `topic` | Fach und Thema getrennt von der vollständigen Themenliste |
| `question_version` | Inhaltsrevision der einzelnen Frage, unabhängig von lokalen Datenbankversionen |
| `selection_mode` | Einfach- oder Mehrfachauswahl mit passender Anzahl richtiger Antworten |
| `source_note` | Optionaler freier Herkunftshinweis eigener Originale |
| `origin` | Originalmanifest eines übernommenen 0.1.0-Pakets ohne dessen Dateiliste, einschließlich ursprünglicher Paket- und Quellenrevision |
| `prompt_blocks`, `explanation_blocks` | Geordnete Text-/Bildblöcke; Bildpfade müssen vollständig ausgezeichnete Medien referenzieren |
| `answers[].blocks` | Geordnete Antwortblöcke einschließlich Bildern |

Textblöcke haben `kind: text` und `text`; Bildblöcke `kind: image` und `path`.
Textzusammenfassungen müssen den Textblöcken entsprechen; reine Bildinhalte
verwenden `[Bild]`. Medien dürfen nicht unreferenziert bleiben. Einzellizenzen,
Alternativtexte und Herkunftsinformationen stehen weiterhin an jedem Medium.
Ein scheinbar erfolgreiches Weglassen von Antwort- oder Erklärungsbildern ist
unzulässig. Unbekannte Versionen werden ausdrücklich abgewiesen.

`tests/fixtures/catalog/0.2.0/golden.zip` enthält synthetische Antwort- und
Erklärungsbilder, getrennte Lizenzen und Sonderzeichen. Seine Prüfsummen und die
Schemas werden separat gesperrt; die 0.1.0-Sperrdatei wird nicht verändert. Python
und .NET prüfen beide tatsächlichen Archive und Roundtrips. Ein PostgreSQL-Test
legt zwei frische unabhängige Datenbanken an, erstellt fünf Fragen auf A, überträgt
eine Teilmenge auf B, prüft Reimport und überlappende Auswahl ohne Duplikate sowie
Bildreihenfolge, Quellen-IDs, private Sichtbarkeit und fehlende Lernstände.
Dies ist keine Freigabe eines stabilen Schemas und keine stabile Major-Migration.

## Optionale Instanzpakete und bestätigte Updates

Unter **Administration > Optionale Lernpakete** lokale ZIP-Datei auswählen,
**Paketfassung und Änderungen prüfen**, Originaltexte unter LICENSES.md, NOTICE
und ATTRIBUTION lesen und Bereitstellung ausdrücklich bestätigen. Keine Auswahl
und kein Download erfolgt automatisch. Das Paket wird Lernenden als optionale
private Übernahme angeboten; die Bereitstellung veröffentlicht keine Kontodaten.
Titel, Beschreibung/Zielgruppe, Sprache, Version, Quellenstand, Fragenzahl sowie
Archivgröße und entpackter Speicherbedarf werden vor Bestätigung angezeigt.

Versionen sind unveränderlich: geänderte Inhalte benötigen eine neue
`catalog_version`. Updates deaktivieren das vorherige Angebot, verändern jedoch
keine privaten Kopien, persönlichen Bearbeitungen oder historischen Lernfassungen.
Deaktivierung ist umkehrbar. **Endgültig entfernen** löscht nur die Instanzdatei;
private Kopien und Lernstände bleiben bestehen. Ein Audit-Nachweis verhindert,
dass eine entfernte Version später mit anderem Inhalt wiederverwendet wird.
Administratoren benötigen eine frische Anmeldung. Normale Benutzer können keine
Instanzpakete verwalten.

Bereits privat importierte Pakete können nach neuer Vorschau ausdrücklich
aktualisiert werden. ZIP-Prüfsumme und bisheriger Paketfingerabdruck müssen zur
Bestätigung passen. Unbearbeitete Fragen behalten ihre lokale Kennung und erhalten
eine neue unveränderliche Fassung. Entfernte Quellfragen, frühere ZIPs und alte
Fassungen bleiben als Nachweis erhalten. Persönliche Änderungen oder Löschungen
sperren den gesamten Wechsel; es findet kein stilles Überschreiben statt.
Neue Importe dürfen einen eigenen bestehenden privaten Zielkatalog verwenden.

### Offline-Setup und geprüfte Online-Quellen

`./scripts/install-release.sh install --catalog-package /absoluter/pfad/paket.zip`
prüft das optionale lokale Paket beim erstmaligen Setup und zeigt die Original-
Lizenztexte vor gesonderter Bestätigung. Die Option ist wiederholbar. Ohne diese
Option funktioniert Installation unverändert. `--yes` bestätigt auch ausdrücklich
angegebene Pakete; Dateien zuvor prüfen. Spätere Nachinstallation und Updates
laufen über Administration. Pakete sind externe ZIP-Artefakte: keine Repo-,
GitHub-, Cloudkonto- oder Internetpflicht und keine Inhalte im App-Image.

Ein Betreiber kann eine neutrale HTTPS-Quelle mit
`LEARNPIP_CATALOG_SOURCE_TITLE`, `LEARNPIP_CATALOG_SOURCE_URL` und dem unabhängig
geprüften `LEARNPIP_CATALOG_SOURCE_SHA256` konfigurieren. In Administration startet
ein ausdrücklicher Klick den Download; SHA-256, Größenlimit, Schema und Medien
werden geprüft. Weiterleitungen werden abgewiesen. Fehlschläge verändern die
Paketverwaltung nicht. Ohne Quellenkonfiguration gibt es keinen externen Zugriff.
Weitere Quellen sind über `CatalogPackages:Sources` mit Title/Url/Sha256 möglich.
Ein Prüfsummenvergleich beweist Integrität gegenüber dem vertrauten Sollwert,
keine Urheberrechte oder Echtheit eines unbekannten Herausgebers.

## Quellen, Rechte und Community-Bericht

Unter **Fragen > Quellen und Rechte je Frage und Bild** tatsächliche Lizenz,
Rechteinhaber, Attribution, Herkunft, Quellenrevision und Bearbeitungsvermerk je
Text und einzeln je Bild erfassen. Die Bestätigung wird bei Feldänderungen
zurückgesetzt. Nachweise sind an die gespeicherte Inhaltsfassung einschließlich
Bildbytes und Alternativtexten gebunden. Nach Inhaltänderungen erneut prüfen;
ein Export mit veralteten Nachweisen wird verhindert. Importierte Originale
behalten ihre vollständigen Originalnachweise. Bearbeitete Importfragen benötigen
explizite Bearbeitungsnachweise, behalten die Quelllizenz und führen ursprüngliche
Attribution/Quellrevision zusätzlich mit. Keine Lizenz wird aus einem Thema erzeugt.

Der Export zeigt einen **Rechtebericht für offene Weitergabe**. Text und jedes Bild
werden separat geprüft; eine offene Textlizenz verdeckt keine private Bildlizenz.
Wikipedia-Bearbeitungen benötigen CC BY-SA 4.0, Attribution, tatsächliche Revision
und Bearbeitungsvermerk; amtliche Originalquellen DL-DE/BY-2.0 werden getrennt
behandelt. Gemischte Quellen müssen im Bearbeitungs- und Attributionsnachweis
abgegrenzt sein. Der technische Bericht ersetzt keine Prüfung tatsächlicher Rechte
oder komplexer Lizenzkompatibilität. Unbekannte/private Lizenzen sperren den
Community-Zweck, verhindern jedoch keinen berechtigten privaten ZIP-Download.

`LEARNPIP_COMMUNITY_EXPORT_ENABLED=false` ist die Standardeinstellung. Auch bestehende öffentliche Einreichungen und ihre Moderationsfreigabe verwenden
den aktuellen Einzellizenzbericht. Fehlende oder veraltete Nachweise sperren die
Freigabe; eine Einreichung darf die Quelllizenz nicht ersetzen. Erst nach
bewusster administrativer Aktivierung und vollständigen offenen Einzelnachweisen
kann **Paket für einen Community-Vorschlag** gewählt werden. Eigene Originale dürfen
bewusst etwa CC BY-SA 4.0 erhalten; zuerst die gespeicherte Frage und ihre Nachweise
entsprechend ändern. Eine zusätzliche Bestätigung erklärt offene Weitergabe und
Unwiderrufbarkeit erteilter offener Lizenzen. Dies erzeugt nur einen lokalen ZIP-
Download: kein Upload, kein Versand und keine automatische Veröffentlichung.
LICENSES.md, NOTICE und ATTRIBUTION samt Einzellizenzen werden mitgeführt.

Prüfungen: Golden-ZIPs 0.1.0/0.2.0, Schema/Prüfsummen, Zwei-Datenbank-Roundtrip mit
fünf Fragen und Bild/Teilauswahl/Reimport, Rechte- und Admin-Grenzen, bestätigte
Updates, historische Fassungen, Installationsablauf mit/ohne optionale Auswahl
sowie Browserprüfungen einschließlich Smartphone und automatisierter Accessibility.
Ein manueller Test mit zwei vollständigen VM-Installationen und echten Hilfsmitteln
ist damit nicht behauptet; dafür den beschriebenen A/B-Ablauf als Abnahme nutzen.
