// <copyright file="CatalogPackageChanges.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.CatalogPackages;

/// <summary>Beschreibt die inhaltlichen Unterschiede zweier geprüfter Paketfassungen.</summary>
/// <param name="NewQuestions">Neue stabile Fragekennungen.</param>
/// <param name="RemovedQuestions">Entfallene stabile Fragekennungen.</param>
/// <param name="ChangedQuestions">Gemeinsame Kennungen mit geänderten Inhalten oder Nachweisen.</param>
/// <param name="UnchangedQuestions">Gemeinsame Kennungen mit identischen Inhalten und Nachweisen.</param>
public sealed record CatalogPackageChanges(int NewQuestions, int RemovedQuestions, int ChangedQuestions, int UnchangedQuestions)
{
    /// <summary>Vergleicht Fragen, Medien und Rechte ohne Datenbankänderung.</summary>
    /// <param name="previous">Die bisherige Fassung oder kein bisheriges Paket.</param>
    /// <param name="current">Das vollständig validierte neue Paket.</param>
    /// <returns>Die vollständige Aufteilung der stabilen Fragekennungen.</returns>
    public static CatalogPackageChanges Compare(CatalogPackage? previous, CatalogPackage current)
    {
        var before = previous?.Questions.ToDictionary(question => question.GetProperty("id").GetString()!, StringComparer.Ordinal) ?? [];
        var after = current.Questions.ToDictionary(question => question.GetProperty("id").GetString()!, StringComparer.Ordinal);
        var shared = after.Keys.Intersect(before.Keys, StringComparer.Ordinal).ToArray();
        var unchanged = shared.Count(id => CatalogPackageComparison.Same(after[id], current, before[id], previous!));
        return new CatalogPackageChanges(after.Count - shared.Length, before.Count - shared.Length, shared.Length - unchanged, unchanged);
    }
}
