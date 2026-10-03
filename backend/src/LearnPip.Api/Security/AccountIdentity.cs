// <copyright file="AccountIdentity.cs" company="LearnPip contributors">
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
/// Liest die Kontokennung aus dem angemeldeten Benutzer.
/// </summary>
public static class AccountIdentity
{
    // This claim must come from a validated issuer. An OIDC subject is not an Account.Id.

    /// <summary>
    /// Den Claimnamen der Kontokennung.
    /// </summary>
    public const string AccountIdClaim = "learnpip_account_id";

    /// <summary>
    /// Liest die Kontokennung aus einer authentifizierten Benutzeridentität.
    /// </summary>
    /// <param name="principal">Die authentifizierte Benutzeridentität.</param>
    /// <param name="accountId">Die Kennung des betroffenen Kontos.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static bool TryGetAccountId(ClaimsPrincipal principal, out Guid accountId) =>
        Guid.TryParse(principal.FindFirstValue(AccountIdClaim), out accountId);
}
