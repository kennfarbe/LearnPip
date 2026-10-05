// <copyright file="CatalogPackage.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Text.Json;

namespace LearnPip.Api.CatalogPackages;

/// <summary>Enthält ein vollständig geprüftes, unverändertes Austauschpaket.</summary>
/// <param name="Manifest">Die ursprünglichen Manifestangaben.</param>
/// <param name="Questions">Die ursprünglichen Fragen mit Einzellizenzen.</param>
/// <param name="Files">Die geprüften Originaldateien.</param>
/// <param name="Archive">Die Originalbytes für verlustfreie Weitergabe.</param>
/// <param name="Fingerprint">Der von JSON-Formatierung unabhängige Inhaltsvergleich.</param>
public sealed record CatalogPackage(
    JsonElement Manifest,
    IReadOnlyList<JsonElement> Questions,
    IReadOnlyDictionary<string,
    byte[]> Files,
    byte[] Archive,
    string Fingerprint);
