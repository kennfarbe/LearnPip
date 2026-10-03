// <copyright file="PublishedQuestionVersion.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.RegularExpressions;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;
public sealed record PublishedQuestionVersion(
    Guid Id, int Version, string SelectionMode, string Subject, string Topic, string Language,
    string Source, string License, string AuthorAttribution,
    DateTimeOffset PublishedAtUtc, string Visibility,
    IReadOnlyList<ContentBlockOutput> Prompt, IReadOnlyList<ContentBlockOutput> Explanation,
    IReadOnlyList<AnswerOutput> Answers);
