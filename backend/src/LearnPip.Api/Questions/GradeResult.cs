// <copyright file="GradeResult.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.RegularExpressions;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;
/// <summary>
/// Ergebnis eines Antwortversuchs mit Korrektheitsmarkierung.
/// </summary>
/// <param name="AttemptId">Die Kennung des Antwortversuchs.</param>
/// <param name="VersionId">Die Kennung der Fragenfassung.</param>
/// <param name="IsCorrect">Gibt an, ob die Antwort richtig ist.</param>
/// <param name="SelectedOptionIds">Die vom Benutzer ausgewählten Antwortkennungen.</param>
/// <param name="CorrectOptionIds">Die Kennungen der richtigen Antwortoptionen.</param>
public sealed record GradeResult(
        Guid AttemptId,
        Guid VersionId,
        bool IsCorrect,
        IReadOnlyList<Guid> SelectedOptionIds,
        IReadOnlyList<Guid> CorrectOptionIds);
