# Einen Katalog freiwillig für die Community vorschlagen

Der lokale Community-Export und die anschließende Prüfung sind getrennte Schritte.
Der Export funktioniert ohne GitHub-Konto oder Internetzugang. Eine Einreichung ist
freiwillig; LearnPip lädt nichts hoch und veröffentlicht keine Pakete automatisch.
Kataloge und ihre Releases gehören in separate Daten-Repositories, nicht in das
LearnPip-Anwendungsrepository. Deren Einrichtung bleibt Aufgabe von #114.

## Vom eigenen Katalog zum lokalen Paket

1. Unter **Kataloge und Inhalte** den eigenen Katalog, einzelne Fragen oder eine
   durch Fach, Thema, Sprache oder Zielgruppe gefilterte Teilmenge auswählen.
2. Unter **Quellen und Rechte je Frage und Bild** tatsächliche Einzellizenzen,
   Rechteinhaber, Attribution, Quellfassung und Änderungen prüfen. Eigene Originale
   können bewusst beispielsweise CC BY-SA 4.0 erhalten. Fremdinhalte behalten ihre
   tatsächlichen Lizenzen; die Sammlungslizenz ersetzt keine Bildlizenz.
3. Die Exportvorschau einschließlich Rechtebericht und LICENSES.md, NOTICE und
   ATTRIBUTION lesen. Technische Schema-/Duplikatsprüfungen ersetzen keine
   fachliche Prüfung oder Prüfung der tatsächlichen Rechte.
4. Der Betreiber muss `LEARNPIP_COMMUNITY_EXPORT_ENABLED=true` ausdrücklich
   freigeben. Ohne diese Freigabe bleibt privater Export möglich. In der Vorschau
   **Paket für einen Community-Vorschlag** wählen und die offene Weitergabe
   ausdrücklich bestätigen. Änderungen benötigen eine neue Vorschau.
5. Einen ausdrücklich gewählten Attributionsnamen verwenden. Konten, E-Mail,
   Lernstände, Gruppencodes und Kommentare anderer werden nicht als Kontodaten
   exportiert. Freitext, Bilder, Attribution und Quelldokumente trotzdem auf
   unbeabsichtigte personenbezogene oder vertrauliche Angaben prüfen.
6. Das versionierte ZIP lokal herunterladen. Der Download ist keine Einreichung
   und keine Zustimmung zur Übertragung des Urheberrechts an LearnPip.

Details zu Format, Limits, Quellen und erhaltenen Medien:
[Katalogaustausch](CATALOG-INTERCHANGE.md#quellen-rechte-und-community-bericht).

## Optionaler separater Review-Kanal

Der Herausgeber eines unabhängigen Katalog-Repositories benennt in dessen README
einen betreuten Einreichungskanal. Möglich sind ein Pull Request oder ein vereinbarter
Datei-/Link-Kanal ohne GitHub-Pflicht. Ist kein Kanal eingerichtet, bleibt das ZIP
lokal; LearnPip erfindet keine Kontaktadresse und sendet nichts.

Vor einer öffentlichen Einreichung Metadaten erneut prüfen. Herausgeber begrenzen
Einreichungsgröße und -häufigkeit im gewählten Kanal und behandeln unbekannte
Archive als Daten: keine enthaltenen Programme ausführen, keine ungeprüfte
Entpackung und kein automatischer Merge von Beiträgen. Der Validator prüft
Archivgrößen, Pfade, Schema, IDs, Hashes und Beziehungen ohne Netzwerkzugriff.

Review umfasst fachliche Richtigkeit, eindeutige Lösungen, Zielgruppe,
Quellen-/Urhebernachweise, alle Medienlizenzen, Lizenzkompatibilität sowie Schutz
vertraulicher Inhalte. Der Prüfer gibt eine nachvollziehbare Freigabe oder Ablehnung
mit Rückmeldung. Bei Korrekturen eine neue Inhaltsversion exportieren und erneut
prüfen; die Schema- und App-Version bleiben davon unabhängig.

## Offline-Prüfprotokoll und Freigabeindex

`scripts/review-catalog.py` ist ein optionales lokales Herausgeberwerkzeug. Es nutzt
den vorhandenen versionierten Reader und benötigt nur Python 3. Das Werkzeug samt
`read-catalog.py`, `validate-catalog.py`, `catalog-schema.py` und den archivierten
`schemas/catalog/` kann im separaten Katalog-Repository verwendet werden; ein
Build oder Release der LearnPip-Anwendung ist dafür nicht erforderlich.

Eine Freigabe bestätigt drei tatsächlich durchgeführte Prüfungen. Die Flags sind
Prüfererklärungen, keine automatische Rechts- oder Qualitätsbescheinigung:

```bash
python3 scripts/review-catalog.py decide katalog.zip reviews.json \
  --decision approved --reviewer 'Redaktion-1' \
  --reason 'Lösungen, Quellen, Einzellizenzen und öffentliche Metadaten geprüft.' \
  --checked rights --checked content --checked privacy \
  --url 'https://example.org/releases/v1.0.0/katalog.zip'
```

Die Beispieladresse ist durch die geplante unveränderliche Release-Adresse des
Katalog-Herausgebers zu ersetzen. Es findet kein Abruf statt. Adresse und Datei
müssen vor manueller Veröffentlichung übereinstimmen. Geheimnisse, URL-Parameter
und Anmeldedaten sind dort nicht zulässig. Private oder unbekannte Einzellizenzen
sperren die Freigabe auch bei offen lizenziertem Fragetext.

Ablehnung und Rücknahme benötigen eine Begründung, aber keine Freigabe-Flags:

```bash
python3 scripts/review-catalog.py decide katalog.zip reviews.json \
  --decision rejected --reviewer 'Redaktion-1' \
  --reason 'Bildquelle unvollständig; bitte berichtigen und erneut einreichen.'

python3 scripts/review-catalog.py decide katalog.zip reviews.json \
  --decision withdrawn --reviewer 'Redaktion-1' \
  --reason 'Fachlicher Fehler; diese Fassung wird nicht weiter angeboten.'

python3 scripts/review-catalog.py index reviews.json > catalog-index.json
```

Das Werkzeug verwaltet gültige Pakete. Ein formal ungültiges oder gefährliches
Archiv wird vor jeder Protokolländerung abgewiesen; die Rückmeldung erfolgt dann
über den Einreichungskanal. Keine Freigabe und kein Teileintrag entstehen.

`reviews.json` enthält Entscheidungen mit Prüfpseudonym, UTC-Zeit, Begründung,
Inhalts-/Schema-/Quellversion und SHA-256 der exakt geprüften ZIP-Bytes. Historische
Einträge bleiben bei Folgeentscheidungen erhalten. Eine Inhaltsversion darf nie
andere ZIP-Bytes erhalten. Zurückgezogene Versionen können nicht wieder freigegeben
werden; eine Korrektur benötigt eine neue Inhaltsversion. Gleichzeitige Schreiber
werden mit einer Sperre abgewiesen, Historie und Index atomar gespeichert.
Eine nach Absturz verbliebene `.lock`-Directory erst entfernen, wenn kein
Review-Prozess mehr läuft. Das Protokoll ist auf 8 MiB begrenzt; bei Erreichen
bewusst archivieren, nicht still Einträge löschen.

Der Index enthält ausschließlich Versionen, deren letzte Entscheidung `approved`
ist. Freigaben für mehrere unveränderliche Versionen sind möglich. Ablehnungen und
Rücknahmen entfernen nur das Angebot aus dem nächsten Index. Bereits heruntergeladene
Kopien, importierte Fragen und Lernstände bleiben erhalten; eine Rücknahme ist kein
technischer Widerruf bereits erteilter offener Lizenzen.

Registry und Index sind vertrauenswürdige Herausgeberdateien, keine signierten
Nachweise oder Authentifizierung. Nur autorisierte Redakteure dürfen sie ändern;
Änderungen im separaten Repository prüfen und versionieren. Keine privaten
Fallakten oder E-Mail-Adressen in öffentlichen Begründungen speichern. Das
Werkzeug prüft Indexkonsistenz, schützt aber nicht vor absichtlichem Umschreiben
der gesamten Historie durch jemanden mit Schreibrecht.

## Release, Import und spätere Änderungen

Nach Prüfung veröffentlicht der Herausgeber ZIP, SHA-256, vorhandene
Lizenz-/Attributionsdateien und Index bewusst im separaten Katalog-Release.
Keine dieser CLI-Aktionen erzeugt ein GitHub-Release oder übermittelt Daten.
Feedback, fachliche Änderungen und Rücknahmen werden im Änderungsprotokoll
des Katalogs mit Paketversion und Prüfsumme dokumentiert.

`catalog-index.json` ist ein portabler Freigabeindex für Herausgeber. LearnPip
ruft ihn nicht automatisch ab. Betreiber können daraus die freigegebene
HTTPS-Adresse und Prüfsumme in ihre bereits vorhandene Paketquellenkonfiguration
übernehmen oder das ZIP lokal importieren. Die Instanz zeigt Quellen-/Lizenznachweise
vor bestätigter Übernahme. Ein neues Katalog-Release benötigt kein App-Release.

Die automatischen Tests prüfen Freigabesperren, private Bildlizenzen,
Versionsunveränderlichkeit, Ablehnung, Rücknahme, beschädigte Archive,
konkurrierende Schreiber und das Einlesen identischer überprüfter Artefakte durch
unabhängige Offline-Reader. Sie prüfen keine tatsächliche pädagogische Eignung oder
rechtliche Berechtigung eines realen Katalogs.
