// <copyright file="ReviewSchedule.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Questions;

public sealed record ReviewEvent(Guid AttemptId, DateTimeOffset AtUtc, string Kind, bool IsCorrect);
public sealed record ReviewState(int ConfidentStreak, DateTimeOffset? DueAtUtc, int Answers,
    int Guesses, int ExplanationsViewed, bool Mastered);

/// <summary>Pure replay: the same ordered events always produce the same learning state.</summary>
public static class ReviewSchedule
{
    public static ReviewState Replay(IEnumerable<ReviewEvent> events)
    {
        var streak = 0;
        var answers = 0;
        var guesses = 0;
        var explanations = 0;
        DateTimeOffset? due = null;
        foreach (var item in events.OrderBy(item => item.AtUtc).ThenBy(item => item.AttemptId)
                     .ThenBy(item => item.Kind == "answer" ? 0 : 1))
        {
            switch (item.Kind)
            {
                case "answer":
                    answers++;
                    if (!item.IsCorrect)
                    {
                        streak = 0;
                        due = item.AtUtc.AddDays(1);
                    }
                    else
                    {
                        streak++;
                        due = item.AtUtc.AddDays(streak switch
                        {
                            1 => 1,
                            2 => 3,
                            3 => 7,
                            _ => 14
                        });
                    }
                    break;
                case "guess":
                    guesses++;
                    streak = 0;
                    due = item.AtUtc.AddDays(1);
                    break;
                case "explanation":
                    explanations++;
                    streak = 0;
                    due = item.AtUtc.AddDays(1);
                    break;
            }
        }
        return new ReviewState(streak, due, answers, guesses, explanations, streak >= 3);
    }
}
