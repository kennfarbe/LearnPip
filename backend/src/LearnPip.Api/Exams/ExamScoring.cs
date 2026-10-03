// <copyright file="ExamScoring.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;

public static class ExamScoring
{
    public static PartResult Score(SnapshotPart part, IReadOnlyDictionary<string, int> answers,
        bool timedOut)
    {
        if (part.Credited)
            return new PartResult(part.Rule.Code, 0, part.Rule.QuestionCount, true, true, false);
        var correct = part.Questions.Count(question =>
            answers.TryGetValue(question.Code, out var index) && index == question.CorrectIndex);
        return new PartResult(part.Rule.Code, correct, part.Rule.QuestionCount,
            !timedOut && correct >= part.Rule.RequiredCorrect, false, timedOut);
    }

    public static bool Passed(IReadOnlyList<SnapshotPart> parts, IReadOnlyList<PartResult> results) =>
        parts.All(part => results.Any(result => result.Code == part.Rule.Code && result.Passed));
}
