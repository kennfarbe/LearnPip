// <copyright file="GroupCatalogShare.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell GroupCatalogShare.
/// </summary>
public sealed class GroupCatalogShare
{
    /// <summary>
    /// Holt oder setzt study group id.
    /// </summary>
    public Guid StudyGroupId { get; set; }

    /// <summary>
    /// Holt oder setzt private catalog id.
    /// </summary>
    public Guid PrivateCatalogId { get; set; }

    /// <summary>
    /// Holt oder setzt shared by account id.
    /// </summary>
    public Guid SharedByAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt shared at utc.
    /// </summary>
    public DateTimeOffset SharedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt study group.
    /// </summary>
    public StudyGroup StudyGroup { get; set; } = null!;

    /// <summary>
    /// Holt oder setzt private catalog.
    /// </summary>
    public PrivateCatalog PrivateCatalog { get; set; } = null!;
}
