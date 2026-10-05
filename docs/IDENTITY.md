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
| `DELETE /api/v1/auth/providers/{provider}/link` | Apple-, Microsoft-, GitHub- oder Facebook-Verknüpfung nach aktiver Anmeldung trennen |

Externe Verknüpfungen lassen sich über `DELETE /api/v1/auth/providers/{provider}/link` trennen; zulässige Provider sind `apple`, `microsoft`, `github` und `facebook`. Die API verlangt eine aktive Sitzung und verweigert das Entfernen, wenn dadurch das letzte dauerhafte Anmelde- oder Wiederherstellungsmittel verloren ginge. Nach erfolgreicher Trennung werden Fragen, Lernstand und die übrigen Kontozugänge nicht verändert.

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

## Optionale externe Identitätsanbieter (#95, #104–#107)

Die bisherige `Oidc:*`-Konfiguration bleibt erhalten. Apple und Microsoft verwenden getrennte OIDC-Schemas; GitHub und Facebook Login verwenden eigene OAuth-Code-Flows, weil diese Integrationen keine beliebigen OIDC-Issuer sind. Alle Anbieter haben eigene Callback-Adressen, serverseitige Client-Secrets, getrennte Provider-Kennungen und dieselbe Kontenlogik: neue Identität = neues LearnPip-Konto; Verknüpfung nur aus einer aktiven LearnPip-Sitzung heraus. E-Mail-Adressen führen nie automatisch zu einer Kontenzusammenführung. Externe Tokens werden weder an den Browser ausgegeben noch in LearnPip-Sitzungen gespeichert.

Ein Anbieter wird erst bei vollständiger Serverkonfiguration registriert. Für OIDC erscheinen Apple und Microsoft dann in `GET /api/v1/auth/oidc/providers` und können über `/api/v1/auth/oidc/<name>/start` oder angemeldet über `/api/v1/auth/oidc/<name>/link/start` gestartet werden. GitHub und Facebook verwenden `/api/v1/auth/github/start`, `/api/v1/auth/github/link/start`, `/api/v1/auth/facebook/start` und `/api/v1/auth/facebook/link/start`. Unvollständige Konfiguration bietet keinen funktionslosen Anmeldeweg an. Die zentrale Anzeige und bewusste Admin-Bestätigung vor Aktivierung gemäß #103 bleiben separat.

Die Regressionstests prüfen gültige und ungültige Authority-Werte, explizite Microsoft-Tenant-Strategien, getrennte Schemas sowie die Abschaltung unvollständiger OAuth-Konfigurationen. Sie ersetzen keine echte Anmeldung mit beim jeweiligen Anbieter registrierten Apps und keine Ende-zu-Ende-Prüfung mit gültigen Betreiber-Secrets.

Für die Produktions-Compose-Konfiguration sind die optionalen Apple-/Microsoft-Parameter `LEARNPIP_APPLE_OIDC_AUTHORITY`, `LEARNPIP_APPLE_OIDC_CLIENT_ID`, `LEARNPIP_MICROSOFT_OIDC_AUTHORITY` und `LEARNPIP_MICROSOFT_OIDC_CLIENT_ID` in der geschützten `shared/.env.production` vorgesehen. Die jeweiligen Secrets liegen ausschließlich unter `shared/secrets/Oidc__Providers__apple__ClientSecret` beziehungsweise `shared/secrets/Oidc__Providers__microsoft__ClientSecret`; die Erstinstallation legt fehlende Dateien leer mit eingeschränkten Rechten an und überschreibt vorhandene Werte bei Updates nicht. Nur vollständig und gültig konfigurierte Anbieter werden angezeigt. Die technische Einrichtung ersetzt weder die Registrierung und Freigabe beim jeweiligen Anbieter noch die Hinweise und Prüfungen vor Aktivierung gemäß #103.

Der Release-Installer führt die idempotente Secret-Initialisierung auch bei einem Versionsupdate aus. Fehlt auf einer Altinstallation eine neue optionale Anbieterdatei, wird sie leer angelegt; bereits befüllte Dateien, insbesondere bisherige Datenbank- und Anmeldegeheimnisse, bleiben erhalten. Ein leerer Anbieter-Schlüssel aktiviert keine Anmeldung.

### Anbieterbindung und Tenant-Prüfung

Apple akzeptiert ausschließlich den HTTPS-Issuer `https://appleid.apple.com`. Registriere beim Anbieter die Services ID als Client-ID und `https://<öffentlicher-host>/signin-oidc-apple` als Rückrufadresse. Der Flow fordert `openid email` mit Authorization Code, PKCE und Apples erforderlichem `form_post` an. Dafür sind die Korrelations- und Nonce-Cookies auf `SameSite=None` und weiterhin `Secure` gesetzt. Die E-Mail kann beim ersten Login fehlen oder eine Apple-Relay-Adresse sein; der stabile Provider-`sub` ist der Kontoschlüssel und E-Mail-Gleichheit führt niemals zum Verknüpfen. Für die optionale Nutzung des privaten Relay-Postfachs müssen auch die beim Apple-Konto konfigurierten ausgehenden Mail-Domains/-Adressen freigegeben sein.

Apple verlangt ein signiertes ES256-Client-Secret-JWT; den Inhalt von `Oidc__Providers__apple__ClientSecret` musst du mit Team-ID, Services-ID, Key-ID und dem privaten Sign-in-with-Apple-Schlüssel erzeugen. Das JWT darf höchstens sechs Monate gültig sein. Vor Ablauf muss ein neues JWT erzeugt, in der Secret-Datei ersetzt und LearnPip neu gestartet werden. Der private .p8-Schlüssel gehört nicht in die LearnPip-Konfiguration oder ins Repository. Bei Rotation das neue Secret rechtzeitig testen; die Anwendung erzeugt das JWT nicht selbst. Siehe [Apple: Sign in with Apple für das Web konfigurieren](https://developer.apple.com/help/account/capabilities/configure-sign-in-with-apple-for-the-web/) und [Apple: Client Secret erzeugen](https://developer.apple.com/documentation/signinwithapple/creating-a-client-secret).

Microsoft verlangt eine ausdrücklich gesetzte `Authority` in `https://login.microsoftonline.com/<tenant>/v2.0`. Unterstützt werden ein einzelner Tenant (Tenant-ID), `organizations` für Arbeits-/Schulkonten aus Organisationen oder `consumers` für persönliche Microsoft-Konten; der mehrdeutige Wert `common` wird absichtlich abgelehnt. Die App-Registrierung muss zu dieser Strategie passen. Es werden keine Gruppen oder App-Rollen als LearnPip-Rechte übernommen. Callback ist `https://<öffentlicher-host>/signin-oidc-microsoft`. Siehe [Microsoft: unterstützte Kontotypen und Authority](https://learn.microsoft.com/en-us/entra/identity-platform/msal-client-application-configuration).

Die OIDC-Konfiguration verwendet nur die festgelegten Provider-Issuer/Authority-Endpunkte; ein beliebiger HTTPS-Issuer kann nicht durch den Anzeigenamen Apple oder Microsoft als Anbieter erscheinen. Die echte Validierung von App-Registrierung und Token-Antwort muss zusätzlich mit den realen Anbieter-Apps im Ende-zu-Ende-Test erfolgen.

### GitHub OAuth (separater Anmeldeweg)

Die GitHub-OAuth-App verwendet die feste Callback-Adresse `https://<öffentlicher-host>/signin-github`. Die Client-ID wird über `LEARNPIP_GITHUB_OAUTH_CLIENT_ID` und das Secret ausschließlich über `shared/secrets/GithubOAuth__ClientSecret` konfiguriert. Ohne vollständige Konfiguration ist `/api/v1/auth/github/start` nicht verfügbar. Für bewusste Verknüpfung mit einem bereits bestehenden Konto steht `/api/v1/auth/github/link/start` unter aktiver Sitzung bereit. Der Flow nutzt Authorization Code, PKCE und geschützten State; die API ruft mit dem kurzlebigen Zugangstoken serverseitig `https://api.github.com/user` auf, übernimmt nur die stabile numerische GitHub-ID und erstellt danach eine lokale widerrufbare LearnPip-Sitzung. Es werden keine Repository-, Organisations- oder E-Mail-Berechtigungen angefordert; Gleichheit von E-Mail-Adressen löst keine Kontozusammenführung aus. Der GitHub-Zugang ist eine eigene OAuth-Integration, **kein** generischer OIDC-Issuer. Vor Freigabe im Produktivbetrieb sind die GitHub-App-Registrierung und ein echter End-to-End-Test einschließlich Abbruch, Konflikt und Wiederanmeldung erforderlich. Ein bestehendes optionales Secret wird beim Upgrade nicht überschrieben.

### Facebook Login (eigener OAuth-Webflow)

Die optionale Meta-App erhält als Callback `https://<öffentlicher-host>/signin-facebook`. Die App-ID steht in `LEARNPIP_FACEBOOK_OAUTH_CLIENT_ID`, das App-Secret ausschließlich in `shared/secrets/FacebookOAuth__ClientSecret`. Der Anmeldeweg `/api/v1/auth/facebook/start` ist bei fehlenden Angaben nicht verfügbar. Mit aktiver Sitzung verknüpft `/api/v1/auth/facebook/link/start` die app-spezifische, über den Graph-Endpunkt `/me?fields=id` abgefragte Kennung ausdrücklich mit dem vorhandenen Konto. Keine Zusammenführung allein anhand von E-Mail-Adressen, keine angeforderten zusätzlichen Profil- oder Freundesberechtigungen; externes Zugangstoken wird nicht gespeichert. Vor Produktivfreigabe müssen die Meta-App-Einstellungen, Data-Deletion-Anforderungen, geltenden Freigaben und reale Ende-zu-Ende-Tests einschließlich Ablehnung und Konflikt geprüft werden.


## Erneute Anmeldung vor Identitätsänderungen (#95)

E-Mail-/Provider-Verknüpfung, Provider-Trennung und Recovery-Rotation verlangen
zusätzlich eine höchstens 15 Minuten alte, aktive lokale Sitzung. Normale
Lesezugriffe aktualisieren Aktivität, aber nicht den Authentifizierungszeitpunkt.
Bei HTTP 403 erneut mit Wiederherstellungsgeheimnis, verifiziertem E-Mail-Code,
Passwort oder vorhandenem Anbieter anmelden und den Vorgang neu starten.
Der externe Rücksprung prüft die initiierende Sitzung erneut auf Alter, Widerruf,
Deaktivierung und Ablauf. Rotation mit einer alten Sitzung kann diese Grenze
nicht umgehen. Inhalte und Kontokennung bleiben erhalten.

Die erneute Anmeldung ist keine pauschale MFA-Zusage. Die Testabnahme umfasst
lokale Kontoanlage, E-Mail-Verifizierung, Identitätskonflikte, Kontoinhalte,
Rollen-/Sitzungsentzug und alte Sitzungen. Produktive App-Registrierungen und
Anbieterbedingungen werden pro Betreiber geprüft.
