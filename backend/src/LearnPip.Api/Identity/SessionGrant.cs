// <copyright file="SessionGrant.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LearnPip.Api.Identity;

/// <summary>
/// Ausgestelltes Sitzungsgeheimnis mit Ablaufzeitpunkt.
/// </summary>
/// <param name="Token">Das Sitzungs- oder Einladungstoken.</param>
/// <param name="ExpiresAtUtc">Der Ablaufzeitpunkt in UTC.</param>
public sealed record SessionGrant(string Token, DateTimeOffset ExpiresAtUtc);
