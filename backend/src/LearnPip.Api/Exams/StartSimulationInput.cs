// <copyright file="StartSimulationInput.cs" company="LearnPip contributors">
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
/// Anfrage zum Start einer Prüfungssimulation.
/// </summary>
/// <param name="ProfileVersionId">Die Kennung der Prüfungsprofilversion.</param>
/// <param name="QuestionMode">Der Modus für Originalfragen oder Varianten.</param>
public sealed record StartSimulationInput(Guid ProfileVersionId, string QuestionMode = "original");
