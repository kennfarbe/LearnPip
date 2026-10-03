// <copyright file="DraftView.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;
/// <summary>
/// Übersicht eines Fragenentwurfs mit Katalogzuordnung.
/// </summary>
/// <param name="QuestionId">Die Kennung der Frage.</param>
/// <param name="CatalogId">Die Kennung des Katalogs, sofern zugeordnet.</param>
/// <param name="LatestVersion">Die neueste Fragenfassung, sofern vorhanden.</param>
/// <param name="UpdatedAtUtc">Den letzten Änderungszeitpunkt in UTC.</param>
/// <param name="Content">Der Inhalt der Fragenfassung.</param>
public sealed record DraftView(
        Guid QuestionId,
        Guid? CatalogId,
        int LatestVersion,
        DateTimeOffset UpdatedAtUtc,
        QuestionPublishRequest Content);
