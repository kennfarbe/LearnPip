// <copyright file="UpdateIntervalRequest.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Administration;

/// <summary>
/// Anfrage zum Ändern des Update-Prüfintervalls.
/// </summary>
/// <param name="Interval">Das gewünschte Prüfintervall.</param>
public sealed record UpdateIntervalRequest(string Interval);
