// <copyright file="QuestionVersionDetails.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api;

/// <summary>
/// Informationen zu einer Fragenfassung.
/// </summary>
/// <param name="Prompt">Den Fragetext.</param>
/// <param name="Id">Die eindeutige Kennung.</param>
/// <param name="Version">Die Versionsnummer.</param>
public sealed record QuestionVersionDetails(Guid Id, int Version, string Prompt);
