// <copyright file="LearningPlanItem.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using LearnPip.Api.Ai;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

internal sealed record LearningPlanItem(Guid QuestionId, Guid VersionId, Guid[] OptionIds,
    string State, string Language = "de", Guid? TranslationId = null);
