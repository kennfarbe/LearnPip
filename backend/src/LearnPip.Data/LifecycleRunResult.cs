// <copyright file="LifecycleRunResult.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Data;

/// <summary>
/// Fasst einen Durchlauf des Kontolebenszyklus zusammen.
/// </summary>
/// <param name="WarningsClaimed">Anzahl vorgemerkter Warnungen.</param>
/// <param name="Deactivated">Anzahl deaktivierter Konten.</param>
/// <param name="Deleted">Anzahl gelöschter Konten.</param>
public sealed record LifecycleRunResult(int WarningsClaimed, int Deactivated, int Deleted);
