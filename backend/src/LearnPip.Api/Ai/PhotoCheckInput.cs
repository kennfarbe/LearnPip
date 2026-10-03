// <copyright file="PhotoCheckInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LearnPip.Api.Media;
using LearnPip.Api.Questions;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Ai;
public sealed record PhotoCheckInput(string? Formula, string? ComputedSolution,
    string? ReferenceSolution, string? ChosenAnswer, IReadOnlyList<string>? Steps,
    string? QuestionText);
