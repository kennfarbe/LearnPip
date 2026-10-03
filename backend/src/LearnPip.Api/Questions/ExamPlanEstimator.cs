// <copyright file="ExamPlanEstimator.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Questions;

/// <summary>Capacity estimate, not a predicted exam result. A content needs three spaced answers.</summary>
public static class ExamPlanEstimator
{
    /// <summary>
    /// Prüft, ob zeitlich getrennte erfolgreiche Antworten eine sichere Beherrschung belegen.
    /// </summary>
    /// <param name="events">Die zur Rekonstruktion des Wiederholungsstands verwendeten Ereignisse.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static bool HasSpacedMastery(IEnumerable<ReviewEvent> events)
    {
        var streak = 0;
        DateOnly? due = null;
        foreach (var item in events.OrderBy(item => item.AtUtc).ThenBy(item => item.AttemptId)
                     .ThenBy(item => item.Kind == "answer" ? 0 : 1))
        {
            if (item.Kind != "answer" || !item.IsCorrect)
            {
                streak = 0;
                due = null;
                continue;
            }

            var day = DateOnly.FromDateTime(item.AtUtc.UtcDateTime);
            if (due.HasValue && day < due.Value)
            {
                continue;
            }

            streak++;
            due = day.AddDays(streak == 1 ? 1 : streak == 2 ? 3 : 7);
        }

        return streak >= 3;
    }

    /// <summary>
    /// Berechnet eine konservative Lern- oder Prüfungsprognose aus den Eingabedaten.
    /// </summary>
    /// <param name="input">Die zu prüfenden Eingabedaten.</param>
    /// <param name="available">Die Anzahl verfügbarer Lerninhalte.</param>
    /// <param name="mastered">Die Anzahl sicher beherrschter Inhalte.</param>
    /// <param name="start">Der Beginn des betrachteten Zeitraums.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static ExamPlanResult Estimate(
        ExamPlanInput input,
        int available,
        int mastered,
        DateOnly start)
    {
        var days = input.ExamDate.HasValue
            ? input.ExamDate.Value.DayNumber - start.DayNumber
            : input.HorizonDays ?? 28;
        var school = (input.SchoolDays ?? []).ToHashSet();
        var breaks = (input.BreakDays ?? []).ToHashSet();
        var cap = input.DailyLimitMinutes;
        var missing = input.ScopeContents == null ? "scope" :
            input.DailyMinutes == null ? "time" : "target";
        int scope;
        int minutes;
        int target;
        if (missing == "scope")
        {
            minutes = input.DailyMinutes!.Value;
            target = input.TargetPercent!.Value;

            // The number is a capacity bound. It does not invent available learning material.
            var capacity = Simulate(2000, days, minutes, cap, start, school, breaks, 0.8);
            scope = Math.Min(available, (int)Math.Floor((mastered + capacity) * 100.0 / target));
        }
        else
        {
            scope = input.ScopeContents!.Value;
            if (missing == "time")
            {
                target = input.TargetPercent!.Value;
                var needed = Math.Max(0, (int)Math.Ceiling(scope * target / 100.0) - mastered);
                minutes = MinimumMinutes(needed, scope - mastered, days, cap, start, school, breaks);
            }
            else
            {
                minutes = input.DailyMinutes!.Value;
                target = Math.Min(100, (mastered + Simulate(scope - mastered, days, minutes, cap, start, school, breaks, 0.8)) * 100 / scope);
            }
        }

        var expected = Simulate(
            Math.Max(0, scope - mastered),
            days,
            minutes,
            cap,
            start,
            school,
            breaks,
            0.8);
        var conservative = Simulate(
            Math.Max(0, scope - mastered),
            days,
            minutes,
            cap,
            start,
            school,
            breaks,
            0.6);
        var neededForTarget = (int)Math.Ceiling(scope * target / 100.0);
        var feasible = scope > 0 && mastered + expected >= neededForTarget && minutes <= cap;
        int? suggestedMinutes = feasible ? null : MinimumMinutes(
            Math.Max(0, neededForTarget - mastered),
            scope - mastered,
            days,
            cap,
            start,
            school,
            breaks);
        int? suggestedScope = feasible ? null : Math.Min(scope, target == 0 ? scope : (int)Math.Floor((mastered + expected) * 100.0 / target));
        DateOnly? suggestedDate = null;
        if (!feasible && minutes > 0 && days < 365 &&
            mastered + Simulate(
            scope - mastered,
            365,
            Math.Min(minutes, cap),
            cap,
            start,
            school,
            breaks,
            0.8) >= neededForTarget)
        {
            var low = days + 1;
            var high = 365;
            while (low < high)
            {
                var mid = (low + high) / 2;
                if (mastered + Simulate(
                    scope - mastered,
                    mid,
                    Math.Min(minutes, cap),
                    cap,
                    start,
                    school,
                    breaks,
                    0.8) >= neededForTarget)
                {
                    high = mid;
                }
                else
                {
                    low = mid + 1;
                }
            }

            suggestedDate = start.AddDays(low);
        }

        var studyDays = Enumerable.Range(0, days).Count(index =>
            !breaks.Contains(start.AddDays(index)));
        var options = new List<string>();
        if (!feasible)
        {
            if (suggestedDate.HasValue)
            {
                options.Add("Prüfungstermin verschieben");
            }

            if (suggestedMinutes.HasValue)
            {
                options.Add("Lernzeit pro Tag erhöhen");
            }

            if (suggestedScope.HasValue)
            {
                options.Add("Stoffumfang reduzieren");
            }

            options.Add("Zielquote anpassen");
        }

        return new ExamPlanResult(
            missing,
            scope,
            minutes,
            target,
            available,
            mastered,
            days,
            studyDays,
            expected,
            conservative,
            Math.Min(100, (mastered + expected) * 100 / Math.Max(scope, 1)),
            Math.Min(100, (mastered + conservative) * 100 / Math.Max(scope, 1)),
            feasible,
            suggestedMinutes,
            suggestedScope,
            suggestedDate,
            options,
            "Schätzung ohne Erfolgsgarantie: je Lerninhalt 15 Minuten Einstieg und drei " + "zeitversetzte sichere Wiederholungen zu je 5 Minuten (Tag 0, 1 und 4). " + "20 % Reserve, vorsichtig 40 %; Schultage halbe Kapazität; Pausentage frei. " + "Beherrschung wird nur aus verschiedenen Lerninhalten mit drei sicheren Antworten gezählt.");
    }

    private static int MinimumMinutes(
        int needed,
        int remaining,
        int days,
        int cap,
        DateOnly start,
        HashSet<int> school,
        HashSet<DateOnly> breaks)
    {
        if (needed <= 0)
        {
            return 0;
        }

        if (Simulate(remaining, days, cap, cap, start, school, breaks, 0.8) < needed)
        {
            return cap + 1;
        }

        var low = 1;
        var high = cap;
        while (low < high)
        {
            var mid = (low + high) / 2;
            if (Simulate(remaining, days, mid, cap, start, school, breaks, 0.8) >= needed)
            {
                high = mid;
            }
            else
            {
                low = mid + 1;
            }
        }

        return low;
    }

    private static int Simulate(
        int remaining,
        int days,
        int minutes,
        int cap,
        DateOnly start,
        HashSet<int> school,
        HashSet<DateOnly> breaks,
        double reserve)
    {
        if (remaining <= 0 || minutes <= 0)
        {
            return 0;
        }

        var states = new List<(int Stage, int Due)>(Math.Min(remaining, 2000));
        var completed = 0;
        for (var day = 0; day < days; day++)
        {
            var date = start.AddDays(day);
            if (breaks.Contains(date))
            {
                continue;
            }

            var fraction = school.Contains((int)date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek)
                ? 0.5 : 1.0;
            var budget = (int)Math.Floor(Math.Min(minutes, cap) * fraction * reserve);

            // Due reviews take priority. A postponed review stays due, never counts early.
            for (var i = 0; i < states.Count && budget >= 5; i++)
            {
                var state = states[i];
                if (state.Stage >= 3 || state.Due > day)
                {
                    continue;
                }

                budget -= 5;
                var stage = state.Stage + 1;
                states[i] = (stage, stage == 2 ? day + 3 : int.MaxValue);
                if (stage == 3)
                {
                    completed++;
                }
            }

            if (completed == remaining)
            {
                break;
            }

            while (budget >= 20 && states.Count < remaining)
            {
                budget -= 20;
                states.Add((1, day + 1));
            }
        }

        return completed;
    }
}
