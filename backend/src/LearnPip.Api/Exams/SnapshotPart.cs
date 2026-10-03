// <copyright file="SnapshotPart.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;
public sealed record SnapshotPart(ProfilePart Rule, bool Credited,
    IReadOnlyList<CatalogQuestion> Questions);
