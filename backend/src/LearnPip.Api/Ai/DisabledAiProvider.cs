// <copyright file="DisabledAiProvider.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LearnPip.Api.Ai;

/// <summary>
/// Verweigert KI-Operationen im deaktivierten Betriebsmodus.
/// </summary>
public sealed class DisabledAiProvider : IAiProvider
{
    /// <summary>
    /// Erstellt eine Textantwort mit dem ausgewählten KI-Anbieter.
    /// </summary>
    /// <param name="prompt">Die Textanweisung an den KI-Anbieter.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("AI is disabled.");

    /// <summary>
    /// Erstellt eine Übersetzung mit dem ausgewählten KI-Anbieter.
    /// </summary>
    /// <param name="prompt">Die Textanweisung an den KI-Anbieter.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public Task<string> TranslateAsync(string prompt, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("AI is disabled.");

    /// <summary>
    /// Analysiert ein Bild anhand der vorgegebenen Anweisung.
    /// </summary>
    /// <param name="instruction">Die Anweisung zur Bildanalyse.</param>
    /// <param name="image">Die zu analysierenden Bilddaten.</param>
    /// <param name="mediaType">Der MIME-Typ der Bilddaten.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public Task<string> AnalyzeImageAsync(
        string instruction,
        byte[] image,
        string mediaType,
        CancellationToken cancellationToken) => throw new InvalidOperationException("AI is disabled.");
}
