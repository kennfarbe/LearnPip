// <copyright file="DraftSaveRequest.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Anfrage zum Speichern eines Fragenentwurfs.
/// </summary>
/// <param name="Content">Der Inhalt der Fragenfassung.</param>
/// <param name="CatalogId">Die Kennung des Katalogs, sofern zugeordnet.</param>
/// <param name="CatalogIds">Alle Katalogzuordnungen; null verwendet den bisherigen Einzelkatalog.</param>
public sealed record DraftSaveRequest(QuestionPublishRequest Content, Guid? CatalogId, Guid[]? CatalogIds = null);
