// <copyright file="ApiAuthorization.cs" company="LearnPip contributors">
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
/// Registriert die Autorisierungsrichtlinien und deren Handler.
/// </summary>
public static class ApiAuthorization
{
    /// <summary>
    /// Registriert die API-Richtlinien und den Ressourcenautorisierungshandler.
    /// </summary>
    /// <param name="services">Die Dienstregistrierung des Hosts.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                    ApiPolicies.ActiveAccount,
                    policy =>
                policy.RequireAuthenticatedUser().AddRequirements(new ActiveAccountRequirement()));
            options.AddPolicy(
                    ApiPolicies.FreshIdentity,
                    policy =>
                policy.RequireAuthenticatedUser().AddRequirements(new ActiveAccountRequirement()).AddRequirements(new FreshIdentitySessionRequirement()));
            options.AddPolicy(
                    ApiPolicies.QuestionRead,
                    policy =>
                policy.RequireAuthenticatedUser().AddRequirements(new QuestionReadRequirement()));
            options.AddPolicy(
                    ApiPolicies.MediaRead,
                    policy =>
                policy.RequireAuthenticatedUser().AddRequirements(new MediaReadRequirement()));
            options.AddPolicy(
                    ApiPolicies.GroupRead,
                    policy =>
                policy.RequireAuthenticatedUser().AddRequirements(new GroupReadRequirement()));
            options.AddPolicy(
                    ApiPolicies.Moderation,
                    policy =>
                policy.RequireAuthenticatedUser().AddRequirements(new SystemRoleRequirement("moderator")));
            options.AddPolicy(
                    ApiPolicies.Admin,
                    policy =>
                policy.RequireAuthenticatedUser().AddRequirements(new SystemRoleRequirement("admin")).AddRequirements(new FreshAdminSessionRequirement()));
        });
        services.AddScoped<IAuthorizationHandler, ResourceAuthorizationHandler>();
        return services;
    }
}
