// <copyright file="ModerationBrowseInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Questions;

/// <summary>
/// Benennt den zu protokollierenden Moderationszweck der begrenzten Fragenübersicht.
/// </summary>
/// <param name="Reason">Der konkrete Prüfzweck.</param>
/// <param name="Page">Nullbasierte Seite mit höchstens 50 Ergebnissen.</param>
public sealed record ModerationBrowseInput(string Reason, int Page = 0);
