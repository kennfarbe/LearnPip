// <copyright file="CatalogPackagePreview.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Text.Json;

namespace LearnPip.Api.CatalogPackages;

/// <summary>Zeigt geprüfte Inhalte vor einer privaten Importbestätigung.</summary>
/// <param name="PackageId">Die externe Paketkennung.</param>
/// <param name="Title">Der Originaltitel.</param>
/// <param name="CatalogVersion">Die Inhaltsversion.</param>
/// <param name="SchemaVersion">Die Formatversion.</param>
/// <param name="SourceRevision">Der Quellenstand.</param>
/// <param name="Language">Die Paketsprache.</param>
/// <param name="QuestionCount">Die Anzahl der Fragen.</param>
/// <param name="MediaCount">Die Anzahl der Medien.</param>
/// <param name="ArchiveBytes">Die Originalgröße des ZIP-Archivs.</param>
/// <param name="ExpandedBytes">Die Größe aller entpackten Originaldateien.</param>
/// <param name="License">Die Übersichtsangaben zur Lizenz.</param>
/// <param name="QuestionLicenses">Alle eigenständigen Frage- und Medienlizenzen.</param>
/// <param name="Notices">Die originalen Lizenz- und Attributionstexte.</param>
/// <param name="Topics">Die enthaltenen Themen.</param>
/// <param name="ArchiveSha256">Die Prüfsumme zur Bindung der Bestätigung an die Vorschau.</param>
/// <param name="State">Neu, bereits importiert oder Konflikt.</param>
/// <param name="CatalogId">Der vorhandene private Zielkatalog.</param>
public sealed record CatalogPackagePreview(
    string PackageId,
    string Title,
    string CatalogVersion,
    string SchemaVersion,
    string SourceRevision,
    string Language,
    int QuestionCount,
    int MediaCount,
    long ArchiveBytes,
    long ExpandedBytes,
    JsonElement License,
    IReadOnlyList<JsonElement> QuestionLicenses,
    IReadOnlyDictionary<string,
    string> Notices,
    IReadOnlyList<string> Topics,
    string ArchiveSha256,
    string State,
    Guid? CatalogId);
