// <copyright file="GroupInput.cs" company="LearnPip contributors">
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
/// Anfrage zum Anlegen oder Umbenennen einer Lerngruppe.
/// </summary>
/// <param name="Name">Der Anzeigename.</param>
public sealed record GroupInput(string Name);
