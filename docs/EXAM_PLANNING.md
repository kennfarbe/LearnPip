# Lernziel- und Prüfungsplanung (LP-23)

`POST /api/v1/learning/exam-plan/estimate` schätzt genau einen fehlenden Wert aus `scopeContents` (Lernumfang), `dailyMinutes` (tägliche Lernzeit) und `targetPercent` (Zielanteil), wenn die beiden anderen vorliegen. Der Endpunkt speichert keinen Plan und behauptet keine Wahrscheinlichkeit, eine Prüfung zu bestehen. Die Antwort enthält eine Basisschätzung, eine vorsichtigere Spanne und gegebenenfalls Vorschläge zur Anpassung eines nicht erreichbaren Ziels.

Der Lernumfang besteht aus unterschiedlichen **IDs von Lerninhalten** und ist weder die Anzahl von Fragen noch wiederholte Klicks. Die Auswahl wird mit den zugänglichen Inhalten des angemeldeten Kontos abgeglichen. Ein fest vorgegebener Umfang darf die ausgewählten Inhalte nicht übersteigen; wird der Umfang gesucht, ist das Ergebnis auf die verfügbaren Inhalte begrenzt.

Als bereits beherrscht gelten nur ausgewählte Inhalte, bei denen die jüngste ununterbrochene Folge drei sichere richtige Antworten enthält: mit mindestens einem Tag zwischen der ersten und zweiten sowie mindestens drei Tagen zwischen der zweiten und dritten Antwort. Raten, falsche Antworten und das Anzeigen einer Erklärung unterbrechen die Folge. Diese Schätzung ist daher strenger als die allgemeine Fortschrittsanzeige. Allein aus Zeitaufwand oder Fragenzahl wird keine Beherrschung abgeleitet.

## Annahmen des Kapazitätsmodells

Für jeden verbleibenden Inhalt rechnet das Modell mit 20 Minuten für Einführung und erste Antwort sowie je fünf Minuten für zwei fällige Wiederholungen (frühestens an Tag 1 und Tag 4). Fällige Wiederholungen gehen neuen Inhalten vor. Für Unterbrechungen reserviert das Basismodell 20 % und das vorsichtige Modell 40 % der Tageskapazität. Angegebene Schultage halbieren die verfügbare Zeit; an freien Ausfalltagen steht keine Kapazität zur Verfügung. Das Tageslimit gilt immer.

Ein optionaler Prüfungstag ist exklusiv – am Prüfungstag wird nicht mehr gelernt. Ohne Prüfungstag gilt `horizonDays = 28`. Eingaben sind auf 365 Tage, 480 Minuten täglich und 2.000 Inhalte begrenzt.

Das Ergebnis ist eine **Planungshilfe, keine Bestehensprognose**. Schwierigkeitsgrad, Merkfähigkeit und individuelles Lerntempo können abweichen; die vorsichtige Spanne ist kein statistisch kalibriertes Konfidenzintervall. Bei einem Ziel außerhalb der modellierten Kapazität werden ein späterer Termin (innerhalb eines Jahres), eine realistische tägliche Lernzeit unterhalb des Limits, weniger Inhalte oder ein kleinerer Zielanteil vorgeschlagen. Die Oberfläche zeigt die Annahmen an. Die Schätzung wird bei jedem Abruf anhand des aktuellen Lernverlaufs neu erstellt.
