# Konten und Identitätswege

LearnPip unterstützt ein Konto ohne E-Mail-Adresse. Die erste Antwort von `POST /api/v1/auth/pseudonymous` enthält ein zufälliges Wiederherstellungsgeheimnis mit 256 Bit Entropie und eine Sitzung. **Das Geheimnis wird nur einmal angezeigt.** Ohne gespeichertes Geheimnis und ohne verknüpften weiteren Identitätsweg lässt sich das Konto nach dem Verlust aller Sitzungen nicht wiederherstellen. Die Datenbank speichert nur SHA-256-Hashes der hochentropen Geheimnisse und Sitzungstokens. Eine Sitzung läuft nach 30 Tagen ab und wird bei jeder API-Anfrage gegen die Datenbank geprüft; `logout` und `logout-all` widerrufen sie sofort. `recovery/rotate` ersetzt das Geheimnis, widerruft alle älteren Sitzungen und stellt eine neue Sitzung aus.

| Endpunkt | Zweck |
| --- | --- |
| `POST /api/v1/auth/pseudonymous` | Neues Konto, einmaliges Wiederherstellungsgeheimnis und Sitzung |
| `POST /api/v1/auth/recovery` | Neues Sitzungstoken mit dem langen Geheimnis |
| `POST /api/v1/auth/recovery/rotate` | Neues Geheimnis und Widerruf älterer Sitzungen |
| `GET /api/v1/auth/me` | Konto, letzte Aktivität und Deaktivierungszeit anzeigen |
| `POST /api/v1/auth/logout`, `POST /api/v1/auth/logout-all` | Aktuelle oder alle Sitzungen widerrufen |
| `POST /api/v1/auth/email/start`, `/complete` | Einmaligen E-Mail-Code für Anmeldung oder neues Konto verwenden |
| `POST /api/v1/auth/email/link/start`, `/complete` | Nach aktiver Anmeldung eine E-Mail-Adresse verifizieren und an dasselbe Konto binden |
| `GET /api/v1/auth/oidc/start`, `/link/start` | OIDC-Code-Flow starten oder mit aktivem Konto verknüpfen |

Die JSON-Antwort bei der Kontoanlage, Wiederherstellung und E-Mail-Anmeldung enthält ein Bearer-Token für API-Clients. Der Browser erhält außerdem ein `HttpOnly`-Cookie mit `Secure` und `SameSite=Strict`. Für Cookie-Anfragen mit schreibender Methode prüft die API den `Origin`-Header gegen `Authentication__PublicOrigin` oder, falls nicht gesetzt, den Request-Origin. Webclients speichern das Bearer-Token nicht in JavaScript-Speichern. Lokal über HTTP kann das Secure-Cookie nicht genutzt werden; zum API-Test ist das Bearer-Token vorgesehen. Für einen öffentlich erreichbaren Browserbetrieb muss TLS am Proxy/API-Endpunkt aktiv sein.

## E-Mail-Codes

E-Mail ist optional. Vor Aktivierung müssen SMTP über STARTTLS und ein eigener HMAC-Schlüssel gesetzt sein:

```sh
openssl rand -base64 32
```

Den Wert als `LEARNPIP_EMAIL_CODE_KEY` in der ignorierten `deploy/.env` ablegen. Zusätzlich sind `LEARNPIP_MAIL_HOST`, `LEARNPIP_MAIL_FROM`, `LEARNPIP_MAIL_USERNAME` und `LEARNPIP_MAIL_PASSWORD` nötig; `LEARNPIP_MAIL_PORT` ist standardmäßig 587. Ohne vollständige Konfiguration antworten die E-Mail-Endpunkte mit HTTP 503. Zugangsdaten gehören ausschließlich in die lokale Secret-Verwaltung, niemals ins Repository.

Ein Code hat sechs Ziffern und ist zehn Minuten gültig. Der Server speichert nur einen HMAC des Codes mit dem geheimen Schlüssel, akzeptiert ihn einmal und sperrt ihn nach fünf Fehlversuchen. Ein neuer Code macht den bisherigen Code für denselben Zweck ungültig. Pro Adresse und Zweck wird höchstens ein Code pro Minute und fünf pro Stunde versandt; Auth-Endpunkte haben zusätzlich ein IP-Limit. Die Startantwort ist für bekannte und unbekannte Adressen gleich. Nach verifiziertem E-Mail-Code kann ein neues Konto entstehen. Die Verknüpfung verlangt zusätzlich die aktuelle Sitzung; gehört die Adresse bereits einem anderen Konto, wird die Verbindung abgelehnt. Fragen und Lernstand verbleiben beim bestehenden Konto.

## OpenID Connect

Optional kann ein OIDC-Anbieter über `LEARNPIP_OIDC_AUTHORITY` (HTTPS-Issuer), `LEARNPIP_OIDC_CLIENT_ID` und `LEARNPIP_OIDC_CLIENT_SECRET` eingerichtet werden. Beim Anbieter ist die Redirect-URI `https://<öffentlicher-host>/signin-oidc` zu registrieren. Der Backend-Flow nutzt Authorization Code mit PKCE, validiert die ID-Token durch den OIDC-Handler und speichert keine externen Tokens in der LearnPip-Sitzung. `LEARNPIP_OIDC_POST_LOGIN_PATH` kann einen relativen Zielpfad enthalten, standardmäßig `/`. Im Produktionsbetrieb legt die API ASP.NET-Datenschutzschlüssel im persistenten Docker-Volume `data-protection-keys` ab. Der Volume-Inhalt muss über Neustarts und Updates erhalten bleiben und bei mehreren API-Instanzen mit passenden Dateirechten gemeinsam verfügbar sein. Die API bricht im Produktionsmodus ab, wenn das Schlüsselverzeichnis nicht bereitgestellt wurde. Schlüsseldateien sind sensible Daten; das Volume ist vor fremdem Zugriff zu schützen und in die geschützte Datensicherung aufzunehmen. Die Dateispeicherung verschlüsselt Schlüssel nicht automatisch im Ruhezustand; dafür ist der Schutz des Hosts und Datenträgers erforderlich. Die Compose-Entwicklungsumgebung ist weiterhin kein produktives Schlüsselmanagement.

Bei einer neuen externen Identität wird ein Konto angelegt. Nach `oidc/link/start` wird die externe Identität nur an das bereits angemeldete Konto gebunden; der geschützte OIDC-State enthält die ID der initiierenden Sitzung, die beim Rücksprung erneut als aktiv geprüft wird. Eine bereits einem anderen Konto zugeordnete Identität wird **nicht** automatisch zusammengeführt. Der Provider-Schlüssel leitet sich aus dem konfigurierten Issuer ab, und der externe `sub` wird nie als LearnPip-Konto-ID oder Rollenclaim übernommen. Die API gibt stattdessen eine lokale, widerrufbare Sitzung aus. Ein Gruppencode ist kein Identitätsnachweis und wird von keinem Login-Endpunkt akzeptiert.

**Upgrade älterer Installationen:** Beim regulären `install-release.sh update` wird das neue benannte Docker-Volume automatisch vor dem Migrationslauf angelegt und sowohl dort als auch in der API eingebunden. Vorhandene Volumes und Schlüssel werden nicht neu initialisiert oder überschrieben. Frühere Installationen ohne dauerhaft gespeicherte Data-Protection-Schlüssel besitzen möglicherweise noch laufende OIDC-Anmeldungen, deren temporärer State nach einem Neustart nicht mehr entschlüsselt werden kann; in diesem Fall muss der Anmeldevorgang erneut gestartet werden. Bereits bestehende LearnPip-Konten, Wiederherstellungsgeheimnisse und E-Mail-Anmeldungen werden dadurch nicht migriert oder zurückgesetzt. Bei Wiederherstellung eines Backups einer neueren Installation muss das zugehörige Schlüssel-Volume ebenfalls gesichert und wiederhergestellt werden; ein bloßer Datenbankdump reicht dafür nicht aus.

## Kontoinaktivität

Erfolgreiche authentifizierte API-Anfragen, auch Lesezugriffe, aktualisieren `LastActivityAtUtc`. Fehlgeschlagene Anfragen, öffentliche Seiten und Worker-Läufe zählen nicht. Die Kontoseite zeigt den letzten Aktivitätszeitpunkt und gegebenenfalls die Deaktivierung. Der tägliche Worker prüft die gespeicherte Aktivität unmittelbar vor jeder Aktion erneut. Nach 60, 76 und 87 Tagen sendet er jeweils höchstens eine Warnung per E-Mail, sofern eine verifizierte E-Mail-Adresse vorhanden und SMTP eingerichtet ist. Nach 90 Tagen ohne Aktivität deaktiviert er das Konto und widerruft Sitzungen. Eine erfolgreiche Anmeldung mit Wiederherstellungsgeheimnis, E-Mail-Code oder OIDC reaktiviert es. Nach weiteren 90 Tagen ohne Reaktivierung löscht er das Konto samt persönlichen Inhalten und Medien endgültig. Moderations- und Administrationskonten sind ausgenommen. Fehlgeschlagene oder mangels Mailkonfiguration ausgelassene Warnungen werden für diese Inaktivitätsphase nicht erneut versendet.

## Betrieb und Datenpflege

Die Migrationen legen Wiederherstellungs-, Sitzungs-, E-Mail-Code- und Inaktivitätswarnungs-Tabellen an. Für Backup/Restore gelten die Schritte in [DATABASE.md](DATABASE.md); die Tabellen enthalten sensible Anmelde-Metadaten. Ein produktiver Test mit dem gewählten OIDC- und SMTP-Anbieter ist vor öffentlichem Betrieb erforderlich.

## Mehrere optionale OIDC-Konfigurationen (Teilumsetzung #95)

Zusätzlich zur unveränderten bisherigen `Oidc:*`-Konfiguration lassen sich getrennte `Oidc:Providers:<name>:Authority`, `ClientId`, `ClientSecret` und optional `PostLoginPath` setzen. Die derzeit zugelassenen Namen sind `apple`, `microsoft`, `github` und `facebook`; **ein Name allein macht einen Dienst nicht OIDC-kompatibel**. Erst nach Prüfung eines konkreten, standardkonformen HTTPS-OIDC-Angebots darf er aktiviert werden. Insbesondere GitHub/Facebook benötigen gegebenenfalls eine eigene, später zu implementierende OAuth-Anbindung (#106/#107). Für konforme Konfigurationen erhält jeder Anbieter ein eigenes Authentifizierungsschema und einen eigenen Callback `/signin-oidc-<name>`, der beim Anbieter exakt hinterlegt werden muss. Nur vollständig konfigurierte Anbieter erscheinen unter `GET /api/v1/auth/oidc/providers` und haben `/api/v1/auth/oidc/<name>/start` sowie bei bestehender Anmeldung `/api/v1/auth/oidc/<name>/link/start`. Gleichlautende E-Mail-Adressen führen niemals zu einer Zusammenführung. Geheimnisse bleiben auf dem Server. Noch keine fertigen Anbieterintegrationen oder Aktivierungsoberfläche.
