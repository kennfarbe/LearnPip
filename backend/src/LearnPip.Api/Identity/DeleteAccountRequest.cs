// <copyright file="DeleteAccountRequest.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Identity;

/// <summary>
/// Anfrage zur bestätigten Löschung eines Kontos.
/// </summary>
/// <param name="Confirmation">Die ausdrückliche Bestätigung der Kontolöschung.</param>
/// <param name="RecoverySecret">Das Wiederherstellungsgeheimnis des Kontos.</param>
public sealed record DeleteAccountRequest(string? Confirmation, string? RecoverySecret);
