// <copyright file="UpdateStatus.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Administration;

/// <summary>Beschreibt den Status der Release-Prüfung.</summary>
/// <param name="InstalledVersion">Installierte Version.</param>
/// <param name="LatestVersion">Verfügbare Version.</param>
/// <param name="State">Update-Status.</param>
/// <param name="Interval">Prüfintervall.</param>
/// <param name="LastCheckedAtUtc">Letzte Prüfung.</param>
/// <param name="NextCheckAtUtc">Nächste Prüfung.</param>
/// <param name="Error">Fehler.</param>
/// <param name="Release">Release-Metadaten.</param>
/// <param name="Job">Update-Auftrag.</param>
public sealed record UpdateStatus(
        string InstalledVersion,
        string? LatestVersion,
        string State,
        string Interval,
        DateTimeOffset? LastCheckedAtUtc,
        DateTimeOffset? NextCheckAtUtc,
        string? Error,
        ReleaseInfo? Release,
        UpdateJob? Job);
