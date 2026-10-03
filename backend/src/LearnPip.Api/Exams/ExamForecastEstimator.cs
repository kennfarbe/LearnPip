// <copyright file="ExamForecastEstimator.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Questions;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;

public static class ExamForecastEstimator
{
    public static ExamForecast Estimate(ForecastEvidence evidence,
        IReadOnlyList<SimulationSignal> simulations, IReadOnlyList<ExamSession> sessions,
        int version, string? rulesSource, DateOnly? rulesChecked, DateOnly today)
    {
        var recent = simulations.Where(item =>
                DateOnly.FromDateTime(item.CompletedAtUtc.UtcDateTime) >= today.AddDays(-30))
            .OrderByDescending(item => item.CompletedAtUtc).Take(2).ToArray();
        var reasons = new List<string>();
        if (evidence.CatalogQuestions == 0 ||
            evidence.AnsweredCatalogQuestions * 100 < evidence.CatalogQuestions * 80)
            reasons.Add("Weniger als 80 % der Original-Fragegruppen wurden beantwortet.");
        if (evidence.OwnContents < 3 ||
            evidence.SpacedMasteredContents * 100 < evidence.OwnContents * 70)
            reasons.Add("Zu wenige eigene Lerninhalte haben drei zeitversetzte sichere Wiederholungen.");
        if (recent.Length < 2 || recent.Any(item => !item.Passed))
            reasons.Add("Es fehlen zwei vollständig bestandene Simulationen ohne angerechnete Teile aus den letzten 30 Tagen.");
        var earliest = reasons.Count == 0 ? today.AddDays(7) : (DateOnly?)null;
        var latest = reasons.Count == 0 ? today.AddDays(28) : (DateOnly?)null;
        var visible = sessions.OrderBy(item => item.Date).ThenBy(item => item.Place)
            .Select(item => new ForecastSession(item.Date, item.Place, item.RegistrationDeadline,
                item.RegistrationDeadline is null ? "unknown" :
                    item.RegistrationDeadline < today ? "closed" : "open",
                item.SourceUrl, item.CheckedOn)).ToArray();
        var suggestion = earliest is null ? null : visible.FirstOrDefault(item =>
            item.Date >= earliest && item.RegistrationStatus == "open" &&
            item.CheckedOn >= today.AddDays(-30) && rulesChecked >= today.AddDays(-30))?.Date;
        if (sessions.Count == 0) reasons.Add("Keine geprüften Termine in dieser Profilfassung.");
        else if (suggestion is null && earliest != null)
            reasons.Add("Kein Termin mit offener, kürzlich geprüfter Anmeldefrist verfügbar.");
        return new ExamForecast(earliest is null ? "insufficient" : "window", earliest,
            latest, suggestion, evidence, reasons,
            "Heuristik, keine Erfolgswahrscheinlichkeit: mindestens 80 % beantwortete " +
            "Original-Fragegruppen, mindestens 70 % eigene Inhalte mit drei zeitversetzten " +
            "sicheren Wiederholungen (mindestens drei Inhalte) und die letzten zwei " +
            "Simulationen derselben Profilfassung ohne angerechnete Teile in 30 Tagen " +
            "bestanden. Eigene Inhalte " +
            "sind nicht zuverlässig dem amtlichen Katalog zugeordnet. Das Fenster " +
            "7–28 Tage enthält einen Vorbereitungspuffer; weitere Übung und geänderte " +
            "Regeln können es verschieben. Termine werden nur bei Quellenstand bis " +
            "30 Tage und bekannter offener Frist vorgeschlagen.",
            "Nicht geprüft: formale Zulassung und tatsächliche Anmeldung müssen " +
            "bei der Prüfungsstelle separat geklärt werden.", rulesSource, rulesChecked,
            version, visible);
    }
}
