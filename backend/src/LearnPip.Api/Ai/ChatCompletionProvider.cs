// <copyright file="ChatCompletionProvider.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LearnPip.Api.Ai;

public sealed class ChatCompletionProvider(Uri endpoint, string model, string? key,
    HttpClient client) : IAiProvider
{
    public Task<string> TranslateAsync(string prompt, CancellationToken cancellationToken) =>
        this.CompleteAsync(new { role = "user", content = (object)prompt }, 2048, cancellationToken);
    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
        => await this.CompleteAsync(new { role = "user", content = (object)prompt }, 512,
            cancellationToken);

    public Task<string> AnalyzeImageAsync(string instruction, byte[] image, string mediaType,
        CancellationToken cancellationToken)
    {
        object[] content =
        [
            new { type = "text", text = instruction },
            new { type = "image_url", image_url = new
                { url = $"data:{mediaType};base64,{Convert.ToBase64String(image)}" } }
        ];
        return this.CompleteAsync(new { role = "user", content = (object)content }, 2048,
            cancellationToken);
    }

    private async Task<string> CompleteAsync(object message, int outputTokens,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        if (key != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model,
            messages = new[] { message },
            max_tokens = outputTokens
        }), Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("AI provider failed.");
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[4096];
        int count;
        while ((count = await stream.ReadAsync(chunk, cancellationToken)) != 0)
        {
            if (buffer.Length + count > 65536) throw new InvalidDataException("AI response too large.");
            buffer.Write(chunk, 0, count);
        }
        using var json = JsonDocument.Parse(buffer.ToArray());
        var text = json.RootElement.GetProperty("choices")[0].GetProperty("message")
            .GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(text)) throw new InvalidDataException("Empty AI response.");
        return text.Length > 16000 ? text[..16000] : text;
    }
}
