// <copyright file="IAiProvider.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LearnPip.Api.Ai;

public interface IAiProvider
{
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken);
    Task<string> TranslateAsync(string prompt, CancellationToken cancellationToken);
    Task<string> AnalyzeImageAsync(string instruction, byte[] image, string mediaType,
        CancellationToken cancellationToken);
}
