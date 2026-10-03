// <copyright file="ReportInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Meldung eines möglichen Problems mit einer öffentlichen Frage.
/// </summary>
/// <param name="Reason">Die Begründung des Prüfergebnisses oder der Meldung.</param>
/// <param name="Details">Die ergänzende Beschreibung der Meldung.</param>
public sealed record ReportInput(string Reason, string Details);
