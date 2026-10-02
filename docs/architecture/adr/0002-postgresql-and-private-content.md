# ADR 0002: PostgreSQL und private Inhalte

- Status: Angenommen
- Datum: 2026-09-29

## Kontext

Konten, Fragen, Antworten und Lernverläufe sind relational verknüpft. Fotos gehören zu privaten Lerninhalten. Gruppenbezogene und moderierte öffentliche Inhalte sind inzwischen in der Anwendung modelliert.

## Entscheidung

- PostgreSQL ist die relationale Datenbank des MVP.
- Strukturierte Konten-, Fragen- und Fortschrittsdaten werden relational gespeichert.
- Private Medien werden derzeit mit Metadaten und Bilddaten in PostgreSQL gespeichert; ein externer Objektspeicher ist keine Voraussetzung.
- Neue Inhalte bleiben privat. Öffentliches Teilen erfordert eine separate ausdrückliche Freigabe.

## Folgen

- Migrationen und Wiederherstellung müssen dokumentiert und geprüft werden.
- Datenbankabfragen geben keine Inhalte ohne Berechtigungsprüfung zurück.
- Vorhandene Gruppen- und Veröffentlichungsabläufe erfordern weiterhin ausdrückliche Berechtigungs- und Rechteentscheidungen.
