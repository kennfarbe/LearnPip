// <copyright file="ExamPlanInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Questions;

/// <summary>
/// Vorgaben des Prüfungsdreiecks für Umfang, Termin und Erfolgsquote.
/// </summary>
/// <param name="ScopeContents">Die Anzahl der im Lernziel berücksichtigten Inhalte.</param>
/// <param name="DailyMinutes">Die tägliche Lernzeit in Minuten.</param>
/// <param name="TargetPercent">Die angestrebte Erfolgsquote in Prozent.</param>
/// <param name="ExamDate">Das geplante Prüfungsdatum.</param>
/// <param name="HorizonDays">Der Planungshorizont in Tagen.</param>
/// <param name="DailyLimitMinutes">Die maximale tägliche Lernzeit in Minuten.</param>
/// <param name="SchoolDays">Die vorgesehenen Schultage.</param>
/// <param name="BreakDays">Die Anzahl der Lerntage ohne Teilnahme.</param>
/// <param name="ContentIds">Die Kennungen der berücksichtigten Lerninhalte.</param>
public sealed record ExamPlanInput(
        int? ScopeContents,
        int? DailyMinutes,
        int? TargetPercent,
        DateOnly? ExamDate,
        int? HorizonDays,
        int DailyLimitMinutes,
        IReadOnlyList<int>? SchoolDays,
        IReadOnlyList<DateOnly>? BreakDays,
        IReadOnlyList<Guid>? ContentIds);
