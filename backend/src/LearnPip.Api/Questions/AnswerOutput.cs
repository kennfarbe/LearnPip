// <copyright file="AnswerOutput.cs" company="LearnPip contributors">
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
/// Veröffentlichte Antwortoption mit Kennung und Inhaltsblöcken.
/// </summary>
/// <param name="Id">Die eindeutige Kennung.</param>
/// <param name="IsCorrect">Gibt an, ob die Antwort richtig ist.</param>
/// <param name="Blocks">Die Inhaltsblöcke der Antwort.</param>
public sealed record AnswerOutput(Guid Id, bool IsCorrect, IReadOnlyList<ContentBlockOutput> Blocks);
