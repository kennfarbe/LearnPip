// <copyright file="TranslationDraftInput.cs" company="LearnPip contributors">
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
/// Anfrage zum Speichern eines Übersetzungsentwurfs.
/// </summary>
/// <param name="Language">Der Sprachcode.</param>
/// <param name="Payload">Die übersetzten Inhaltsblöcke.</param>
/// <param name="Source">Die Herkunft der Übersetzung.</param>
/// <param name="License">Die Inhaltslizenz.</param>
/// <param name="Provenance">Die Herkunft der Inhalte.</param>
public sealed record TranslationDraftInput(
        string Language,
        TranslationPayload Payload,
        string Source,
        string License,
        string Provenance);
