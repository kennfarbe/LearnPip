// <copyright file="QuestionCatalogMembership.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>Ordnet eine Frage ohne Inhaltskopie einem privaten Katalog zu.</summary>
public sealed class QuestionCatalogMembership
{
    /// <summary>Holt oder setzt die Katalogkennung.</summary>
    public Guid CatalogId { get; set; }

    /// <summary>Holt oder setzt die Fragenkennung.</summary>
    public Guid QuestionId { get; set; }

    /// <summary>Holt oder setzt den Katalog.</summary>
    public PrivateCatalog Catalog { get; set; } = null!;

    /// <summary>Holt oder setzt die Frage.</summary>
    public Question Question { get; set; } = null!;
}
