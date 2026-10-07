// <copyright file="QuestionRevisionInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Questions;

/// <summary>Neue Fassung aus einer ausdrücklich geprüften Vorlage.</summary>
/// <param name="Content">Neue Frage mit Antworten und Erklärung.</param>
/// <param name="Reason">Prüf- und Bearbeitungsgrund.</param>
/// <param name="ExpectedVersion">Geprüfte letzte Versionsnummer.</param>
public sealed record QuestionRevisionInput(QuestionPublishRequest Content, string Reason, int ExpectedVersion);
