// <copyright file="TopicProgress.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Lernfortschritt eines Fachs und Themas.
/// </summary>
/// <param name="Subject">Das Fach oder Themengebiet.</param>
/// <param name="Topic">Das Thema innerhalb des Fachs.</param>
/// <param name="TotalContents">Die Anzahl der berücksichtigten Lerninhalte.</param>
/// <param name="MasteredContents">Die Anzahl sicher beherrschter Inhalte.</param>
/// <param name="ImprovedContents">Die Anzahl verbesserter Lerninhalte.</param>
public sealed record TopicProgress(
        string Subject,
        string Topic,
        int TotalContents,
        int MasteredContents,
        int ImprovedContents);
