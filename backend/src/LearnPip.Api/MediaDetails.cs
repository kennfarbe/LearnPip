// <copyright file="MediaDetails.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api;

/// <summary>
/// Metadaten eines gespeicherten Mediums.
/// </summary>
/// <param name="AltText">Die alternative Bildbeschreibung.</param>
public sealed record MediaDetails(Guid Id, string MediaType, long ByteLength, Guid? QuestionVersionId, string AltText);
