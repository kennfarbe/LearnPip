// <copyright file="GradeRequest.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.RegularExpressions;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;
/// <summary>
/// Antwortkennungen zur Bewertung einer veröffentlichten Fragenfassung.
/// </summary>
/// <param name="VersionId">Die Kennung der Fragenfassung.</param>
/// <param name="SelectedOptionIds">Die vom Benutzer ausgewählten Antwortkennungen.</param>
public sealed record GradeRequest(Guid VersionId, IReadOnlyList<Guid> SelectedOptionIds);
