# Fragenberechtigungen

Administratoren öffnen **Administration → Benutzer und Rollen:
Fragenberechtigungen**. Die Rechte für Benutzer und Moderatoren lassen sich
einzeln ändern. Vor dem Speichern erscheint eine Übersicht aller Auswirkungen.
„Standardwerte wiederherstellen“ füllt die Vorgaben wieder ein; gespeichert
werden sie erst nach Prüfung der Vorschau.

## Standardmatrix

| Aktion | Benutzer | Moderator | Administrator |
| --- | --- | --- | --- |
| Eigene Fragen erstellen, lesen, bearbeiten, löschen | Ja | Ja | Ja |
| Öffentlich oder für eigene Gruppen freigegebene fremde Fragen lesen | Ja | Ja | Ja |
| Fremde private, Gruppen- und öffentliche Fragen im Moderationsbereich lesen | Nein | Ja | Ja |
| Fremde Fragen als neue private Fassung bearbeiten | Nein | Ja | Ja |
| Fremde Fragen löschen | Nein | Ja | Ja |
| Einreichungen freigeben, Freigaben sperren, Meldungen bearbeiten | Nein | Ja | Ja |
| Berechtigte Fragen gemeinsam löschen | Nein | Ja | Ja |
| Eigene Fragenpakete importieren und exportieren | Ja | Ja | Ja |
| Eigene Fragen zur Community-Freigabe einreichen | Ja | Ja | Ja |
| Konten, Rollen, Rechte oder Instanzeinstellungen verwalten | Nein | Nein | Ja |

Die Fragenrechte der ersten beiden Rollen sind konfigurierbar. Gruppenrollen
verleihen keine globale Moderationsberechtigung. Administratorrechte werden nicht
über diese Matrix geändert. Der letzte aktive Administrator bleibt geschützt.

## Private Inhalte und Zweckbindung

In **Alle Fragen im Moderationskontext** zuerst einen konkreten Prüfzweck
mit 10–500 Zeichen eintragen. Übersicht, Vorschau, Bilder und Änderungen werden
protokolliert. Der Bereich umfasst auch unveröffentlichte Entwürfe.
Konten, Lernstände, Passwörter und Gruppencodes werden dort nicht angezeigt.
Private Inhalte erscheinen nicht automatisch im normalen Lernbereich.

Weitreichende Erweiterungen brauchen eine separate Bestätigung. Die erste
Aktivierung privater Inhaltsrechte benötigt zusätzlich die Bestätigung des
Betreiberhinweises. Betreiber prüfen Zweck, Datenminimierung, Datenschutzhinweise
und Rollenvergabe. Die Vergabe der Moderatorenrolle über die API liefert bei
privaten Inhaltsrechten zunächst `privacy_confirmation_required` mit diesem
Hinweis. Erst nach dessen Prüfung wird mit `privacyConfirmed=true` bestätigt.

Private Moderation benötigt `readForeign` und `readPrivate`; Bearbeiten und
Löschen benötigen außerdem das jeweilige eigene oder fremde Recht. Import setzt
`create` und `import` voraus; Export benötigt `readOwn` und `export`.
Community-Einreichungen benötigen `editOwn` und `community`. Gruppenfreigaben
benötigen `readOwn` und `editOwn`. Eigenes Lernen prüft `readOwn` auch beim Abruf
einer bereits gestarteten Sitzung. Öffentlich angebotene anonyme Inhalte bleiben
öffentlich; Rollenrechte sind keine Zugangssperre für öffentliches Material.

## Änderungen, Freigaben und Löschung

Eine Bearbeitung erzeugt eine neue private Fassung mit Bearbeiter und Zeitpunkt.
Bei Fremdbearbeitung sind Grund und geprüfte Versionsnummer erforderlich.
Zwischenzeitliche Änderungen führen zu einem Konflikt statt zum Überschreiben.
Die vorherige Fassung bleibt erhalten; ein ursprünglicher Entwurf bleibt als
Entwurf erhalten. Herkunft, Lizenz und Attribution werden bei Fremdbearbeitung
übernommen. Bei importierten Fragen bleiben diese Angaben auch bei eigener
Bearbeitung erhalten. Das importierte Originalpaket bleibt unverändert und getrennt
herunterladbar. Eine neue Fassung benötigt eine neue öffentliche Prüfung.

„Freigaben sperren und zurückziehen“ entfernt öffentliche und Gruppenfreigaben
für die betreffende Frage. Löschen benötigt Grund und ausdrückliche Bestätigung.
Fragen werden zunächst als gelöscht markiert; bestehende Referenzen und
Auditinformationen bleiben erhalten. Auch im Meldungsbereich ist die Bestätigung
verpflichtend. Sammellöschung ist auf 100 Fragen begrenzt und atomar: Ist nur eine
Frage nicht berechtigt oder nicht vorhanden, wird keine Frage gelöscht.

Inhaltszugriff verleiht **keine Weiterverbreitungsrechte**. Der ZIP-Export bleibt
auf eigene Inhalte und zulässige, dem eigenen Konto zugeordnete Importinhalte
beschränkt. Moderatorenrechte ermöglichen keinen fremden Massenexport.
Bearbeitete Importfragen bleiben vom Auswahl-Export ausgeschlossen, solange
vollständige getrennte Bearbeitungslizenzen und Herkunftsnachweise fehlen.

## Betrieb und Prüfung

Die Datenmigration übernimmt die Standardmatrix für vorhandene Instanzen.
Vorhandene Einzelwerte werden nicht überschrieben. Jede Aktion wird anhand der
aktuellen Datenbankwerte geprüft, ohne Rollen- oder Berechtigungscache.
Fehlende oder ungültige Einzelwerte verweigern das betreffende Recht.
Rechteänderungen speichern Administrator, Zeitpunkt, vorherige und neue Matrix
sowie einen optionalen Grund. Die Änderung wirkt sofort für aktive Sitzungen.
Eine veraltete Administrationsvorschau kann nicht gespeichert werden.

Die automatisierten Tests prüfen eigene/fremde private, Gruppen- und öffentliche
Fragen, Entwürfe, Medien, direkte API-Aufrufe, Import/Export, bestätigte
Sammellöschung, Rollenentzug, fehlende Konfiguration, Audit und Versionskonflikte.
Oberflächentests prüfen die mobile Rechteverwaltung, getrennte Bestätigungen,
Rücksetzen, Fremdbearbeitung und gesperrte Benutzeraktionen.
