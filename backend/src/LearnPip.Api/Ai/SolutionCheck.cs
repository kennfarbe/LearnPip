// <copyright file="SolutionCheck.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Globalization;
using System.Text.RegularExpressions;

namespace LearnPip.Api.Ai;

public sealed record SolutionCheck(string Status, string Reason, string? Expected);
