// <copyright file="CatalogPackageImportRevision.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>Bewahrt private Originalnachweise vor einem ausdrücklich bestätigten Paketupdate.</summary>
public sealed class CatalogPackageImportRevision
{
    /// <summary>Holt oder setzt die historische Fassungskennung.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Holt oder setzt das berechtigte Eigentümerkonto.</summary>
    public Guid OwnerAccountId { get; set; }

    /// <summary>Holt oder setzt die ursprüngliche Paketkennung.</summary>
    public string PackageId { get; set; } = string.Empty;

    /// <summary>Holt oder setzt das unveränderte Originalarchiv.</summary>
    public byte[] Archive { get; set; } = [];

    /// <summary>Holt oder setzt die ursprüngliche Zuordnung externer und lokaler Fragen.</summary>
    public string QuestionIdsJson { get; set; } = "{}";

    /// <summary>Holt oder setzt die letzte unpersönlich bearbeitete Inhaltsfassung.</summary>
    public string QuestionVersionIdsJson { get; set; } = "{}";

    /// <summary>Holt oder setzt den Zeitpunkt der Archivierung.</summary>
    public DateTimeOffset ArchivedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
