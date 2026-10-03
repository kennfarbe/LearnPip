// <copyright file="PasswordCredential.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>Speichert ausschließlich den gesalzenen Passwort-Hash eines lokalen Zugangs.</summary>
public sealed class PasswordCredential
{
    /// <summary>Holt oder setzt die Kontokennung.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Holt oder setzt den normalisierten Benutzernamen.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Holt oder setzt den versionierten Passwort-Hash.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Holt oder setzt die Anzahl fehlgeschlagener Versuche.</summary>
    public int FailedAttempts { get; set; }

    /// <summary>Holt oder setzt das Ende einer vorübergehenden Anmeldesperre.</summary>
    public DateTimeOffset? LockedUntilUtc { get; set; }

    /// <summary>Holt oder setzt das zugehörige Konto.</summary>
    public Account Account { get; set; } = null!;
}
