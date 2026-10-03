// <copyright file="LearningQuestion.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using LearnPip.Api.Ai;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;
/// <summary>
/// Frage innerhalb einer Lernsitzung mit Antwortoptionen.
/// </summary>
/// <param name="QuestionId">Die Kennung der Frage.</param>
/// <param name="VersionId">Die Kennung der Fragenfassung.</param>
/// <param name="SelectionMode">Der Modus für Einfach- oder Mehrfachauswahl.</param>
/// <param name="Prompt">Die Text- oder Inhaltsblöcke der Frage.</param>
/// <param name="Answers">Die Anzahl oder Zuordnung der Antworten.</param>
/// <param name="Hint">Der Lernhinweis, sofern vorhanden.</param>
/// <param name="NextStep">Der vorgeschlagene nächste Lernschritt, sofern vorhanden.</param>
/// <param name="Language">Der Sprachcode.</param>
/// <param name="RequestedLanguage">Der angeforderte Sprachcode.</param>
/// <param name="TranslationMissing">Gibt an, ob eine freigegebene Übersetzung fehlt.</param>
/// <param name="TranslationId">Die Kennung der Übersetzung, sofern vorhanden.</param>
/// <param name="VersionNumber">Die Nummer der Fragenfassung.</param>
public sealed record LearningQuestion(
        Guid QuestionId,
        Guid VersionId,
        string SelectionMode,
        IReadOnlyList<ContentBlockOutput> Prompt,
        IReadOnlyList<LearningOption> Answers,
        string? Hint,
        string? NextStep,
        string Language,
        string RequestedLanguage,
        bool TranslationMissing,
        Guid? TranslationId,
        int VersionNumber);
