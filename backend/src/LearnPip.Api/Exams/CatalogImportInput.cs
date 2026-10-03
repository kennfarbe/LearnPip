// <copyright file="CatalogImportInput.cs" company="LearnPip contributors">
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
/// Importdaten eines versionierten Prüfungskatalogs.
/// </summary>
/// <param name="Code">Der fachliche Bezeichner oder Bestätigungscode.</param>
/// <param name="Title">Der Titel.</param>
/// <param name="Revision">Die Revision der Daten.</param>
/// <param name="SourceUrl">Die Adresse der Inhaltsquelle.</param>
/// <param name="License">Die Inhaltslizenz.</param>
/// <param name="Attribution">Die Quellen- oder Urheberangabe.</param>
/// <param name="ChangedOn">Das Datum der letzten Änderung.</param>
/// <param name="RightsConfirmed">Gibt an, ob die Inhaltsrechte bestätigt wurden.</param>
/// <param name="Questions">Die ausgewählten Prüfungsfragen.</param>
public sealed record CatalogImportInput(
        string Code,
        string Title,
        string Revision,
        string SourceUrl,
        string License,
        string Attribution,
        DateOnly ChangedOn,
        bool RightsConfirmed,
        IReadOnlyList<CatalogQuestion> Questions);
