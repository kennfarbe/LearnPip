// <copyright file="ApiPolicies.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Identity;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Security;

public static class ApiPolicies
{
    public const string ActiveAccount = nameof(ActiveAccount);
    public const string QuestionRead = nameof(QuestionRead);
    public const string MediaRead = nameof(MediaRead);
    public const string GroupRead = nameof(GroupRead);
    public const string Moderation = nameof(Moderation);
    public const string Admin = nameof(Admin);
}
