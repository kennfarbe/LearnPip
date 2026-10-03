// <copyright file="ModerationActionInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;
/// <summary>
/// Moderationsentscheidung mit Begründung und optional korrigierter Frage.
/// </summary>
/// <param name="Action">Die gewünschte Moderationsaktion.</param>
/// <param name="Note">Die Begründung der Moderationsentscheidung.</param>
/// <param name="CorrectedPrompt">Die korrigierte Fragenformulierung, sofern vorhanden.</param>
public sealed record ModerationActionInput(string Action, string Note, string? CorrectedPrompt);
