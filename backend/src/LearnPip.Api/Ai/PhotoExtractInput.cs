// <copyright file="PhotoExtractInput.cs" company="LearnPip contributors">
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
/// Anfrage zur Bildanalyse nach bestätigtem Datenschutzhinweis.
/// </summary>
/// <param name="MediaId">Die Kennung des zugehörigen Mediums, sofern vorhanden.</param>
/// <param name="Mode">Der ausgewählte KI-Betriebsmodus.</param>
/// <param name="DisclosureVersion">Die Version des zu bestätigenden Datenschutzhinweises.</param>
/// <param name="Confirmed">Gibt an, ob der Datenschutzhinweis bestätigt wurde.</param>
/// <param name="ReferenceSolutionHint">Der Hinweis zur Referenzlösung, sofern vorhanden.</param>
public sealed record PhotoExtractInput(
        Guid MediaId,
        string Mode,
        string DisclosureVersion,
        bool Confirmed,
        string? ReferenceSolutionHint);
