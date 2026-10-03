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

/// <summary>
/// Namen der Autorisierungsrichtlinien für die API.
/// </summary>
public static class ApiPolicies
{
    /// <summary>
    /// Den Richtliniennamen für aktive Konten.
    /// </summary>
    public const string ActiveAccount = nameof(ActiveAccount);

    /// <summary>
    /// Den Richtliniennamen zum Lesen von Fragen.
    /// </summary>
    public const string QuestionRead = nameof(QuestionRead);

    /// <summary>
    /// Den Richtliniennamen zum Lesen privater Medien.
    /// </summary>
    public const string MediaRead = nameof(MediaRead);

    /// <summary>
    /// Den Richtliniennamen zum Lesen einer Lerngruppe.
    /// </summary>
    public const string GroupRead = nameof(GroupRead);

    /// <summary>
    /// Den Richtliniennamen für Moderationsrechte.
    /// </summary>
    public const string Moderation = nameof(Moderation);

    /// <summary>
    /// Den Richtliniennamen für Administratorrechte.
    /// </summary>
    public const string Admin = nameof(Admin);
}
