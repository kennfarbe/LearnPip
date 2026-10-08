// <copyright file="PrivateCatalog.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell PrivateCatalog.
/// </summary>
public sealed class PrivateCatalog
{
    /// <summary>
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt Kennung des Eigentümerkontos.
    /// </summary>
    public Guid OwnerAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt Name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Holt oder setzt die optionale Beschreibung.</summary>
    public string? Description { get; set; }

    /// <summary>Holt oder setzt die zusätzlichen Fragenzuordnungen.</summary>
    public ICollection<QuestionCatalogMembership> Memberships { get; set; } = [];

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt questions.
    /// </summary>
    public ICollection<Question> Questions { get; set; } = [];
}
