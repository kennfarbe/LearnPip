# Optionale KI-Provider (LP-27)

LearnPip funktioniert ohne KI. Der Standardmodus `off` sendet keine Daten. Die bestehende manuelle Frageerstellung und die Lernfunktionen bleiben unabhängig. Eine KI-Anfrage enthält derzeit **nur den im KI-Feld ausdrücklich eingegebenen Text**; private Fragen, Kontodaten und Bilder werden weder automatisch ausgewählt noch hochgeladen. Bildanalyse und automatische Inhaltserstellung sind nicht Teil dieser Schnittstelle.

Die vier Modi sind `off`, `operator-cloud`, `operator-local` und `user-key`. Die verfügbaren Modi stehen in `Ai:AllowedModes` als kommagetrennte Liste, standardmäßig `off`. Ein nicht erlaubter oder unvollständig konfigurierter Modus wird abgelehnt. Die API wählt nie selbst einen Ersatzprovider. Das UI ruft vor der Anfrage `GET /api/v1/ai/modes` ab und zeigt Empfänger, Datenweg, Tagesquote, Verbrauch und maximale UTF-8-Textgröße. `POST /api/v1/ai/generate` verlangt den gewählten Modus, `prompt`, `confirmed: true` und die aktuelle `disclosureVersion` (`ai-text-v1`). Die Bestätigung gilt für genau diesen Text und Modus. Ein deaktivierter Modus verarbeitet nichts.

## Betreiberkonfiguration

Produktiv in `deploy/.env.production` (ohne Schlüsselwerte) beispielsweise:

```env
LEARNPIP_AI_ALLOWED_MODES=off,operator-cloud,operator-local,user-key
LEARNPIP_AI_CLOUD_ENDPOINT=https://api.example.org/v1/chat/completions
LEARNPIP_AI_CLOUD_MODEL=example-model
LEARNPIP_AI_LOCAL_ENDPOINT=http://ai-local:8080/v1/chat/completions
LEARNPIP_AI_LOCAL_MODEL=example-local-model
LEARNPIP_AI_MAX_INPUT_BYTES=8192
LEARNPIP_AI_CLOUD_DAILY_QUOTA=10
LEARNPIP_AI_LOCAL_DAILY_QUOTA=50
LEARNPIP_AI_USER_DAILY_QUOTA=20
```

Dies sind **Platzhalter**, keine mitgelieferten oder getesteten Provider. Die Endpunkte müssen ein JSON-Chat-Completion-Format mit `model`, `messages` und einer Antwort `choices[0].message.content` unterstützen. Das Cloud-Ziel muss HTTPS sein; URL und Modell bestimmt allein der Betreiber, nicht der Nutzer. `operator-local` akzeptiert einen Loopback-, privaten IP- oder einteiligen Dienstnamen über HTTP/HTTPS. Der lokale Dienst muss separat betrieben und vom API-Container erreichbar sein; bei Compose kann er demselben `frontend`-Netz beitreten. Das Beispiel `ai-local` existiert nicht automatisch. Auch ein lokaler Dienst erhält den angegebenen Text; Betreiber und Serverzugriff sind bei der Datenschutzbewertung zu berücksichtigen.

`deploy/secrets/Ai__CloudKey` enthält den Betreiber-API-Schlüssel; `deploy/secrets/Ai__KeyEncryptionKey` enthält einen stabilen Base64-kodierten 32-Byte-Schlüssel für die Benutzerschlüssel. `scripts/prod-init.sh` erstellt den zweiten Schlüssel einmalig und legt einen leeren Cloud-Schlüssel an. Den Verschlüsselungsschlüssel zusammen mit Datenbank-Backups sicher aufbewahren und nicht ohne Migrationsplan austauschen: Andernfalls sind vorhandene Benutzerschlüssel nicht mehr lesbar. BYOK (`user-key`) verwendet denselben fest konfigurierten Cloud-Endpunkt und das dort konfigurierte Modell, jedoch den persönlichen API-Schlüssel statt des Betreiber-Schlüssels. Der Schlüssel wird per `PUT /api/v1/ai/user-key` ersetzt und mit AES-256-GCM samt kontogebundener Authentifizierung verschlüsselt gespeichert; `DELETE` entfernt ihn. Die Listen-API meldet nur, ob ein Schlüssel vorhanden ist. Nach Kontolöschung werden Schlüssel und Nutzungszähler entfernt.

Die erlaubten Quoten sind 0–1000 Anfragen je Konto, Modus und UTC-Tag (0 deaktiviert); `Ai:MaxInputBytes` ist auf 1–32768 begrenzt. Der Server prüft die UTF-8-Größe vor dem Aufruf, reserviert die Quote **atomar in PostgreSQL vor** dem Provider-Aufruf und gibt bei Erschöpfung HTTP 429 zurück. Reservierte Versuche zählen auch bei einem Providerfehler, damit parallele Fehlversuche keine Kostenbegrenzung umgehen. Die Antwort ist auf 64 KiB Provider-JSON und 16000 Zeichen Ergebnistext begrenzt, die Laufzeit auf 30 Sekunden. HTTP-Weiterleitungen werden nicht verfolgt und ein fehlgeschlagener Provider löst keinen stillen Fallback aus. Für reale Geldbeträge zusätzlich die Anbieter-eigenen Ausgabenlimits konfigurieren: eine Anzahl Anfragen begrenzt keine Tokenpreise oder den Umfang der Providerantworten vollständig.

Der Adapter protokolliert weder Text noch Schlüssel oder Providerfehlerdetails; API-Antworten enthalten keinen Schlüssel. Betriebslogs und Datenbank-Backups trotzdem als vertraulich behandeln. Für die Entwicklungs-Compose-Datei können `LEARNPIP_AI_*`-Variablen in der ignorierten `deploy/.env` gesetzt werden. Produktionsschlüssel gehören ausschließlich in `deploy/secrets/`, nicht in Git oder die `.env.production`-Datei. Nach Konfigurationsänderungen die API neu erstellen. Der Nutzer kann jederzeit `off` wählen und ohne KI fortfahren.
