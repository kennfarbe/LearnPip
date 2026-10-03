// <copyright file="CatalogMoveRequest.cs" company="LearnPip contributors">
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
/// Anfrage zum Verschieben einer Frage in einen Katalog.
/// </summary>
/// <param name="CatalogId">Die Kennung des Katalogs, sofern zugeordnet.</param>
public sealed record CatalogMoveRequest(Guid? CatalogId);
