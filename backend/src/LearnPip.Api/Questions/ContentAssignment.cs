// <copyright file="ContentAssignment.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Zuordnung eines Lerninhalts zu seinem Lernstand und Fälligkeitstermin.
/// </summary>
/// <param name="ContentId">Die Kennung des Lerninhalts.</param>
public sealed record ContentAssignment(Guid ContentId);
