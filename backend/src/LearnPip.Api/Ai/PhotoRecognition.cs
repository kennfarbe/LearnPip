// <copyright file="PhotoRecognition.cs" company="LearnPip contributors">
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
public sealed record PhotoRecognition(string DetectedText, string QuestionText, string Subject,
    string Topic, string? Formula, string? DrawingDescription, IReadOnlyList<string> Answers,
    int? SuggestedCorrectIndex, string ComputedSolution, IReadOnlyList<string> Steps,
    string? ReferenceSolution, IReadOnlyList<string> Uncertainties,
    string? Hint = null, string? NextStep = null);
