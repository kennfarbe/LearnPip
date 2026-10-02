// <copyright file="IInactivityNoticeSender.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data;

/// <summary>
/// Sendet Hinweise zum Ablauf inaktiver Konten.
/// </summary>
public interface IInactivityNoticeSender
{
    /// <summary>Gibt an, ob der Benachrichtigungsversand verfügbar ist.</summary>
    bool IsAvailable { get; }

    /// <summary>Versendet eine Warnung vor der Kontodeaktivierung oder Löschung.</summary>
    /// <param name="email">Empfangsadresse.</param>
    /// <param name="phaseDays">Aktuelle Inaktivitätsphase in Tagen.</param>
    /// <param name="lastActivityAtUtc">Zeitpunkt der letzten Aktivität.</param>
    /// <param name="cancellationToken">Token zum Abbrechen.</param>
    /// <returns>Ein Task für den Versand.</returns>
    Task SendAsync(
        string email,
        int phaseDays,
        DateTimeOffset lastActivityAtUtc,
        CancellationToken cancellationToken);
}
