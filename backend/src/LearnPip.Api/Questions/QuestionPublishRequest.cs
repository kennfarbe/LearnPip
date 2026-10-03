// <copyright file="QuestionPublishRequest.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.RegularExpressions;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Inhalt und Veröffentlichungsangaben einer neuen Fragenfassung.
/// </summary>
/// <param name="SelectionMode">Der Modus für Einfach- oder Mehrfachauswahl.</param>
/// <param name="Subject">Das Fach oder Themengebiet.</param>
/// <param name="Topic">Das Thema innerhalb des Fachs.</param>
/// <param name="Language">Der Sprachcode.</param>
/// <param name="Source">Die Herkunft der Übersetzung.</param>
/// <param name="License">Die Inhaltslizenz.</param>
/// <param name="Prompt">Die Text- oder Inhaltsblöcke der Frage.</param>
/// <param name="Explanation">Die Erklärung zur Lösung.</param>
/// <param name="Answers">Die Anzahl oder Zuordnung der Antworten.</param>
public sealed record QuestionPublishRequest(
        string SelectionMode,
        string Subject,
        string Topic,
        string Language,
        string Source,
        string License,
        IReadOnlyList<ContentBlockInput> Prompt,
        IReadOnlyList<ContentBlockInput> Explanation,
        IReadOnlyList<AnswerInput> Answers);
