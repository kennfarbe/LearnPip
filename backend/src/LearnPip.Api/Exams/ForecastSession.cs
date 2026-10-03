// <copyright file="ForecastSession.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Questions;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;
public sealed record ForecastSession(DateOnly Date, string Place, DateOnly? RegistrationDeadline,
    string RegistrationStatus, string SourceUrl, DateOnly CheckedOn);
