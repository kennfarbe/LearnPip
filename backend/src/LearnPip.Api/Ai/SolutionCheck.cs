// <copyright file="SolutionCheck.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Globalization;
using System.Text.RegularExpressions;

namespace LearnPip.Api.Ai;

/// <summary>
/// Ergebnis eines Lösungsvergleichs mit Begründung.
/// </summary>
/// <param name="Status">Der Status der Operation oder Prognose.</param>
/// <param name="Reason">Die Begründung des Prüfergebnisses oder der Meldung.</param>
/// <param name="Expected">Die erwartete Lösung, sofern bekannt.</param>
public sealed record SolutionCheck(string Status, string Reason, string? Expected);
