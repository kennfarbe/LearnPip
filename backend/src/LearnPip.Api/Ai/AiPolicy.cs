// <copyright file="AiPolicy.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LearnPip.Api.Ai;

public static class AiPolicy
{
    public const string DisclosureVersion = "ai-photo-v2";
    public static readonly string[] Modes = ["off", "operator-cloud", "operator-local", "user-key"];

    public static AiModeInfo Describe(string mode, IConfiguration config, bool hasUserKey)
    {
        var allowed = (config["Ai:AllowedModes"] ?? "off").Split(',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Contains(mode, StringComparer.Ordinal);
        var local = mode == "operator-local";
        var endpoint = local ? config["Ai:LocalEndpoint"] : config["Ai:CloudEndpoint"];
        var uriValid = Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) &&
            uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
            (local ? LocalUri(uri) : uri.Scheme == Uri.UriSchemeHttps);
        var model = local ? config["Ai:LocalModel"] : config["Ai:CloudModel"];
        var keyReady = mode switch
        {
            "operator-cloud" => !string.IsNullOrWhiteSpace(config["Ai:CloudKey"]),
            "user-key" => hasUserKey && AiKeyVault.Available(config),
            _ => true
        };
        var quota = int.TryParse(config[$"Ai:Quota:{mode}"], out var configured) ? configured :
            mode == "operator-cloud" ? 10 : mode == "operator-local" ? 50 : 20;
        quota = Math.Clamp(quota, 0, 1000);
        var max = int.TryParse(config["Ai:MaxInputBytes"], out var configuredMax) ?
            configuredMax : 8192;
        max = Math.Clamp(max, 1, 32768);
        var imageMax = int.TryParse(config["Ai:MaxImageBytes"], out var configuredImageMax) ?
            configuredImageMax : 2 * 1024 * 1024;
        imageMax = Math.Clamp(imageMax, 1, 5 * 1024 * 1024);
        return mode == "off" ? new AiModeInfo(mode, true, "Kein Anbieter",
            "Keine Übermittlung. Manuelles Lernen bleibt verfügbar.", 0, max, imageMax, false) :
            new AiModeInfo(mode, allowed && uriValid && !string.IsNullOrWhiteSpace(model) &&
                keyReady && quota > 0,
                uriValid ? uri!.GetLeftPart(UriPartial.Authority) : "Nicht konfiguriert",
                local ? "Ausdrücklich gewählter Text oder Foto an den lokalen Dienst des Betreibers." :
                    "Ausdrücklich gewählter Text oder Foto an den Cloud-Anbieter; " +
                    (mode == "user-key" ? "der eigene API-Schlüssel wird mitgesendet." :
                        "der Betreiber trägt den API-Schlüssel."),
                quota, max, imageMax, mode == "user-key");
    }

    private static bool LocalUri(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return false;
        if (uri.Host is "localhost" or "127.0.0.1" or "::1") return true;
        if (!uri.Host.Contains('.') && uri.Host.All(character =>
                char.IsAsciiLetterOrDigit(character) || character == '-')) return true;
        if (!IPAddress.TryParse(uri.Host, out var address)) return false;
        var bytes = address.GetAddressBytes();
        return bytes.Length == 4 && (bytes[0] == 10 || bytes[0] == 192 && bytes[1] == 168 ||
            bytes[0] == 172 && bytes[1] is >= 16 and <= 31);
    }
}
