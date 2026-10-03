// <copyright file="LearningAnswerRequest.cs" company="LearnPip contributors">
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
/// Antwort auf eine Lernfrage einschließlich der Angabe eines geratenen Ergebnisses.
/// </summary>
/// <param name="SelectedOptionIds">Die vom Benutzer ausgewählten Antwortkennungen.</param>
/// <param name="WasGuessed">Gibt an, ob die Antwort geraten wurde.</param>
public sealed record LearningAnswerRequest(IReadOnlyList<Guid> SelectedOptionIds, bool WasGuessed = false);
