// <copyright file="PasswordChangeRequest.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Identity;

/// <summary>Fordert eine Passwortänderung nach Prüfung des bisherigen Passworts an.</summary>
/// <param name="CurrentPassword">Das bisherige Passwort.</param>
/// <param name="NewPassword">Das neue Passwort.</param>
public sealed record PasswordChangeRequest(string? CurrentPassword, string? NewPassword);
