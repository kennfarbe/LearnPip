// <copyright file="AiGateway.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LearnPip.Api.Ai;

/// <summary>
/// Wählt einen KI-Anbieter entsprechend dem freigegebenen Betriebsmodus aus.
/// </summary>
public sealed class AiGateway : IDisposable
{
    private readonly HttpClient client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        UseProxy = false,
    })
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    /// <summary>
    /// Wählt den konfigurierten KI-Anbieter für einen Betriebsmodus aus.
    /// </summary>
    /// <param name="mode">Der ausgewählte KI-Betriebsmodus.</param>
    /// <param name="config">Die Anwendungskonfiguration.</param>
    /// <param name="userKey">Der entschlüsselte persönliche KI-Schlüssel, sofern vorhanden.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public IAiProvider Resolve(
        string mode,
        IConfiguration config,
        string? userKey)
    {
        if (mode == "off")
        {
            return new DisabledAiProvider();
        }

        var key = mode switch
        {
            "operator-cloud" => config["Ai:CloudKey"],
            "user-key" => userKey,
            _ => null,
        };
        return new ChatCompletionProvider(
            new Uri(config[mode == "operator-local" ? "Ai:LocalEndpoint" : "Ai:CloudEndpoint"]!),
            config[mode == "operator-local" ? "Ai:LocalModel" : "Ai:CloudModel"]!,
            key,
            this.client);
    }

    /// <summary>
    /// Gibt den vom Gateway verwalteten HTTP-Client frei.
    /// </summary>
    public void Dispose() => this.client.Dispose();
}
