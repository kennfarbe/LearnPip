# Versionierte Prüfungsprofile und Katalogfassungen (LP-24)

Administrierende importieren eine Katalogfassung über `POST /api/v1/exams/admin/catalogs/import` und veröffentlichen eine unveränderliche Profilfassung über `POST /api/v1/exams/admin/profiles/versions`. Beide Aktionen erfordern eine frische Administratorsitzung. Der Import akzeptiert normalisiertes JSON, keine beliebigen fremden URLs oder ausführbaren Archive. Neue Fassungen verwenden denselben Katalogcode mit neuer Revision; bestehende Fassungen können nicht überschrieben werden. Quell-URL, Namensnennung, Lizenz, Revision und Änderungsdatum werden in der Katalogliste angezeigt.

Die Bundesnetzagentur veröffentlicht Fragen der Amateurfunkklassen N/E/A als PDF und maschinenlesbares ZIP unter **DL-DE BY 2.0**. Importe müssen aus einer berechtigten Quelle erstellt und mit der vorgeschriebenen Namensnennung versehen werden. LearnPip beansprucht keinen offiziellen Prüfungsstatus. Fragebestände anderer Anbieter dürfen nicht allein wegen derselben Themen übernommen werden. Siehe [Bundesnetzagentur](https://www.bundesnetzagentur.de/Amateurfunk) und [Lizenzbedingungen](https://www.govdata.de/dl-de/by-2-0). Die Administration bestätigt die Wiederverwendungsrechte; als Inhaltslizenzen akzeptiert der Import DL-DE-BY-2.0, CC-BY-4.0 und CC0-1.0. Offizielle Fragen werden nicht mitgeliefert.

## Beispiel für einen normalisierten Katalogimport

Beispieltexte ersetzen und für jeden Profilteil genügend Fragen bereitstellen:

```json
{
  "code": "AFU", "title": "Amateurfunk", "revision": "2024-03-edition-3",
  "sourceUrl": "https://www.bundesnetzagentur.de/Amateurfunk",
  "license": "DL-DE-BY-2.0", "attribution": "Bundesnetzagentur, Prüfungsfragen Amateurfunk",
  "changedOn": "2024-03-20", "rightsConfirmed": true,
  "questions": [
    { "code": "B001", "partCode": "B", "prompt": "Beispieltext ersetzen",
      "answers": ["Antwort A", "Antwort B"], "correctIndex": 0 }
  ]
}
```

## Profilfassung und Prüfungsteile

Eine Profilfassung bindet die Kennung der Katalogfassung und Regeln für einzelne Prüfungsteile fest ein. Notwendige richtige Antworten und Zeitlimits werden von der Administration konfiguriert und müssen vor der Veröffentlichung anhand der jeweils geltenden Prüfungsregeln überprüft werden. Jeder Teil besitzt einen eigenen Fragenpool-Code. Für die Amateurfunkklassen N, E und A werden **B**, **V** und jeweils **T-N**, **T-E** oder **T-A** verwendet. Bereits bestandene Teile können gemäß den anwendbaren Regeln angerechnet werden; die tatsächlichen amtlichen Anforderungen können sich ändern und sind nicht als unveränderliche Schwellenwerte anzusehen.

```json
{
  "code": "AFU-N", "title": "Amateurfunk Klasse N", "amateurClass": "N",
  "catalogEditionId": "<Kennung aus dem Katalogimport>",
  "parts": [
    { "code": "B", "title": "Betriebliche Kenntnisse", "catalogPartCode": "B",
      "questionCount": 25, "timeLimitMinutes": 45, "requiredCorrect": 19, "creditCode": "B" },
    { "code": "V", "title": "Vorschriften", "catalogPartCode": "V",
      "questionCount": 25, "timeLimitMinutes": 45, "requiredCorrect": 19, "creditCode": "V" },
    { "code": "T-N", "title": "Technik N", "catalogPartCode": "T-N",
      "questionCount": 25, "timeLimitMinutes": 45, "requiredCorrect": 19, "creditCode": "T-N" }
  ]
}
```

**Die Zahlen sind ausschließlich beispielhafte Konfigurationswerte**, keine Aussage zu derzeit amtlich geltenden Bestehensgrenzen. `PUT /api/v1/exams/credits` speichert vom Nutzer selbst angegebene bestandene Teile; dies ist kein amtlicher Nachweis. Nur ein exakt zu einem Profilteil passender Code wird berücksichtigt.

Eine Simulation bindet sich an eine feste Profilfassung und speichert die gewählten Fragetexte, Antwortoptionen und richtigen Indizes als Snapshot. Vor dem Abschluss liefert die API nur Aufgaben und Optionen des aktiven Teils; die Lösung verbleibt serverseitig. Jeder notwendige Teil wird separat bewertet. Ergebnisse bleiben ihrer gespeicherten Profilfassung zugeordnet, auch wenn später neue Katalogfassungen importiert werden. Bei der Kontolöschung werden Simulationen und angerechnete Teile entfernt.
