// <copyright file="PowerSnapshot.cs" company="LearnPip contributors">
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
/// Eingefrorene Fragen- und Profilregeln eines intensiven Prüfungstests.
/// </summary>
/// <param name="QuestionMode">Der Modus für Originalfragen oder Varianten.</param>
/// <param name="StageSize">Die Fragenanzahl pro Stufe.</param>
/// <param name="Parts">Die Prüfungsteile.</param>
public sealed record PowerSnapshot(
        string QuestionMode,
        int StageSize,
        IReadOnlyList<SnapshotPart> Parts);
