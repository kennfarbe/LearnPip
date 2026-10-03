// <copyright file="QuestionDetails.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api;

/// <summary>
/// Details einer Frage einschließlich der neuesten Fassung.
/// </summary>
/// <param name="LatestVersion">Die neueste Fragenfassung, sofern vorhanden.</param>
/// <param name="Id">Die eindeutige Kennung.</param>
public sealed record QuestionDetails(Guid Id, QuestionVersionDetails? LatestVersion);
