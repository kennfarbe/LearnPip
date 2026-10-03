// <copyright file="ModerationInspectInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Questions;

/// <summary>
/// Benennt den konkreten Zweck für den Moderationszugriff auf eine Fragenfassung.
/// </summary>
/// <param name="Reason">Der zu protokollierende Prüfzweck.</param>
public sealed record ModerationInspectInput(string Reason);
