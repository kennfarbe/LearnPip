# Datenexport und Kontoauflösung (LP-34)

Unter „Dein Konto“ kann ein angemeldeter Nutzer einen JSON-Export herunterladen.
Er enthält Kontometadaten, verknüpfte Identitäten, eigene Kataloge, Fragen,
Entwürfe, veröffentlichte Versionen und Antworten, Lernsitzungen, chronologisch
sortierte Lernversuche mit Auswahl und Bewertung, Gruppenmitgliedschaften sowie
eine Liste privater Bilder. Geheimnisse, Sitzungstokens, AI-Schlüssel und
Passworthashes fehlen absichtlich. Bildbytes lädt der Kontoinhaber einzeln über
`GET /api/v1/media/{id}/content` herunter; der JSON-Export enthält keine
Base64-Bilder. Die Antwort wird nur authentifiziert und mit `no-store` geliefert.
Große Exporte sollten an einem vertrauenswürdigen Gerät gespeichert und geschützt
werden. Die API begrenzt den Export auf fünf Abrufe je Konto und Stunde.

Die freiwillige Auflösung benötigt eine aktive Sitzung, die Bestätigung `DELETE`
und das gültige Wiederherstellungsgeheimnis. Wer das Geheimnis nicht mehr besitzt,
muss sich zunächst erneut über einen vorhandenen Anmeldeweg authentifizieren
und kann es dann binnen 15 Minuten über die Recovery-Rotation erneuern. Der
Löschdienst löscht in einer Datenbanktransaktion private Fragen und Bilder,
Lernverlauf, Identitäten, Sitzungen, Familien- und Gruppenbeziehungen. Nach
Erfolg ist keine Anmeldung oder Wiederherstellung dieses Kontos mehr möglich.
Vor der Löschung Daten und Bilder exportieren. Die Selbstlöschung nimmt keine
Rücksicht auf die Inaktivitätsschwellen; sie kann nicht rückgängig gemacht werden.

Mit einer offenen Lizenz veröffentlichte Inhalte können bereits von anderen
kopiert worden sein. Diese rechtmäßig erhaltenen Kopien außerhalb der Instanz
lassen sich durch die Löschung **nicht** zurückrufen. Beiträge in der lokalen
Datenbank und Gruppenfreigaben werden mit dem Konto entfernt; dies kann
Gruppeninhalte und Antworten anderer Lernender betreffen. Sicherungen laufen
lokal und extern nach 30 Tagen ab. Nach einem Restore muss vor erneuter
öffentlicher Freigabe der Löschlauf ausgeführt werden. Freiwillige sofortige
Auflösungen müssen zusätzlich anhand eines privaten Löschregisters erneut
angewendet werden; der Inaktivitätslauf allein erkennt sie nicht zuverlässig.
Die konkrete Lösch-/Restorekontrolle ist vor organisatorischem Betrieb
nachzuweisen; siehe [Betriebsnachweis](privacy/EU-OPERATING-RECORD.md).

Für Missbrauchsschutz gelten bestehende Größen- und Bildtypgrenzen plus
höchstens 100 aktive private Bilder bzw. 100 MiB pro Konto. Ein DB-Lock schützt
bei parallelen Uploads vor Überschreiten. Neue Uploads, Frageerstellung und
Kommentare sind zusätzlich mit 30 Aktionen je Konto pro zehn Minuten limitiert;
Anmelde- und E-Mail-Codes bleiben über das eigene IP-Zeitfenster begrenzt und
fehlgeschlagene Codeversuche werden gesperrt. Ein 429 signalisiert eine
überschrittene Schreibrate. Diese Grenzen sollten beim öffentlichen Betrieb
anhand von Metriken für 429-Antworten überprüft werden.
