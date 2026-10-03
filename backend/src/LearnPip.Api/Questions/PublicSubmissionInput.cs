// <copyright file="PublicSubmissionInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Cryptography;
using LearnPip.Api.Identity;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

public sealed record PublicSubmissionInput(string PreviewToken, string LicenseChoice,
    string AuthorAttribution, bool RightsConfirmed, bool ImageRightsConfirmed,
    string AgeDeclaration);
