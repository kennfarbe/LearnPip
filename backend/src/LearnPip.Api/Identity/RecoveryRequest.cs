// <copyright file="RecoveryRequest.cs" company="LearnPip contributors">
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
/// Anfrage zur Kontowiederherstellung mit einem Wiederherstellungsgeheimnis.
/// </summary>
/// <param name="Secret">Das Wiederherstellungsgeheimnis.</param>
public sealed record RecoveryRequest(string? Secret);
