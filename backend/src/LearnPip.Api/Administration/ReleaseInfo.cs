// <copyright file="ReleaseInfo.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Administration;

/// <summary>Beschreibt ein stabiles LearnPip-Release.</summary>
/// <param name="Version">Versionskennung.</param>
/// <param name="Name">Bezeichnung.</param>
/// <param name="Notes">Veröffentlichungshinweise.</param>
/// <param name="Url">Release-Seite.</param>
/// <param name="PublishedAtUtc">Veröffentlichungszeitpunkt.</param>
public sealed record ReleaseInfo(string Version, string Name, string Notes, string Url, DateTimeOffset? PublishedAtUtc);
