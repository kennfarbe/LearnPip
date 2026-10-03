// <copyright file="MaintenanceNoticeRequest.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Administration;

/// <summary>
/// Anfrage zum Ändern des Wartungshinweises.
/// </summary>
/// <param name="Value">Der neue Wert der Einstellung.</param>
public sealed record MaintenanceNoticeRequest(string? Value);
