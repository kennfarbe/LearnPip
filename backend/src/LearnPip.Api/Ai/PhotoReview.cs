// <copyright file="PhotoReview.cs" company="LearnPip contributors">
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
/// Erkanntes Fotoergebnis mit geprüftem Lösungsvergleich.
/// </summary>
/// <param name="Recognition">Das strukturierte Bilderkennungsergebnis.</param>
/// <param name="Comparison">Das Ergebnis des Lösungsvergleichs.</param>
/// <param name="ComparisonExplanation">Die Begründung des Lösungsvergleichs.</param>
/// <param name="MediaId">Die Kennung des zugehörigen Mediums, sofern vorhanden.</param>
/// <param name="Mode">Der ausgewählte KI-Betriebsmodus.</param>
/// <param name="Verification">Der Verifikationsstatus der Verknüpfung.</param>
public sealed record PhotoReview(
        PhotoRecognition Recognition,
        string Comparison,
        string ComparisonExplanation,
        Guid MediaId,
        string Mode,
        SolutionCheck Verification);
