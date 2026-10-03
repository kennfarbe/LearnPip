// <copyright file="LearningFeedback.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using LearnPip.Api.Ai;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;
public sealed record LearningFeedback(Guid AttemptId, Guid ContentId, bool IsCorrect,
    IReadOnlyList<Guid> CorrectOptionIds,
    IReadOnlyList<ContentBlockOutput> Explanation, string? ShortExplanation);
