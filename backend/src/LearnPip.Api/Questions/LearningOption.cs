// <copyright file="LearningOption.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using LearnPip.Api.Ai;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Antwortoption innerhalb einer Lernsitzung.
/// </summary>
/// <param name="Id">Die eindeutige Kennung.</param>
/// <param name="Blocks">Die Inhaltsblöcke der Antwort.</param>
public sealed record LearningOption(Guid Id, IReadOnlyList<ContentBlockOutput> Blocks);
