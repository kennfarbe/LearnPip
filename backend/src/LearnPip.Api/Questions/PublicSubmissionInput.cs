// <copyright file="PublicSubmissionInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Cryptography;
using LearnPip.Api.Identity;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Bestätigte Einreichung einer Fragenfassung mit Lizenz- und Rechteangaben.
/// </summary>
/// <param name="PreviewToken">Das Bestätigungstoken der Veröffentlichungsvorschau.</param>
/// <param name="LicenseChoice">Die ausgewählte Inhaltslizenz.</param>
/// <param name="AuthorAttribution">Die gewünschte Urheberangabe.</param>
/// <param name="RightsConfirmed">Gibt an, ob die Inhaltsrechte bestätigt wurden.</param>
/// <param name="ImageRightsConfirmed">Gibt an, ob die Bildrechte bestätigt wurden.</param>
/// <param name="AgeDeclaration">Die Altersangabe des Kontos.</param>
public sealed record PublicSubmissionInput(
        string PreviewToken,
        string LicenseChoice,
        string AuthorAttribution,
        bool RightsConfirmed,
        bool ImageRightsConfirmed,
        string AgeDeclaration);
