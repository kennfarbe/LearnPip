// <copyright file="ApiResponse.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api;

/// <summary>
/// Antwortumschlag für eine API-Antwort.
/// </summary>
/// <typeparam name="T">Der Typ der übertragenen Nutzdaten.</typeparam>
/// <param name="Data">Die übertragenen Nutzdaten.</param>
public sealed record ApiResponse<T>(T Data);
