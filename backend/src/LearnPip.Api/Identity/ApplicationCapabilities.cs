// <copyright file="ApplicationCapabilities.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Identity;

/// <summary>
/// Aktuell verfügbare Verwaltungsbereiche gemäß den Autorisierungsrichtlinien der API.
/// </summary>
/// <param name="Administration">Gibt an, ob eine aktuelle Administratorberechtigung vorliegt.</param>
/// <param name="Moderation">Gibt an, ob öffentliche Einreichungen geprüft werden dürfen.</param>
public sealed record ApplicationCapabilities(bool Administration, bool Moderation);
