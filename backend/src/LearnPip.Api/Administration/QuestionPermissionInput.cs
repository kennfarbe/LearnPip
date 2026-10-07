// <copyright file="QuestionPermissionInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Administration;

/// <summary>Bestätigte rollenbezogene Rechteänderung.</summary>
/// <param name="Rights">Vollständige Matrix.</param>
/// <param name="ExpectedValue">Konfiguration aus der Vorschau; schützt vor verlorenen Änderungen.</param>
/// <param name="Reason">Optionaler Änderungsgrund.</param>
/// <param name="ForeignAccessConfirmed">Getrennte Bestätigung weitreichender Inhaltsrechte.</param>
/// <param name="PrivacyConfirmed">Bestätigung des Betreiberhinweises zu privaten Inhalten.</param>
public sealed record QuestionPermissionInput(
    Dictionary<string, bool> Rights,
    string? ExpectedValue,
    string? Reason,
    bool ForeignAccessConfirmed,
    bool PrivacyConfirmed);
