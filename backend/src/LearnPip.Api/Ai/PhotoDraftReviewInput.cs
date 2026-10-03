// <copyright file="PhotoDraftReviewInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LearnPip.Api.Media;
using LearnPip.Api.Questions;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Ai;

/// <summary>
/// Bestätigter Fotoentwurf mit Inhalts- und Rechteprüfung.
/// </summary>
/// <param name="MediaId">Die Kennung des zugehörigen Mediums, sofern vorhanden.</param>
/// <param name="Recognition">Das strukturierte Bilderkennungsergebnis.</param>
/// <param name="CorrectIndex">Der Index der richtigen Antwort.</param>
/// <param name="Confirmed">Gibt an, ob der Datenschutzhinweis bestätigt wurde.</param>
public sealed record PhotoDraftReviewInput(
        Guid MediaId,
        PhotoRecognition Recognition,
        int CorrectIndex,
        bool Confirmed);
