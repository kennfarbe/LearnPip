# Austauschformat für eigenständige LearnPip-Kataloge

Status: **Entwurf 0.1.0, noch kein freigegebenes stabiles Format**. Dieses Dokument und die JSON-Schemas legen den öffentlichen Vertragsentwurf fest. Die Integration in API, Import/Export, Migrationen und Instanztests fehlt noch. Ein öffentliches stabiles 1.0.0 darf erst mit entsprechend getesteten Readern und Golden-Files veröffentlicht werden. Siehe Issues #108, #114, #115, #116 und #118.

## Trennung und Versionierung

- format_id ist derzeit org.learnpip.catalog.zip; andere Containerformate erhalten eine neue Kennung.
- schema_version (SemVer) versioniert ausschließlich die Syntax und Semantik des Austauschformats. Die unveränderlich zu archivierenden Schemas liegen unter schemas/catalog/<Version>/.
- catalog_version bezeichnet unabhängig davon eine Inhaltsfassung, source_revision den nachgewiesenen Quellenstand, exporter_app_version ist nur informativ. Ein LearnPip-Update darf keine künstliche Katalogversion erzeugen.
- package_id ist ein dauerhaft stabiler Namespace für das Katalogprodukt; Fragen tragen innerhalb und außerhalb des Archivs unveränderliche, global qualifizierte IDs. Inhaltliche Änderungen erzeugen eine neue catalog_version, keine neuen IDs derselben Fragen.
- Alte **stabile** Reader bleiben unterstützt; unbekannte Versionen werden vor Mutation abgewiesen, nicht als alte interpretiert. Erweiterungen und Migrationen müssen Originaldateien, Quellen, Rechte, IDs, Lösungen und Medien erhalten. Es gibt keine implizite Abwärtskonvertierung oder stillen Datenverlust. Vor einer zukünftigen stabilen Veröffentlichung sind Golden-File-, Roundtrip- und Migrations-Tests verpflichtend.

## Archivvertrag

Das ZIP enthält am Archivwurzelpfad genau eine manifest.json (UTF-8), eine questions.json (UTF-8), LICENSES.md, NOTICE und ATTRIBUTION sowie bei Bedarf Dateien unter media/. Keine ausführbaren Dateien, externen URLs als Paketinhalt, Symlinks, absolute Pfade, Backslashes, Traversal-Komponenten oder mehrfachen Pfadnamen. Die Manifest-Datei selbst steht nicht in files, da sie ihren eigenen Hash nicht zirkelfrei enthalten kann. Jeder andere Eintrag wird in files mit Byte-Länge und SHA-256 verzeichnet. Die medienbezogenen Lizenzen und Nachweise sind in questions.json je Verwendung enthalten; LICENSES.md, NOTICE und ATTRIBUTION liefern zusätzlich menschenlesbare Nachweise.

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

1. Vollständigen Offline-Validator und ZIP-Import-/Export-Reader erstellen, Schema **und** semantische Regeln testen.
2. Rechte- und Objektprüfungen im Backend, datensparsamen Filterexport, Konfliktvorschau, Transaktionen und Idempotenz implementieren.
3. Synthetische Golden-Pakete inklusive Bild, Attributions- und Lizenzvarianten und Sonderzeichen dauerhaft versionieren. Kein offizieller oder fremder Fragenbestand im App-Repository.
4. Integration auf zwei voneinander unabhängigen Installationen sowie ein Upgrade von alter zu neuer App-Version testen; neue Versionen nur mit expliziter verlustfrei geprüfter Zielversion abwärts exportieren.
5. Jede stabile Schema-Version und ihren Reader dauerhaft in CI testen. Für neue Major-Versionen Migration und unverändertes Originalarchiv nachweisen.

Die Einführung dieses **Entwurfs** schließt die genannten Issues ausdrücklich noch nicht.
