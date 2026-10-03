// <copyright file="AnswerInput.cs" company="LearnPip contributors">
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
/// <summary>
/// Antwortoption mit Korrektheitsmarkierung und Inhaltsblöcken.
/// </summary>
/// <param name="IsCorrect">Gibt an, ob die Antwort richtig ist.</param>
/// <param name="Blocks">Die Inhaltsblöcke der Antwort.</param>
public sealed record AnswerInput(bool IsCorrect, IReadOnlyList<ContentBlockInput> Blocks);
