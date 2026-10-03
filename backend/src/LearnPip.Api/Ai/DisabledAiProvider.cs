// <copyright file="DisabledAiProvider.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LearnPip.Api.Ai;

public sealed class DisabledAiProvider : IAiProvider
{
    public Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("AI is disabled.");
    public Task<string> TranslateAsync(string prompt, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("AI is disabled.");
    public Task<string> AnalyzeImageAsync(string instruction, byte[] image, string mediaType,
        CancellationToken cancellationToken) => throw new InvalidOperationException("AI is disabled.");
}
