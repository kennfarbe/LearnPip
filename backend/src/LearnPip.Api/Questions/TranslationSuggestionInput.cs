// <copyright file="TranslationSuggestionInput.cs" company="LearnPip contributors">
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
/// Anfrage zu einem KI-Übersetzungsvorschlag.
/// </summary>
/// <param name="Language">Der Sprachcode.</param>
/// <param name="Mode">Der ausgewählte KI-Betriebsmodus.</param>
/// <param name="DisclosureVersion">Die Version des zu bestätigenden Datenschutzhinweises.</param>
/// <param name="Confirmed">Gibt an, ob der Datenschutzhinweis bestätigt wurde.</param>
public sealed record TranslationSuggestionInput(
        string Language,
        string Mode,
        string DisclosureVersion,
        bool Confirmed);
