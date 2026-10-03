// <copyright file="LearningPlanItem.cs" company="LearnPip contributors">
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
/// Ausgewählter Lerninhalt mit Reihenfolge innerhalb einer Lernsitzung.
/// </summary>
/// <param name="QuestionId">Die Kennung der Frage.</param>
/// <param name="VersionId">Die Kennung der Fragenfassung.</param>
/// <param name="OptionIds">Die Kennungen der Antwortoptionen.</param>
/// <param name="State">Der Zustand des Updateauftrags.</param>
/// <param name="Language">Der Sprachcode.</param>
/// <param name="TranslationId">Die Kennung der Übersetzung, sofern vorhanden.</param>
internal sealed record LearningPlanItem(
        Guid QuestionId,
        Guid VersionId,
        Guid[] OptionIds,
        string State,
        string Language = "de",
        Guid? TranslationId = null);
