// <copyright file="StartLearningRequest.cs" company="LearnPip contributors">
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
/// Anfrage zum Start einer Lernsitzung.
/// </summary>
/// <param name="CatalogId">Die Kennung des Katalogs, sofern zugeordnet.</param>
/// <param name="Count">Die gewünschte Anzahl von Lernfragen.</param>
/// <param name="Language">Der Sprachcode.</param>
public sealed record StartLearningRequest(Guid? CatalogId, int Count = 5, string Language = "de");
