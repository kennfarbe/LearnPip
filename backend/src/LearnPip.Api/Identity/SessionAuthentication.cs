// <copyright file="SessionAuthentication.cs" company="LearnPip contributors">
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
/// Erstellt Sitzungsgeheimnisse und verwaltet das Sitzungscookie.
/// </summary>
public static class SessionAuthentication
{
    /// <summary>
    /// Den Namen des Authentifizierungsschemas.
    /// </summary>
    public const string Scheme = "LearnPipSession";

    /// <summary>
    /// Den Namen des Sitzungscookies.
    /// </summary>
    public const string CookieName = "learnpip_session";

    /// <summary>
    /// Den Claimnamen der Sitzungskennung.
    /// </summary>
    public const string SessionIdClaim = "learnpip_session_id";

    /// <summary>
    /// Erzeugt ein kryptografisch zufälliges Geheimnis.
    /// </summary>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static string NewSecret() =>
        Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// Berechnet einen SHA-256-Hash des Sitzungs- oder Wiederherstellungsgeheimnisses.
    /// </summary>
    /// <param name="secret">Das unverarbeitete Geheimnis.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static string Hash(string secret) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    /// <summary>
    /// Liest die Sitzungskennung aus einer authentifizierten Benutzeridentität.
    /// </summary>
    /// <param name="principal">Die authentifizierte Benutzeridentität.</param>
    /// <param name="sessionId">Die Kennung der Sitzung.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static bool TryGetSessionId(ClaimsPrincipal principal, out Guid sessionId) =>
        Guid.TryParse(principal.FindFirstValue(SessionIdClaim), out sessionId);

    /// <summary>
    /// Setzt das sichere Sitzungscookie mit dem angegebenen Ablaufzeitpunkt.
    /// </summary>
    /// <param name="context">Der Kontext der HTTP-Anfrage oder Autorisierungsprüfung.</param>
    /// <param name="token">Das unverarbeitete Sitzungstoken.</param>
    /// <param name="expiresAtUtc">Der Ablaufzeitpunkt in UTC.</param>
    public static void SetCookie(HttpContext context, string token, DateTimeOffset expiresAtUtc) =>
        context.Response.Cookies.Append(
        CookieName,
        token,
        new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = expiresAtUtc,
        });

    /// <summary>
    /// Entfernt das Sitzungscookie aus der HTTP-Antwort.
    /// </summary>
    /// <param name="context">Der Kontext der HTTP-Anfrage oder Autorisierungsprüfung.</param>
    public static void ClearCookie(HttpContext context) =>
        context.Response.Cookies.Delete(
        CookieName,
        new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
        });
}
