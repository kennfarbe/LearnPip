// <copyright file="PublicPreview.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Cryptography;
using System.Text.Json;
using LearnPip.Api.Identity;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Vorschau einer zur Veröffentlichung vorgesehenen Fragenfassung.
/// </summary>
/// <param name="Version">Die Versionsnummer.</param>
/// <param name="PreviewToken">Das Bestätigungstoken der Veröffentlichungsvorschau.</param>
/// <param name="HasImages">Gibt an, ob die Frage Bilder enthält.</param>
public sealed record PublicPreview(
        PublishedQuestionVersion Version,
        string PreviewToken,
        bool HasImages)
{
    /// <summary>Holt die tatsächlich bestätigten Frage- und Mediennachweise.</summary>
    public JsonElement? Rights { get; init; }

    /// <summary>Holt die offenen technischen Nachweislücken.</summary>
    public IReadOnlyList<string> RightsReport { get; init; } = [];

    /// <summary>Holt einen Wert, der angibt, ob der Betreiber offene Weitergabe erlaubt.</summary>
    public bool CommunityEnabled { get; init; }

    /// <summary>Holt einen Wert, der angibt, ob aktuelle Einzelnachweise vorhanden sind.</summary>
    public bool CommunityEligible { get; init; }
}
