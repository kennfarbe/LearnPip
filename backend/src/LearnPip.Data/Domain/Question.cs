// <copyright file="Question.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell Question.
/// </summary>
public sealed class Question
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
    /// Holt oder setzt learning content id.
    /// </summary>
    public Guid? LearningContentId { get; set; }

    /// <summary>
    /// Holt oder setzt learning content.
    /// </summary>
    public LearningContent? LearningContent { get; set; }

    /// <summary>
    /// Holt oder setzt private catalog id.
    /// </summary>
    public Guid? PrivateCatalogId { get; set; }

    /// <summary>
    /// Holt oder setzt private catalog.
    /// </summary>
    public PrivateCatalog? PrivateCatalog { get; set; }

    /// <summary>Holt oder setzt sämtliche Katalogzuordnungen ohne Inhaltskopien.</summary>
    public ICollection<QuestionCatalogMembership> CatalogMemberships { get; set; } = [];

    /// <summary>
    /// Holt oder setzt draft.
    /// </summary>
    public QuestionDraft? Draft { get; set; }

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt Änderungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt Löschzeitpunkt in UTC, sofern vorhanden.
    /// </summary>
    public DateTimeOffset? DeletedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt owner.
    /// </summary>
    public Account Owner { get; set; } = null!;

    /// <summary>
    /// Holt oder setzt versions.
    /// </summary>
    public ICollection<QuestionVersion> Versions { get; set; } = [];
}
