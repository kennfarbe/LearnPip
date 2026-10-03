// <copyright file="AiGenerateInput.cs" company="LearnPip contributors">
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
/// Anfrage zur KI-Erstellung nach bestätigter Datenschutzhinweisversion.
/// </summary>
/// <param name="Mode">Der ausgewählte KI-Betriebsmodus.</param>
/// <param name="Prompt">Die Text- oder Inhaltsblöcke der Frage.</param>
/// <param name="DisclosureVersion">Die Version des zu bestätigenden Datenschutzhinweises.</param>
/// <param name="Confirmed">Gibt an, ob der Datenschutzhinweis bestätigt wurde.</param>
public sealed record AiGenerateInput(
        string Mode,
        string Prompt,
        string DisclosureVersion,
        bool Confirmed);
