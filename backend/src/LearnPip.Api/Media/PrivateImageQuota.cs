// <copyright file="PrivateImageQuota.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Media;

/// <summary>Gemeinsames begrenztes Kontingent für Uploads und vollständige Katalogimporte.</summary>
public static class PrivateImageQuota
{
    /// <summary>Maximale Anzahl aktiver privater Bilder pro Konto.</summary>
    public const int MaxImages = 2000;

    /// <summary>Maximale Gesamtgröße aktiver privater Bilder pro Konto.</summary>
    public const long MaxBytes = 100L * 1024 * 1024;

    /// <summary>Prüft den Gesamtbestand einschließlich sämtlicher neuer Bilder.</summary>
    /// <param name="count">Gesamte Bildanzahl nach der Aktion.</param>
    /// <param name="bytes">Gesamte Byteanzahl nach der Aktion.</param>
    /// <returns>Ob der vollständige Bestand innerhalb beider Grenzen liegt.</returns>
    public static bool Fits(int count, long bytes) => count is >= 0 and <= MaxImages && bytes is >= 0 and <= MaxBytes;
}
