// <copyright file="AiKeyInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Ai;

/// <summary>
/// Anfrage zum Hinterlegen eines persönlichen KI-Schlüssels.
/// </summary>
/// <param name="Key">Der persönliche API-Schlüssel.</param>
public sealed record AiKeyInput(string Key);
