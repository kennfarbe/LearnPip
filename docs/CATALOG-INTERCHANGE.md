# Austauschformat für eigenständige LearnPip-Kataloge

Status: **Entwurf 0.1.0, noch kein freigegebenes stabiles Format**. Dieses Dokument und die JSON-Schemas legen den öffentlichen Vertragsentwurf fest. Ein Offline-Reader, ein lokaler Writer und ein privater API-/Browserimport mit Vorschau und Datenbanktransaktion sind vorhanden. Gezielter Export aus Benutzerkonten, kontrollierte Updates, stabile Formatmigrationen und Instanztests fehlen noch. Ein öffentliches stabiles 1.0.0 darf erst mit entsprechend getesteten Readern und Golden-Files veröffentlicht werden. Siehe Issues #108, #114, #115, #116 und #118.

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

Dies ist **noch kein Export aus Benutzerkonten oder der Datenbank**: Rollen- und Eigentumsprüfung, Auswahl nach Themen/Katalogen, Privatsphäre, ausdrückliche Lizenz- und Community-Freigabe sowie Metadaten zur neuen Katalogrevision sind vor einem produktiven Export gesondert umzusetzen (#115–#117). Insbesondere dürfen fremde oder private Inhalte niemals allein aufgrund ihrer ID auswählbar werden. Der technische Helfer ist keine Berechtigungsgrenze.

## Privater ZIP-Import in der Anwendung

Unter **Kataloge und Inhalte > Fragenpaket importieren** kann ein angemeldetes Konto eine lokale ZIP-Datei hochladen. Die Vorschau zeigt Originaltitel, Inhalts-/Formatversion, Quellenstand, Sprache, Themen, Fragen- und Medienanzahl sowie die Lizenz- und Attributionstexte. Erst die ausdrückliche Bestätigung der privaten Nutzungsrechte legt einen neuen privaten Katalog mit lernbaren Fragen an. Es erfolgt kein Download aus externen Quellen und keine Veröffentlichung.

Die API prüft Pfade, doppelte ZIP-/JSON-Einträge, Größen und Kompressionsverhältnis, Dateiprüfsummen, Pflichtfelder, Datentypen und Wertebereiche des archivierten 0.1.0-Vertrags sowie Antworten, Medienverweise und getrennte Herkunftsnachweise. Unbekannte Felder oder Schema-Versionen werden abgewiesen. Die Importbestätigung enthält die SHA-256-Prüfsumme der Vorschau; eine zwischenzeitlich geänderte Datei benötigt eine neue Vorschau. Sämtliche Datenbankänderungen erfolgen in einer Transaktion mit einer Kontensperre, die auch die vorhandenen Bildkontingente schützt.

Paketkennung und Inhaltsvergleich verhindern doppelte Importe auch bei gleichzeitigem Bestätigen. Geänderte JSON-Formatierung oder ZIP-Kompression allein erzeugen keinen neuen Katalog. Ein geänderter Inhalt, Quellen-/Lizenztext oder Paketstand wird als Konflikt abgewiesen. Bestehende Fragen, Entwürfe und Lernstände werden niemals durch einen Reimport überschrieben. Das Entfernen des Katalogordners lässt Fragen und Importidentität bestehen und löst keinen erneuten Import aus.

Die unveränderte Originaldatei und die Zuordnung externer Frage-IDs zu lokalen Fragen werden mit dem Eigentümerkonto gespeichert. Unter **Importierte Originalpakete** kann ausschließlich dieses Konto die Originaldatei herunterladen. Sie enthält unverändert sämtliche Medienbytes, Metadaten, Quellen und Einzellizenzen. Spätere Änderungen der Lernfragen sind darin nicht enthalten; dies ist noch kein Export selbst erstellter oder bearbeiteter Fragen nach #115. Angezeigte Themen und Attribution können für die Kurzansicht gekürzt sein, die Originalangaben bleiben im Paket erhalten.

### Unterstützte Inhalte und Grenzen

- Format: weiterhin **Entwurf 0.1.0**. Ein stabiler Formatvertrag wird damit nicht behauptet.
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
