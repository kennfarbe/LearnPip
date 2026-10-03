// <copyright file="GradeResult.cs" company="LearnPip contributors">
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
public sealed record GradeResult(Guid AttemptId, Guid VersionId, bool IsCorrect,
    IReadOnlyList<Guid> SelectedOptionIds, IReadOnlyList<Guid> CorrectOptionIds);
