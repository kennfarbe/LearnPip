// <copyright file="CatalogExportRequest.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Text.Json;

namespace LearnPip.Api.CatalogPackages;

/// <summary>Explizite Auswahl und Rechteangaben für einen lokalen Download.</summary>
/// <param name="QuestionIds">Die eigenen ausgewählten Fragen, maximal 10000.</param>
/// <param name="Title">Der frei gewählte Pakettitel.</param>
/// <param name="Publisher">Der ausdrücklich angegebene Attributionsname, kein Kontoname.</param>
/// <param name="QuestionLicense">Die ausdrücklich gewählte Lizenz eigener Originaltexte.</param>
/// <param name="ImageLicense">Die separat gewählte Lizenz eigener Originalbilder.</param>
/// <param name="LicenseNotice">Vom Benutzer bereitgestellte Lizenztexte oder Nutzungsbedingungen.</param>
/// <param name="RightsConfirmed">Bestätigt eigene Originalinhalte und Exportrechte.</param>
/// <param name="PreviewSha256">Bindet den Download an die geprüfte Vorschau.</param>
public sealed record CatalogExportRequest(
    IReadOnlyList<Guid> QuestionIds,
    string Title,
    string Publisher,
    JsonElement QuestionLicense,
    JsonElement ImageLicense,
    string LicenseNotice,
    bool RightsConfirmed,
    string? PreviewSha256)
{
    /// <summary>Holt den ausdrücklich gewählten Zweck; Standard ist privat.</summary>
    public string Purpose { get; init; } = "private";

    /// <summary>Holt einen Wert, der angibt, ob die offene Weitergabe mit ihren Folgen bestätigt ist.</summary>
    public bool PublicationConfirmed { get; init; }
}
