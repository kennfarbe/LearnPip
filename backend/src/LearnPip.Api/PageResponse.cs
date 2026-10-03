// <copyright file="PageResponse.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api;

/// <summary>
/// Ergebnis einer paginierten Anfrage.
/// </summary>
/// <typeparam name="T">Der Typ der übertragenen Nutzdaten.</typeparam>
/// <param name="Total">Die Gesamtanzahl der Elemente.</param>
/// <param name="Items">Die Einträge der Seite.</param>
/// <param name="Page">Die aktuelle Seitennummer.</param>
/// <param name="PageSize">Die Anzahl von Einträgen pro Seite.</param>
public sealed record PageResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
