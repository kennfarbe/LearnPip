// <copyright file="ReviewCandidate.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Zur Wiederholung vorgesehene Frage mit persönlichem Lernstand.
/// </summary>
/// <param name="QuestionId">Die Kennung der Frage.</param>
/// <param name="VersionId">Die Kennung der Fragenfassung.</param>
/// <param name="ContentId">Die Kennung des Lerninhalts.</param>
/// <param name="Title">Der Titel.</param>
/// <param name="Subject">Das Fach oder Themengebiet.</param>
/// <param name="CatalogId">Die Kennung des Katalogs, sofern zugeordnet.</param>
internal sealed record ReviewCandidate(
        Guid QuestionId,
        Guid VersionId,
        Guid ContentId,
        string Title,
        string Subject,
        Guid? CatalogId);
