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

public static class AccountIdentity
{
    // This claim must come from a validated issuer. An OIDC subject is not an Account.Id.
    public const string AccountIdClaim = "learnpip_account_id";

    public static bool TryGetAccountId(ClaimsPrincipal principal, out Guid accountId) =>
        Guid.TryParse(principal.FindFirstValue(AccountIdClaim), out accountId);
}
