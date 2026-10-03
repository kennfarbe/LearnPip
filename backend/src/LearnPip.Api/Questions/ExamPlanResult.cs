// <copyright file="ExamPlanResult.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Questions;

/// <summary>
/// Berechnetes Lernziel mit variabler Prüfungsdreieck-Größe und Machbarkeit.
/// </summary>
/// <param name="EstimatedDimension">Die im Prüfungsdreieck geschätzte variable Größe.</param>
/// <param name="ScopeContents">Die Anzahl der im Lernziel berücksichtigten Inhalte.</param>
/// <param name="DailyMinutes">Die tägliche Lernzeit in Minuten.</param>
/// <param name="TargetPercent">Die angestrebte Erfolgsquote in Prozent.</param>
/// <param name="AvailableContents">Die Anzahl verfügbarer Lerninhalte.</param>
/// <param name="MasteredContents">Die Anzahl sicher beherrschter Inhalte.</param>
/// <param name="PlanningDays">Die verbleibenden Tage bis zur Prüfung.</param>
/// <param name="StudyDays">Die vorgesehenen Lerntage.</param>
/// <param name="ExpectedNewlyMastered">Die erwartete Anzahl neu beherrschter Inhalte.</param>
/// <param name="ConservativeNewlyMastered">Die konservative Anzahl neu beherrschter Inhalte.</param>
/// <param name="ExpectedQuotePercent">Die erwartete Erfolgsquote in Prozent.</param>
/// <param name="ConservativeQuotePercent">Die konservative Erfolgsquote in Prozent.</param>
/// <param name="Feasible">Gibt an, ob das Lernziel mit den Vorgaben erreichbar ist.</param>
/// <param name="SuggestedDailyMinutes">Die vorgeschlagene tägliche Lernzeit in Minuten.</param>
/// <param name="SuggestedScopeContents">Der vorgeschlagene Lernumfang.</param>
/// <param name="SuggestedExamDate">Der vorgeschlagene Prüfungstermin.</param>
/// <param name="Options">Die verfügbaren Antwortoptionen.</param>
/// <param name="Assumptions">Die für den Lösungsweg getroffenen Annahmen.</param>
public sealed record ExamPlanResult(
        string EstimatedDimension,
        int ScopeContents,
        int DailyMinutes,
        int TargetPercent,
        int AvailableContents,
        int MasteredContents,
        int PlanningDays,
        int StudyDays,
        int ExpectedNewlyMastered,
        int ConservativeNewlyMastered,
        int ExpectedQuotePercent,
        int ConservativeQuotePercent,
        bool Feasible,
        int? SuggestedDailyMinutes,
        int? SuggestedScopeContents,
        DateOnly? SuggestedExamDate,
        IReadOnlyList<string> Options,
        string Assumptions);
