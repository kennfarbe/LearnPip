// <copyright file="CatalogQuestion.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;

public sealed record CatalogQuestion(string Code, string PartCode, string Prompt,
    IReadOnlyList<string> Answers, int CorrectIndex, string Kind = "original",
    string? BaseCode = null);
