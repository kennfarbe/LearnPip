# ADR 0002: PostgreSQL und private Inhalte

- Status: Angenommen
- Datum: 2026-09-29

## Kontext

Konten, Fragen, Antworten und Lernverläufe sind relational verknüpft. Fotos gehören zu privaten Lerninhalten. Gruppen und öffentliche Inhalte sind nicht Teil des ersten MVP.

## Entscheidung

- PostgreSQL ist die relationale Datenbank des MVP.
- Strukturierte Konten-, Fragen- und Fortschrittsdaten werden relational gespeichert.
- Fotos werden später in einem privaten Objektspeicher abgelegt; die Datenbank enthält nur Metadaten und eine nicht erratbare Referenz.
- Neue Inhalte bleiben privat. Öffentliches Teilen erfordert eine separate ausdrückliche Freigabe.

## Folgen

- Migrationen und Wiederherstellung müssen dokumentiert und geprüft werden.
- Datenbankabfragen geben keine Inhalte ohne Berechtigungsprüfung zurück.
- Ein künftiger Gruppen- oder öffentlicher Bereich erfordert zusätzliche Freigabe- und Rechteentscheidungen.
