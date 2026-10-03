// <copyright file="InvitationInput.cs" company="LearnPip contributors">
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
/// Gültigkeitsdauer und Nutzungsgrenze einer Gruppeneinladung.
/// </summary>
/// <param name="ExpiresAtUtc">Der Ablaufzeitpunkt in UTC.</param>
/// <param name="MaxUses">Die höchstens erlaubte Anzahl von Einlösungen.</param>
public sealed record InvitationInput(DateTimeOffset ExpiresAtUtc, int MaxUses);
