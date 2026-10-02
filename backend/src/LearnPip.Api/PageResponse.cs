// <copyright file="PageResponse.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api;

/// <summary>
/// Ergebnis einer paginierten Anfrage.
/// </summary>
/// <param name="Total">Die Gesamtanzahl der Elemente.</param>
public sealed record PageResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
