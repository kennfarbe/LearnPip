// <copyright file="LifecycleRunResult.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Data;

public sealed record LifecycleRunResult(int WarningsClaimed, int Deactivated, int Deleted);
