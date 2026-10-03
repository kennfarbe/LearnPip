// <copyright file="AnswerSimulationInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;

/// <summary>
/// Antwort auf eine Frage der Prüfungssimulation.
/// </summary>
/// <param name="SelectedIndex">Der Index der gewählten Antwortoption.</param>
public sealed record AnswerSimulationInput(int SelectedIndex);
