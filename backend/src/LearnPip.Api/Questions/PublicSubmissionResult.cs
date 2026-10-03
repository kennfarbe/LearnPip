// <copyright file="PublicSubmissionResult.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Cryptography;
using LearnPip.Api.Identity;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Status einer öffentlichen Frageneinreichung.
/// </summary>
/// <param name="Status">Der Status der Operation oder Prognose.</param>
/// <param name="QuestionVersionId">Die Kennung der Fragenfassung.</param>
public sealed record PublicSubmissionResult(string Status, Guid QuestionVersionId);
