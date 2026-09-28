# Tests

Backend- und Webtests werden jeweils zusammen mit der betroffenen Komponente erstellt und in den unabhängigen CI-Jobs ausgeführt. Für API-Verhalten sind Unit- und Integrationstests vorgesehen; Integrationstests dürfen ihre Testdatenbank isoliert starten und keine gemeinsam genutzte oder produktive Datenbank verwenden.

Bis Testprojekte ergänzt werden, prüft der Backend-CI-Job, dass die Solution einschließlich API und Worker für Release gebaut und getestet werden kann. Webtests laufen über `npm test --if-present`; der Angular-Build ist verpflichtend.
