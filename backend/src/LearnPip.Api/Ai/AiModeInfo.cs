// <copyright file="AiModeInfo.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LearnPip.Api.Ai;

/// <summary>
/// Verfügbarkeit, Datenempfänger und Nutzungslimits eines KI-Modus.
/// </summary>
/// <param name="Mode">Der ausgewählte KI-Betriebsmodus.</param>
/// <param name="Available">Die Verfügbarkeit des Betriebsmodus.</param>
/// <param name="Recipient">Der Empfänger der an die KI übermittelten Daten.</param>
/// <param name="DataShared">Die Beschreibung der übermittelten Daten.</param>
/// <param name="DailyQuota">Das tägliche Nutzungslimit.</param>
/// <param name="MaxInputBytes">Die maximale Größe einer Textanfrage in Bytes.</param>
/// <param name="MaxImageBytes">Die maximale Größe einer Bildanfrage in Bytes.</param>
/// <param name="NeedsUserKey">Gibt an, ob ein persönlicher API-Schlüssel benötigt wird.</param>
public sealed record AiModeInfo(
        string Mode,
        bool Available,
        string Recipient,
        string DataShared,
        int DailyQuota,
        int MaxInputBytes,
        int MaxImageBytes,
        bool NeedsUserKey);
