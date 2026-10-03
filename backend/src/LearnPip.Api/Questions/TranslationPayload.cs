// <copyright file="TranslationPayload.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LearnPip.Api.Ai;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Übersetzte Frage mit Antwort- und Erklärungsblöcken.
/// </summary>
/// <param name="Prompt">Die Text- oder Inhaltsblöcke der Frage.</param>
/// <param name="Explanation">Die Erklärung zur Lösung.</param>
/// <param name="Answers">Die Anzahl oder Zuordnung der Antworten.</param>
public sealed record TranslationPayload(
        IReadOnlyList<LocalizedBlock> Prompt,
        IReadOnlyList<LocalizedBlock> Explanation,
        IReadOnlyList<LocalizedAnswer> Answers);
