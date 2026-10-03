// <copyright file="AccountInfo.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Identity;

/// <summary>
/// Kontodaten einschließlich Altersgruppe und Anmeldeinformationen.
/// </summary>
/// <param name="Id">Die eindeutige Kennung.</param>
/// <param name="DisplayName">Der Anzeigename des Kontos, sofern vorhanden.</param>
/// <param name="LastActivityAtUtc">Der Zeitpunkt der letzten Aktivität in UTC.</param>
/// <param name="DisabledAtUtc">Der Deaktivierungszeitpunkt in UTC, sofern vorhanden.</param>
public sealed record AccountInfo(
        Guid Id,
        string? DisplayName,
        DateTimeOffset LastActivityAtUtc,
        DateTimeOffset? DisabledAtUtc);
