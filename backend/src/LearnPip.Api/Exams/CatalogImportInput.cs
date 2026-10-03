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
public sealed record CatalogImportInput(string Code, string Title, string Revision,
    string SourceUrl, string License, string Attribution, DateOnly ChangedOn,
    bool RightsConfirmed, IReadOnlyList<CatalogQuestion> Questions);
