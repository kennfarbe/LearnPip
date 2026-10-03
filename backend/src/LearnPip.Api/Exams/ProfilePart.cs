// <copyright file="ProfilePart.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;
public sealed record ProfilePart(string Code, string Title, string CatalogPartCode,
    int QuestionCount, int TimeLimitMinutes, int RequiredCorrect, string? CreditCode,
    bool AllowVariants = false, bool ShuffleAnswers = true);
