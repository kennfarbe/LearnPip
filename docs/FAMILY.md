# Familienverknüpfung und altersgerechte Freigaben

Die Altersgruppe wird je Konto einmalig als `minor` oder `adult` erklärt. Diese Selbstauskunft
ist **kein Nachweis** eines Verwandtschaftsverhältnisses. Eine falsch gesetzte Altersgruppe
braucht eine getrennte administrativ geprüfte Korrektur; sie kann nicht per API umgestellt werden.

1. Das angemeldete Kinderkonto erzeugt mit `POST /api/v1/family/invites` einen 24 Stunden
   gültigen, einmaligen Einladungscode. Nur der SHA-256-Hash wird gespeichert. Den Code
   vertraulich an die berechtigte erwachsene Person übermitteln.
2. Das als erwachsen deklarierte Konto nimmt ihn unter `POST /api/v1/family/invites/redeem`
   mit `{ "token": "..." }` an. Noch besteht keinerlei Familienzugriff.
3. Eine Administratorin oder ein Administrator prüft **außerhalb** der Anwendung die Identität,
   Volljährigkeit und Sorge- beziehungsweise Vertretungsberechtigung anhand eines dokumentierten
   Verfahrens. `POST /api/v1/admin/family/links/{id}/verify` mit `{ "reference": "CASE-..." }`
   speichert nur die Referenz des Prüfbelegs, keine Ausweisdaten oder Dokumente. Das prüfende
   Admin-Konto darf nicht selbst eines der beiden verknüpften Konten sein.
4. Das Kinderkonto bestätigt die geprüfte Verknüpfung über
   `POST /api/v1/family/links/{id}/confirm`. Erst dann ist sie aktiv. Beide Konten können
   mit `POST /api/v1/family/links/{id}/revoke` sofort widerrufen. Statuswechsel und Ziele
   werden mit Akteur und Zeit protokolliert.

`GET /api/v1/family/links/{id}/overview` ist ausschließlich für das aktive, geprüfte
Elternkonto freigegeben. Es enthält zusammengefassten Themenfortschritt, die Zahl der
abgeschlossenen Lerneinheiten und Lerntage der letzten 28 Tage sowie gemeinsam gepflegte
Ziele. Es gibt keine falschen Einzelantworten, Versuche, privaten Fragen, Medien oder
Erklärungen aus. Gruppenleitung und Moderation verleihen kein Familienrecht.

Ein Kinderbeitrag bleibt mit Status `minor_hold` privat, bis das aktive Elternkonto **diese
konkrete Fragenversion** unter `POST /api/v1/family/links/{id}/submissions/{versionId}/approve`
freigibt und danach die gewöhnliche Moderation zustimmt. Ein Widerruf vor der
Moderationsentscheidung blockiert die Veröffentlichung erneut. Das ist keine pauschale
Freigabe zukünftiger Beiträge. Ohne erfolgreiches externes Prüfverfahren keine
Familienverknüpfung aktivieren. Die Speicherung der Fallreferenz setzt ein lokales Verfahren
für Belegaufbewahrung und Löschung voraus.
