# Bereitschaftsfenster und Prüfungstermine (LP-26)

`GET /api/v1/exams/profiles/{id}/forecast` benötigt ein aktives Konto und berechnet eine **Lernprognose**, keine amtliche Zulassung oder Erfolgswahrscheinlichkeit. Sie verwendet nur die angeforderte unveränderliche Profilfassung:

- Katalogabdeckung: unterschiedliche beantwortete Original-Fragegruppen aus Simulationen und Powertests dieser Fassung, einschließlich Varianten über `baseCode`, geteilt durch die Original-Fragegruppen der Profiltteile. Das ist Kontakt mit Fragen, nicht eine richtig beantwortete Quote.
- Wiederholungen: eigene Lerninhalte mit drei zeitversetzten sicheren Antworten nach dem LP-23-Modell. Diese Inhalte sind derzeit nicht zuverlässig den offiziellen Katalogfragen zugeordnet und werden deshalb ausdrücklich als separater, kontoweiter Hinweis angezeigt.
- Simulationen: mindestens die **zwei letzten** abgeschlossenen Simulationen genau dieser Profilfassung innerhalb von 30 Tagen müssen alle erforderlichen Teile bestanden haben. Powertests zählen hier nicht als Prüfungssimulation.

Erst bei mindestens 80 % Abdeckung, mindestens 70 % solcher Wiederholungen aus mindestens drei eigenen Inhalten und zwei jüngsten bestandenen Simulationen wird ein grobes Bereitschaftsfenster **7 bis 28 Tage ab heute** angezeigt. Die Schwellen und der Puffer sind eine transparente Produkthypothese, keine wissenschaftlich kalibrierte Wahrscheinlichkeit. Bei fehlenden Daten bleibt das Fenster unbestimmt; die Antwort erläutert die Gründe. Die Prognose wird bei jedem Abruf neu berechnet und speichert keine zusätzliche personenbezogene Kopie.

Eine neue Profilfassung kann bei `POST /api/v1/exams/admin/profiles/versions` neben den Teilen `rulesSourceUrl` (HTTPS), `rulesCheckedOn` und `sessions` erhalten. Beispiel:

```json
{
  "code": "AFU-N", "title": "Amateurfunk Klasse N", "amateurClass": "N",
  "catalogEditionId": "<Katalog-ID>", "parts": ["<vollständige Profilteile>"],
  "rulesSourceUrl": "https://www.bundesnetzagentur.de/Amateurfunk",
  "rulesCheckedOn": "2026-09-29",
  "sessions": [
    { "date": "2026-11-10", "place": "Beispielort", "registrationDeadline": null,
      "sourceUrl": "https://www.bundesnetzagentur.de/amateurfunk-termine.html",
      "checkedOn": "2026-09-29" }
  ]
}
```

Das Beispiel enthält **keinen bestätigten echten Termin**. Die Administration trägt nur tatsächlich aus der verlinkten Quelle geprüfte Termine ein. `registrationDeadline: null` heißt **nicht veröffentlicht/unbekannt**, nicht „Anmeldung jederzeit möglich“. Jeder Termin braucht Ort, HTTPS-Quelle und Prüfdatum; ein bekanntes Fristdatum darf nicht nach dem Prüfungstag liegen. Für ältere Profilfassungen bleiben Termine und Quellen leer. Geänderte Termine oder Regeln werden durch Veröffentlichen einer neuen Profilfassung mit neuer Versionsnummer und neuem Quellenstand sichtbar; ältere Fassungen und Simulationen bleiben nachvollziehbar.

Ein Termin wird nur als Vorschlag gezeigt, wenn er nach Beginn des Bereitschaftsfensters liegt, die bekannte Anmeldefrist noch offen ist und sowohl Terminquelle als auch Regelstand innerhalb der letzten 30 Tage geprüft wurden. Auch danach muss die Person Termin, Plätze, Anforderungen und Antrag bei der zuständigen Prüfungsstelle selbst kontrollieren. Die API bietet keine Anmeldung an und behauptet nie, formale Zulassung geprüft zu haben. Die [Bundesnetzagentur](https://www.bundesnetzagentur.de/Amateurfunk) veröffentlicht die offiziellen Informationen und [Prüfungstermine](https://www.bundesnetzagentur.de/amateurfunk-termine.html).
