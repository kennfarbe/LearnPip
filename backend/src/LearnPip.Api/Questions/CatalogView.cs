// <copyright file="CatalogView.cs" company="LearnPip contributors">
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
/// Übersicht eines privaten Fragenkatalogs.
/// </summary>
/// <param name="Id">Die eindeutige Kennung.</param>
/// <param name="Name">Der Anzeigename.</param>
/// <param name="QuestionCount">Die Anzahl der Fragen.</param>
public sealed record CatalogView(Guid Id, string Name, int QuestionCount);
