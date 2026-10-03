// <copyright file="InvitationView.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Groups;

/// <summary>
/// Gruppeneinladung mit Code und Ablaufzeitpunkt.
/// </summary>
/// <param name="Id">Die eindeutige Kennung.</param>
/// <param name="Code">Der fachliche Bezeichner oder Bestätigungscode.</param>
/// <param name="ExpiresAtUtc">Der Ablaufzeitpunkt in UTC.</param>
/// <param name="MaxUses">Die höchstens erlaubte Anzahl von Einlösungen.</param>
public sealed record InvitationView(Guid Id, string Code, DateTimeOffset ExpiresAtUtc, int MaxUses);
