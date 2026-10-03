// <copyright file="ContentBlockInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.RegularExpressions;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Eingabeblock für Text oder Medien einer Frage.
/// </summary>
/// <param name="Kind">Die Art des Inhaltsblocks oder Ereignisses.</param>
/// <param name="Text">Der Textinhalt, sofern vorhanden.</param>
/// <param name="MediaId">Die Kennung des zugehörigen Mediums, sofern vorhanden.</param>
public sealed record ContentBlockInput(string Kind, string? Text, Guid? MediaId);
