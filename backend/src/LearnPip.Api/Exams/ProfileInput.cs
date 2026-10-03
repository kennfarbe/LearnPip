// <copyright file="ProfileInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;
public sealed record ProfileInput(string Code, string Title, string AmateurClass,
    Guid CatalogEditionId, IReadOnlyList<ProfilePart> Parts,
    IReadOnlyList<ExamSession>? Sessions = null, string? RulesSourceUrl = null,
    DateOnly? RulesCheckedOn = null);
