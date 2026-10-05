// <copyright file="CatalogPackageImport.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>Bewahrt Originalpaket und stabile Kennungen eines privaten Imports.</summary>
public sealed class CatalogPackageImport
{
    /// <summary>Holt oder setzt Kennung des Imports.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Holt oder setzt Eigentümerkonto.</summary>
    public Guid OwnerAccountId { get; set; }

    /// <summary>Holt oder setzt Privater Zielkatalog; Fragen bleiben bei dessen Löschung erhalten.</summary>
    public Guid? PrivateCatalogId { get; set; }

    /// <summary>Holt oder setzt Dauerhafte externe Paketkennung.</summary>
    public string PackageId { get; set; } = string.Empty;

    /// <summary>Holt oder setzt Unabhängige Inhaltsversion.</summary>
    public string CatalogVersion { get; set; } = string.Empty;

    /// <summary>Holt oder setzt Formatierungsunabhängiger Vergleich der Originalinhalte.</summary>
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>Holt oder setzt Unveränderte Originaldatei mit vollständigen Herkunftsnachweisen.</summary>
    public byte[] Archive { get; set; } = [];

    /// <summary>Holt oder setzt Zuordnung externer Fragekennungen zu lokalen Fragen.</summary>
    public string QuestionIdsJson { get; set; } = "{}";

    /// <summary>Holt oder setzt Zeitpunkt des bestätigten Imports.</summary>
    public DateTimeOffset ImportedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
