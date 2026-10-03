// <copyright file="ProfileInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;

/// <summary>
/// Eingabedaten eines versionierten Prüfungsprofils.
/// </summary>
/// <param name="Code">Der fachliche Bezeichner oder Bestätigungscode.</param>
/// <param name="Title">Der Titel.</param>
/// <param name="AmateurClass">Die Amateurfunkklasse des Prüfungsprofils.</param>
/// <param name="CatalogEditionId">Die Kennung der Katalogausgabe.</param>
/// <param name="Parts">Die Prüfungsteile.</param>
/// <param name="Sessions">Die berücksichtigten Prüfungstermine.</param>
/// <param name="RulesSourceUrl">Die Adresse der maßgeblichen Prüfungsregeln.</param>
/// <param name="RulesCheckedOn">Das Datum der letzten Regelprüfung.</param>
public sealed record ProfileInput(
        string Code,
        string Title,
        string AmateurClass,
        Guid CatalogEditionId,
        IReadOnlyList<ProfilePart> Parts,
        IReadOnlyList<ExamSession>? Sessions = null,
        string? RulesSourceUrl = null,
        DateOnly? RulesCheckedOn = null);
