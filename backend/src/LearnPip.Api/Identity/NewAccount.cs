// <copyright file="NewAccount.cs" company="LearnPip contributors">
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
/// Neu angelegtes Konto mit Wiederherstellungsgeheimnis und Sitzung.
/// </summary>
/// <param name="AccountId">Die Kontokennung.</param>
/// <param name="RecoverySecret">Das Wiederherstellungsgeheimnis des Kontos.</param>
/// <param name="Session">Die ausgestellte Kontositzung.</param>
public sealed record NewAccount(Guid AccountId, string RecoverySecret, SessionGrant Session);
