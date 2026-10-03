// <copyright file="MediaDetails.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api;

/// <summary>
/// Metadaten eines gespeicherten Mediums.
/// </summary>
/// <param name="AltText">Die alternative Bildbeschreibung.</param>
/// <param name="Id">Die eindeutige Kennung.</param>
/// <param name="MediaType">Der MIME-Typ des Mediums.</param>
/// <param name="ByteLength">Die Größe der Mediendaten in Bytes.</param>
/// <param name="QuestionVersionId">Die Kennung der Fragenfassung.</param>
public sealed record MediaDetails(Guid Id, string MediaType, long ByteLength, Guid? QuestionVersionId, string AltText);
