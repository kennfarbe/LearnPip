// <copyright file="ReviewOverview.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;
public sealed record ReviewOverview(int TotalContents, int MasteredContents, int OftenForMeCount,
    IReadOnlyList<LearningContentView> Contents);
