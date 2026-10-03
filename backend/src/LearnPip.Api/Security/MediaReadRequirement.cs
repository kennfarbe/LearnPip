// <copyright file="MediaReadRequirement.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Identity;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Security;
/// <summary>
/// Autorisierungsanforderung zum Lesen eines privaten Mediums.
/// </summary>
public sealed record MediaReadRequirement : IAuthorizationRequirement;
