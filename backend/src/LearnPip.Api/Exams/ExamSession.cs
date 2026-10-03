// <copyright file="ExamSession.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;

/// <summary>
/// Prüfungstermin einschließlich Anmeldung und Quellenangabe.
/// </summary>
/// <param name="Date">Das Datum des Prüfungstermins.</param>
/// <param name="Place">Der Prüfungsort.</param>
/// <param name="RegistrationDeadline">Die Anmeldefrist, sofern bekannt.</param>
/// <param name="SourceUrl">Die Adresse der Inhaltsquelle.</param>
/// <param name="CheckedOn">Das Datum der letzten Prüfung.</param>
public sealed record ExamSession(
        DateOnly Date,
        string Place,
        DateOnly? RegistrationDeadline,
        string SourceUrl,
        DateOnly CheckedOn);
