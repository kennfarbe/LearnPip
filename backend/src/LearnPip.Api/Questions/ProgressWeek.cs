// <copyright file="ProgressWeek.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;
/// <summary>
/// Teilnahmeübersicht einer Kalenderwoche.
/// </summary>
/// <param name="Label">Die Beschriftung des Zeitabschnitts.</param>
/// <param name="CompletedSessions">Die Anzahl abgeschlossener Lernsitzungen.</param>
/// <param name="ActiveDays">Die Anzahl aktiver Lerntage.</param>
public sealed record ProgressWeek(string Label, int CompletedSessions, int ActiveDays);
