// <copyright file="UpdateJob.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Administration;

/// <summary>Beschreibt einen geprüften Update-Auftrag.</summary>
public sealed class UpdateJob
{
    /// <summary>Holt oder setzt die eindeutige Auftragskennung.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    /// <summary>Holt oder setzt die Kennung des beauftragenden Administrators.</summary>
    public Guid ActorAccountId { get; init; }
    /// <summary>Holt oder setzt die Ausgangsversion.</summary>
    public string FromVersion { get; init; } = string.Empty;
    /// <summary>Holt oder setzt die Zielversion.</summary>
    public string TargetVersion { get; init; } = string.Empty;
    /// <summary>Holt oder setzt den Bearbeitungsstatus.</summary>
    public string State { get; set; } = "queued";
    /// <summary>Holt oder setzt die aktuelle Bearbeitungsphase.</summary>
    public string Phase { get; set; } = "queued";
    /// <summary>Holt oder setzt die optionale Statusmeldung.</summary>
    public string? Message { get; set; }
    /// <summary>Holt oder setzt den Erstellungszeitpunkt in UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    /// <summary>Holt oder setzt den Änderungszeitpunkt in UTC.</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
