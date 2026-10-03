// <copyright file="FamilyGoalInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using LearnPip.Api.Identity;
using LearnPip.Api.Questions;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Family;

/// <summary>
/// Anfrage zum Anlegen eines gemeinsamen Lernziels.
/// </summary>
/// <param name="Title">Der Titel.</param>
/// <param name="TargetAtUtc">Der Zielzeitpunkt in UTC, sofern vorhanden.</param>
public sealed record FamilyGoalInput(string Title, DateTimeOffset? TargetAtUtc);
