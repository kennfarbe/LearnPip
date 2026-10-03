// <copyright file="SystemRoleRequirement.cs" company="LearnPip contributors">
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
/// Autorisierungsanforderung für eine bestimmte Systemrolle.
/// </summary>
/// <param name="Code">Der fachliche Bezeichner oder Bestätigungscode.</param>
public sealed record SystemRoleRequirement(string Code) : IAuthorizationRequirement;
