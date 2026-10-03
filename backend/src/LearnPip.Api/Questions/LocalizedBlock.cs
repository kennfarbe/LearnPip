// <copyright file="LocalizedBlock.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LearnPip.Api.Ai;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Übersetzter Text- oder Medienblock.
/// </summary>
/// <param name="Kind">Die Art des Inhaltsblocks oder Ereignisses.</param>
/// <param name="Text">Der Textinhalt, sofern vorhanden.</param>
/// <param name="MediaId">Die Kennung des zugehörigen Mediums, sofern vorhanden.</param>
/// <param name="AltText">Die alternative Textbeschreibung des Bilds.</param>
public sealed record LocalizedBlock(string Kind, string? Text, Guid? MediaId, string? AltText);
