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
    /// Ruft study group id ab oder legt den Wert fest.
    /// </summary>
    public Guid StudyGroupId { get; set; }

    /// <summary>
    /// Ruft private catalog id ab oder legt den Wert fest.
    /// </summary>
    public Guid PrivateCatalogId { get; set; }

    /// <summary>
    /// Ruft shared by account id ab oder legt den Wert fest.
    /// </summary>
    public Guid SharedByAccountId { get; set; }

    /// <summary>
    /// Ruft shared at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset SharedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft study group ab oder legt den Wert fest.
    /// </summary>
    public StudyGroup StudyGroup { get; set; } = null!;

    /// <summary>
    /// Ruft private catalog ab oder legt den Wert fest.
    /// </summary>
    public PrivateCatalog PrivateCatalog { get; set; } = null!;
}
