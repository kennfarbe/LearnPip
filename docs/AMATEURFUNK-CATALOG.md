# Optionaler Amateurfunkkatalog

Die offiziellen BNetzA-Daten, Importwerkzeuge, Grafiken, Lizenznachweise und
Daten-Releases gehören in das unabhängige Projekt
`kennfarbe/LearnPip-Catalog-Amateurfunk-DE` (Issue #111, Architektur #114).
LearnPip enthält diese Daten nicht und benötigt sie weder zum Bauen noch zum Betrieb.

Die Lernpakete verwenden das [stabile Katalogformat](CATALOG-INTERCHANGE.md).
Nach Prüfung und privatem Import unter „Kataloge und Inhalte“ kann unter
„Kurz lernen“ der Klassen- oder Fachkatalog gewählt werden. Gemeinsame Fragen
behalten in überlappenden Paketen identische IDs und Inhalte; unveränderte
Quellfragen können beim Import wiederverwendet werden. Der Quellenhinweis
enthält die amtliche ID, Klasse und Fassung. Die Paketvorschau und die Rechteansicht
zeigen Quelle, Namensnennung und Lizenz; unveränderte Inhalte behalten beim
erneuten Export ihre gespeicherte Provenienz.

Die Datenfassung März 2024 umfasst 1750 Fragen: 195 Technikfragen N,
463 zusätzliche E-Fragen, 716 zusätzliche A-Fragen, 172 Betriebsfragen und
204 Vorschriftenfragen. Die Klassen-Lernpakete enthalten die Grundlagen der
vorherigen Klassen. Das ist keine Festlegung amtlicher Prüfungspools oder Regeln.

## Darstellung und Grenzen

Allgemeine Frage-Textblöcke unterstützen `$...$` und `$$...$$` für LaTeX-Formeln.
KaTeX wird lokal mit HTML und zugänglichem MathML eingebunden; Formel-URLs,
HTML-Erweiterungen und externe Bilder bleiben gesperrt. Ungültige Formeln
werden als unveränderter Quelltext angezeigt. Die gespeicherten Texte werden
bei der Anzeige nicht geändert. Formel-Schriftdateien gehören zum öffentlichen
Offline-App-Rahmen; private Fragen und Medien werden weiterhin nicht vom
Service Worker gespeichert.

Der private Import unterstützt PNG/JPEG. Der Datenproduzent stellt daher
gekennzeichnete PNG-Darstellungen sowie die bytegenauen Originalquellen mit
SVG und zusätzlichen PNG bereit. Eine Rasterdarstellung erhält keine
verlustfreie Vektorinformation. Die Originalgrafiken bleiben in der separaten
Datenveröffentlichung zugänglich.

Pro Konto sind höchstens **2000 aktive private Bilder und 100 MiB Bilddaten**
zulässig. Das erlaubt auch einen vollständigen bildreichen Katalog und eine
weitere historische Bildfassung, sofern das Bytekontingent genügt. Upload,
Paketimport und Paketupdate nutzen dieselbe Anzahl-/Byteprüfung. Einzelbilder
bleiben auf 5 MiB, 4096 × 4096 Pixel und 16 Millionen Pixel beschränkt.
Originalpakete und deren Historie haben zusätzlich das bestehende Kontingent
von 20 Paketen/100 MiB. Bei einer Überschreitung gibt es keinen Teilimport.

Prüfungssimulationen verwenden einen getrennten normalisierten Vertrag;
diese ZIP-Lernpakete werden nicht automatisch zu Prüfungsprofilen.
Siehe [Prüfungsprofile](EXAM_PROFILES.md). LearnPip beansprucht keine amtliche
Zertifizierung oder Zusammenarbeit mit der Bundesnetzagentur.
