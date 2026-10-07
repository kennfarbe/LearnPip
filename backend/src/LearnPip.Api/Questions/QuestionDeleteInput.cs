// <copyright file="QuestionDeleteInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Questions;

/// <summary>Explizit bestätigte einzelne oder gemeinsame Löschung.</summary>
/// <param name="QuestionIds">Eindeutige Fragenkennungen.</param>
/// <param name="Reason">Grund der Löschung.</param>
/// <param name="Confirmed">Getrennte Löschbestätigung.</param>
public sealed record QuestionDeleteInput(IReadOnlyList<Guid> QuestionIds, string Reason, bool Confirmed);
