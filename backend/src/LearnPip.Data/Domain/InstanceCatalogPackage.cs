// <copyright file="InstanceCatalogPackage.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>Eine unveränderliche Paketfassung mit gesonderter Instanzfreigabe.</summary>
public sealed class InstanceCatalogPackage
{
    /// <summary>Holt oder setzt die Fassungskennung.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Holt oder setzt die unabhängige Paketkennung.</summary>
    public string PackageId { get; set; } = string.Empty;

    /// <summary>Holt oder setzt die Inhaltsfassung.</summary>
    public string CatalogVersion { get; set; } = string.Empty;

    /// <summary>Holt oder setzt den geprüften Archivfingerabdruck.</summary>
    public string ArchiveSha256 { get; set; } = string.Empty;

    /// <summary>Holt oder setzt das unveränderte validierte Archiv.</summary>
    public byte[] Archive { get; set; } = [];

    /// <summary>Holt oder setzt einen Wert, der angibt, ob die Fassung ausdrücklich für Lernende verfügbar ist.</summary>
    public bool Available { get; set; }

    /// <summary>Holt oder setzt den Zeitpunkt der bestätigten Aufnahme.</summary>
    public DateTimeOffset InstalledAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
