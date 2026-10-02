// <copyright file="ApiResponse.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api;

/// <summary>
/// Antwortumschlag für eine API-Antwort.
/// </summary>

public sealed record ApiResponse<T>(T Data);
