// <copyright file="TranslationDraftCreated.cs" company="LearnPip contributors">
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
/// Kennung und Status eines angelegten Übersetzungsentwurfs.
/// </summary>
/// <param name="Id">Die eindeutige Kennung.</param>
/// <param name="Revision">Die Revision der Daten.</param>
/// <param name="Status">Der Status der Operation oder Prognose.</param>
public sealed record TranslationDraftCreated(Guid Id, int Revision, string Status);
