// <copyright file="QuestionRights.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>Bindet ausdrücklich erfasste Rechteangaben an eine konkrete Inhaltsfassung.</summary>
public sealed class QuestionRights
{
    /// <summary>Holt oder setzt die eigene Fragenkennung.</summary>
    public Guid QuestionId { get; set; }

    /// <summary>Holt oder setzt den Inhaltsfingerabdruck der bestätigten Fassung.</summary>
    public string ContentSha256 { get; set; } = string.Empty;

    /// <summary>Holt oder setzt die unverändert archivierten früheren Einzelnachweise.</summary>
    public string HistoryJson { get; set; } = "[]";

    /// <summary>Holt oder setzt vollständige Frage- und Mediennachweise.</summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>Holt oder setzt den Zeitpunkt der Rechteangabe.</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
