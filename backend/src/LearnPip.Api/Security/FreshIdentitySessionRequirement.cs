// <copyright file="FreshIdentitySessionRequirement.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using Microsoft.AspNetCore.Authorization;

namespace LearnPip.Api.Security;

/// <summary>Verlangt eine höchstens 15 Minuten alte Sitzung für Identitätsänderungen.</summary>
public sealed record FreshIdentitySessionRequirement : IAuthorizationRequirement;
