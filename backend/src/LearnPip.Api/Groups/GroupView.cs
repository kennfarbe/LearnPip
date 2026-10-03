// <copyright file="GroupView.cs" company="LearnPip contributors">
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
/// Übersicht einer Lerngruppe mit Besitzerkennung.
/// </summary>
/// <param name="Id">Die eindeutige Kennung.</param>
/// <param name="Name">Der Anzeigename.</param>
/// <param name="OwnerAccountId">Die Kennung des Gruppenbesitzers.</param>
public sealed record GroupView(Guid Id, string Name, Guid OwnerAccountId);
