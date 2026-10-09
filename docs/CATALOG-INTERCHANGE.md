# Austauschformat für eigenständige LearnPip-Kataloge

Status: **stabiler Vertrag 1.0.0**. Neue Exporte verwenden diesen Vertrag.
Die veröffentlichten Entwürfe 0.1.0 und 0.2.0 bleiben direkt importierbar;
ihre Schemas und Golden-ZIPs werden nicht verändert. Diese Festlegung gilt
für das technische Format. Tatsächliche Inhaltsrechte, fachliche Freigaben
und die unabhängigen Katalog-Repositories bleiben gesonderte Aufgaben.

## Trennung und Versionierung

| Feld | Bedeutung |
| --- | --- |
| `format_id` | `org.learnpip.catalog.zip`; ein anderes Containerformat benötigt eine eigene Kennung. |
| `schema_version` | SemVer des archivierten Syntax-/Semantikvertrags; unabhängig von Anwendung und Inhalt. |
| `package_id` | Dauerhafter Paket-Namespace; Frage-IDs sind global qualifiziert und bleiben bei Inhaltsänderungen erhalten. |
| `catalog_version` | Unveränderliche Inhaltsfassung des Katalogprodukts. |
| `source_revision` | Nachgewiesener Quellenstand; keine automatisch erzeugte App-Version. |
| `exporter_app_version` | Optionale informative Exporterkennung; kein Importkriterium. |

Die öffentlichen Schemas liegen je Version unter `schemas/catalog/<version>/`.
Veröffentlichte Schemas und Beispiele bleiben unverändert; Änderungen erhalten
einen neuen Versionsordner. Alte stabile Reader dürfen nicht entfernt werden.
Jeder neue Stand benötigt Golden-Paket, Migrations-/Roundtriptests und den
bestehenden CI-Schutz vor Merge und Release. Ein Importproblem wird behoben,
statt alte Archive passend umzuschreiben. Ist eine Umwandlung tatsächlich
nicht darstellbar, sind die konkrete Grenze und ein separater Migrationsweg
zu dokumentieren; eine Erfolgsmeldung ohne vollständige Inhalte ist unzulässig.

## Archivvertrag

Das ZIP beginnt mit `manifest.json` als erstem Eintrag. Im Archivwurzelpfad
liegen genau einmal `manifest.json`, `questions.json`, `LICENSES.md`, `NOTICE`
und `ATTRIBUTION`; referenzierte Medien liegen unter `media/`. JSON ist UTF-8.
Jede Datei außer dem Manifest steht in `files` mit SHA-256 und Byte-Länge;
optionales `media_type` bleibt bei Weitergabe erhalten. Die Manifestdatei
kann ihren eigenen Hash nicht zirkelfrei enthalten.

Keine Symlinks, Verzeichniseinträge, verschlüsselten Dateien, absoluten Pfade,
Backslashes, Traversal-Komponenten, doppelten ZIP-/JSON-Namen oder ausführbaren
Dateien. Zulässige Medien-Endungen: PNG, JPG/JPEG, WEBP, GIF, SVG, TXT und PDF.
Die Dateiendung beweist keine ungefährlichen Inhalte. Der Paketreader extrahiert
keine Dateien und führt keinen Code aus. Grenzen: 25 MiB komprimiert, 100 MiB
entpackt, 20 MiB je Datei, 2001 ZIP-Einträge einschließlich Manifest,
Kompressionsverhältnis höchstens 100:1 und höchstens 10000 Fragen.

Manifest und Fragen werden gegen den lokal archivierten JSON-Schema-Vertrag
und zusätzliche semantische Regeln geprüft. Alle Fragen- und Antwort-IDs sind
eindeutig; sämtliche Lösungen und Medienverweise müssen existieren. Themen
sind eindeutig. Nicht referenzierte Medien werden zurückgewiesen. Unbekannte
Versionen nennen empfangenen und unterstützte Stände sowie den nötigen
Readerwechsel; es erfolgt kein Teilimport. Unbekannte Felder werden abgewiesen,
damit sie bei einem späteren Export nicht unbemerkt verloren gehen. Optionale
bekannte Felder werden erhalten; neue Felder benötigen einen versionierten Vertrag.

Der portable Python-Schema-Prüfer unterstützt ausdrücklich die Schlüsselwörter
der archivierten Schemas, keine beliebigen fremden Schemas oder Remote-Referenzen.
Die CI prüft diese Verträge zusätzlich mit dem unabhängigen Draft-2020-12-Validator
`jsonschema`. Dieser Prüfdienst ist keine Laufzeitabhängigkeit der Anwendung
oder der Offline-Helfer.

## Blockformat und Rechte

1.0.0 stabilisiert den Blockvertrag aus 0.2.0. Frage, Erklärung und Antworten
führen geordnete Text-/Bildblöcke; Bildantworten und Erklärungsbilder bleiben
an ihrer Position. Textzusammenfassungen entsprechen den Textblöcken, reine
Bildinhalte verwenden `[Bild]`. Fach, Thema, Frageversion und Auswahlmodus
werden ausdrücklich übertragen. Ziel-/Klassenstufe, Schwierigkeit, Themen,
Lizenz, Herkunft, Revision und Bearbeitungsvermerk bleiben getrennte Angaben.

Die Manifestlizenz ist eine Übersicht und überschreibt keine Einzellizenz.
Jede Frage und jedes Medium führen eigenständig Rechteinhaber, Attribution,
Lizenz und Herkunft; externe Quellen benötigen eine tatsächliche Revision,
Bearbeitungen zusätzlich einen Änderungsvermerk. AGPL-3.0-only gilt für die
Anwendung, nicht automatisch für importierte Daten. Abgeleitete Inhalte dürfen
nicht unter Verlust ihrer bestehenden Lizenz umetikettiert werden.
Integrität und technische Metadatenvalidierung beweisen keine tatsächlichen Rechte
oder Authentizität des Herausgebers. Konten, E-Mail-Adressen, Lernstände,
Gruppencodes und Zugangsdaten gehören nicht in den Inhaltsvertrag.

## Offline-Migration und ältere Zielversionen

Alle drei Versionen sind direkt durch den aktuellen Reader unterstützt.
Eine Datenbankmigration des Originalarchivs ist dafür nicht erforderlich.
Für eine separate Datei steht eine explizite, atomare Offline-Migration bereit:

```bash
python3 scripts/migrate-catalog.py alt.zip neu.zip
python3 scripts/migrate-catalog.py alt.zip blockformat.zip --target-version 0.2.0
```

Der dokumentierte Schritt **0.1.0 → 0.2.0 → 1.0.0** ergänzt bei alten Textfragen
ausdrückliche Blöcke. Alte Medien stehen wie beim bisherigen Import hinter dem
Fragetext. Fach/Thema werden aus dem ersten Thema abgeleitet (Anzeigegrenze
120 Zeichen), die vollständige Themenliste bleibt erhalten. Die Frageversion
stammt aus der Inhaltsversion; Lösungen, IDs, Medienbytes, Lizenzen und
Quellennachweise bleiben erhalten. `origin` führt das komplette alte Manifest
außer der Dateiliste mit. 0.2.0 → 1.0.0 stabilisiert diesen Vertrag, ohne
Frageinhalte umzuschreiben. Quellenstand und Inhaltsversion werden nicht erhöht.

Die Originaldatei bleibt bytegenau unverändert. Quelle und Ziel dürfen nicht
dieselbe Datei sein. Validierung vor und nach dem Schritt sowie atomarer
Dateiaustausch verhindern Teilresultate. Wiederholung erzeugt dieselben
Inhalte und ZIP-Bytes; bestehende Zieldateien bleiben bei Fehlern erhalten.

Ein ausdrückliches Ziel 0.2.0 ist möglich, wenn dessen Herkunftsvertrag die
Inhalte vollständig darstellen kann. Ein enthaltenes `origin` 1.0.0 verhindert
diesen Abwärtsexport. Eine Umwandlung blockbasierter Fragen nach 0.1.0 wird
wegen Informationsverlust abgewiesen. Auch alte Inhalte außerhalb der
Blockgrenzen (etwa ein einzelner Text über 4000 Zeichen oder mehr als 20 Blöcke)
werden nicht gekürzt: Sie bleiben im Originalformat direkt lesbar, die separate
Migration wird verständlich abgewiesen. Die Darstellungsgrenzen der Anwendung
gelten weiterhin vor jeder Datenbankspeicherung.

## Dauerhafte Vertragsbeispiele und Prüfung

`tests/fixtures/catalog/<version>/golden.zip` enthält je Vertragsstand synthetische
Bilder, Sonderzeichen, Quellen und unterschiedliche Text-/Bildlizenzen.
`contract-lock.json` sperrt Archiv und Schemas über SHA-256. Die alten JSON-Beispiele
und Sperrdateien bleiben unverändert. Kein realer Fragenbestand liegt im App-Repo.
Golden-Pakete sind öffentlich nachvollziehbare Beispiele, keine realen Katalogprodukte.

Die verpflichtende Repository-Policy prüft jeden archivierten Stand, vollständige
und selektive Roundtrips, unabhängige Schema-Auswertung, Lösungen, Medien,
Attribution sowie die sequenzielle und direkte Migration. .NET prüft alle
Originalarchive mit dem produktiven Reader und die deterministischen Exporte.
Die gemeinsame Integration prüft zwei getrennte Docker-Stacks und zusätzlich
den tatsächlichen API-Stand von LearnPip 1.15.0 (Commit `719d82a6cd9072242ed3726ff5829bd303256683`)
als Exporter gegen die aktuelle App als Importer. Übertragen werden nur lokale
ZIP-Bytes, keine Datenbank oder Sitzung. IDs, Teilauswahl, Bildblöcke, Rechte,
Metadaten und idempotenter Reimport werden geprüft. Ein manuelles VM-/Screenreader-
Prüfprotokoll wird dadurch nicht ersetzt.

```bash
python3 -m pip install -r tests/ops/catalog-contract-requirements.txt
python3 -m unittest discover -s tests/ops -p 'test_catalog*.py' -v
python3 scripts/validate-catalog.py tests/fixtures/catalog/1.0.0/golden.zip
bash tests/ops/integration-smoke.sh
```

## Privater Import und lokaler Export

Unter **Kataloge und Inhalte > Fragenpaket importieren** ein lokales ZIP wählen,
Vorschau und Rechte lesen und ausdrücklich bestätigen. Erst die bestätigte
Dateiprüfsumme führt zur transaktionalen Speicherung. Wiederholte identische
Importe sind idempotent; eigene Bearbeitungen werden nicht überschrieben.
Originalarchive bleiben ausschließlich dem Eigentümerkonto zugänglich.

Unter **Fragen > Fragen auswählen und exportieren** eigene Fragen, Katalog oder
Teilauswahl mit Fach-, Themen-, Sprach- und Textfiltern wählen. Gespeicherte
Entwürfe haben Vorrang. Titel und Herausgeber werden ausdrücklich angegeben;
keine Ableitung aus E-Mail oder Konto. Nach Preview-Prüfsumme und bewusster
Rechtebestätigung entsteht ausschließlich ein lokaler ZIP-Download in 1.0.0.
Normale API-Exporte bieten keinen stillen Abwärtsexport; dafür die explizite
Offline-Migration mit Zielversion verwenden.

Originalfragen behalten ihre Nachweise. Bearbeitete Importe benötigen aktuelle,
an die konkrete Inhaltsfassung gebundene Einzelnachweise; Originalattribution
und Bildlizenzen bleiben erhalten. Eigene Originale sind standardmäßig
`LicenseRef-Private`, keine offene Community-Lizenz. Fremde Fragen dürfen auch
mit Moderationsrechten nicht als eigener Massenexport abgegriffen werden.

Für die Lernanzeige gelten JPEG/PNG bis 5 MiB und 4096 × 4096 Pixel sowie
Alternativtexte bis 300 Zeichen. Andere Paketmedien und nicht darstellbare Inhalte
werden vor Speicherung vollständig abgewiesen. Originalbytes bleiben im Archiv;
Anzeigebilder werden zur Entfernung von Metadaten neu kodiert. Private Originalpakete
sind auf 20 Pakete/100 MiB je Konto begrenzt; das Medienkontingent gilt zusätzlich.
Backup und Kontolebenszyklus schließen Originalarchive und Historien ein.

API: `POST /api/v1/catalog-packages/preview`, `POST /api/v1/catalog-packages/import`,
`GET /api/v1/catalog-packages/`, `GET /api/v1/catalog-packages/{id}/original` und
`POST /api/v1/catalog-exports/preview` bzw. `/download`. Vorschau und Download
verwenden denselben konsistenten Lesestand; Inhaltsänderungen benötigen neue Zustimmung.

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
Option funktioniert Installation unverändert. Mit wiederholbarem
`--catalog-offer /absoluter/pfad/paket.zip` werden verfügbare lokale Pakete samt
Metadaten, Einzelquellen und Lizenztexten angezeigt und bleiben zunächst abgewählt.
Nur `yes` wählt das jeweilige Paket aus; mit `--yes` bleiben Angebote abgewählt.
Mehrere gewählte Dateien werden einzeln geprüft und mit Fortschritt bereitgestellt.
`--yes` bestätigt auch ausdrücklich
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
Attribution/Quellrevision zusätzlich mit. Unveränderte importierte Bilder behalten
auch in einer bearbeiteten Frage ihre Quelllizenz. Keine Lizenz wird aus einem
Thema erzeugt.

Klasse/Zielgruppe, Schwierigkeit und Themenhierarchie/Schlagworte können im selben
Bereich ausdrücklich erfasst werden. Die Exportauswahl kombiniert diese Angaben
mit Katalog, Fach, Thema und Sprache. Importierte Angaben bleiben verfügbar und
werden bei einer bewussten Metadatenänderung zusammen mit den Originalnachweisen
weitergegeben.

Frühere bestätigte Einzelnachweise bleiben in einer begrenzten Historie (100 Stände,
8 MiB je Frage) erhalten; bei Erreichen wird eine weitere Änderung abgewiesen,
kein alter Nachweis still gelöscht. Eine Community-Einreichung erhält zusätzlich
einen festen Nachweisstand, den Moderation und öffentliche Fassung anzeigen.
Spätere Änderungen der privaten Angaben verändern diesen Stand nicht.

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

Für die anschließende freiwillige Einreichung, getrennte Fach-/Rechteprüfung,
versionierte Freigaben und Rücknahmen steht ein
[separater Offline-Review-Prozess](COMMUNITY-CATALOGS.md) bereit. Er führt einen
Index ausschließlich freigegebener Paketfassungen, ohne automatisch zu publizieren.
