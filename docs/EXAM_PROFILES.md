# Versioned exam profiles and catalog editions (LP-24)

An administrator imports a catalog edition with `POST /api/v1/exams/admin/catalogs/import` and publishes an immutable profile version with `POST /api/v1/exams/admin/profiles/versions`. Both operations require a fresh admin session. The import accepts normalized JSON, not arbitrary remote URLs or executable archives. A new edition uses the same catalog code with a new revision; existing editions cannot be overwritten. The stored source URL, attribution, license, revision and change date appear in the catalog listing.

The Bundesnetzagentur publishes N/E/A questions as PDF and machine-readable ZIP under **DL-DE BY 2.0**. The import must be prepared from an authorized source and retain the attribution. The site does not claim official examination status. Do not copy another provider's questions merely because they cover the same material. [Bundesnetzagentur source](https://www.bundesnetzagentur.de/Amateurfunk), [license terms](https://www.govdata.de/dl-de/by-2-0). An administrator confirms reuse rights; imports accept DL-DE-BY-2.0, CC-BY-4.0 and CC0-1.0. There is no bundled copy of official questions.

Example normalized catalog payload (replace the example text and include enough questions for every profile section):

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

A profile version pins an edition ID and a list of section rules; required correct answers and time limits are configured by an admin and should be checked against the applicable examination rules before publication. Each section has a distinct question pool code. For Amateurfunk class N, E or A, the profile requires **B**, **V** and the corresponding **T-N**, **T-E** or **T-A** credit codes. N/E/A here model the class and eligible previously passed sections; actual official requirements may change and are not hard-coded as a score threshold. The source says every required part must pass individually and the technical part varies by class. [Bundesnetzagentur](https://www.bundesnetzagentur.de/Amateurfunk).

```json
{
  "code": "AFU-N", "title": "Amateurfunk Klasse N", "amateurClass": "N",
  "catalogEditionId": "<id from catalog import>",
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

The numbers in this example are *illustrative configuration data* and must be verified by the operator; they are not an assertion of current official examination thresholds. `PUT /api/v1/exams/credits` stores a user's **self-reported** already passed section code. It is not an official certificate and only an exact matching profile section is credited. A simulation pins the profile version and copies its selected question text, options and correct indices into a snapshot. Only the active section's prompt/options are sent before completion; correct answers stay server-side. Each required section is scored separately. Finished results remain attached to their saved version and snapshot even after subsequent catalog imports. Deleting an account removes its simulations and credits.
