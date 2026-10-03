// <copyright file="PasswordLoginRequest.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Identity;

/// <summary>Überträgt lokale Anmeldedaten nur im geschützten Anfragekörper.</summary>
/// <param name="Username">Der lokale Benutzername.</param>
/// <param name="Password">Das unverarbeitete Passwort.</param>
public sealed record PasswordLoginRequest(string? Username, string? Password);
