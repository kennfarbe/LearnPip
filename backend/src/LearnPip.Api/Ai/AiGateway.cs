// <copyright file="AiGateway.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LearnPip.Api.Ai;

public sealed class AiGateway : IDisposable
{
    private readonly HttpClient client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        UseProxy = false
    })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public IAiProvider Resolve(string mode, IConfiguration config, string? userKey) =>
        mode == "off" ? new DisabledAiProvider() : new ChatCompletionProvider(
            new Uri(config[mode == "operator-local" ? "Ai:LocalEndpoint" : "Ai:CloudEndpoint"]!),
            config[mode == "operator-local" ? "Ai:LocalModel" : "Ai:CloudModel"]!, mode switch
            {
                "operator-cloud" => config["Ai:CloudKey"],
                "user-key" => userKey,
                _ => null
            }, this.client);

    public void Dispose() => this.client.Dispose();
}
