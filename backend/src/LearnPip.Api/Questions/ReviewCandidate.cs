// <copyright file="ReviewCandidate.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

internal sealed record ReviewCandidate(Guid QuestionId, Guid VersionId, Guid ContentId,
    string Title, string Subject, Guid? CatalogId);
