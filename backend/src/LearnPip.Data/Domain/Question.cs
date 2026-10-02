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
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft Kennung des Eigentümerkontos ab oder legt den Wert fest.
    /// </summary>
    public Guid OwnerAccountId { get; set; }

    /// <summary>
    /// Ruft learning content id ab oder legt den Wert fest.
    /// </summary>
    public Guid? LearningContentId { get; set; }

    /// <summary>
    /// Ruft learning content ab oder legt den Wert fest.
    /// </summary>
    public LearningContent? LearningContent { get; set; }

    /// <summary>
    /// Ruft private catalog id ab oder legt den Wert fest.
    /// </summary>
    public Guid? PrivateCatalogId { get; set; }

    /// <summary>
    /// Ruft private catalog ab oder legt den Wert fest.
    /// </summary>
    public PrivateCatalog? PrivateCatalog { get; set; }

    /// <summary>
    /// Ruft draft ab oder legt den Wert fest.
    /// </summary>
    public QuestionDraft? Draft { get; set; }

    /// <summary>
    /// Ruft Erstellungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft Änderungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft Löschzeitpunkt in UTC, sofern vorhanden ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? DeletedAtUtc { get; set; }

    /// <summary>
    /// Ruft owner ab oder legt den Wert fest.
    /// </summary>
    public Account Owner { get; set; } = null!;

    /// <summary>
    /// Ruft versions ab oder legt den Wert fest.
    /// </summary>
    public ICollection<QuestionVersion> Versions { get; set; } = [];
}
