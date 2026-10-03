// <copyright file="QuestionPublishRequest.cs" company="LearnPip contributors">
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
public sealed record QuestionPublishRequest(
    string SelectionMode, string Subject, string Topic, string Language, string Source,
    string License, IReadOnlyList<ContentBlockInput> Prompt,
    IReadOnlyList<ContentBlockInput> Explanation, IReadOnlyList<AnswerInput> Answers);
