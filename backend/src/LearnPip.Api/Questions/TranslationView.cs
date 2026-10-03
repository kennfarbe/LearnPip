// <copyright file="TranslationView.cs" company="LearnPip contributors">
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
/// Status und Inhalt einer Übersetzung.
/// </summary>
/// <param name="Id">Die eindeutige Kennung.</param>
/// <param name="Revision">Die Revision der Daten.</param>
/// <param name="Language">Der Sprachcode.</param>
/// <param name="Source">Die Herkunft der Übersetzung.</param>
/// <param name="License">Die Inhaltslizenz.</param>
/// <param name="Provenance">Die Herkunft der Inhalte.</param>
/// <param name="Missing">Die Anzahl fehlender oder fälliger Inhalte.</param>
/// <param name="Payload">Die übersetzten Inhaltsblöcke.</param>
public sealed record TranslationView(
        Guid? Id,
        int Revision,
        string Language,
        string Source,
        string License,
        string Provenance,
        bool Missing,
        TranslationPayload Payload);
