// <copyright file="IEmailCodeSender.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LearnPip.Api.Identity;

/// <summary>
/// Schnittstelle für den Versand kurzlebiger E-Mail-Anmeldecodes.
/// </summary>
public interface IEmailCodeSender
{
    /// <summary>
    /// Holt einen Wert, der angibt, ob der E-Mail-Versand konfiguriert ist.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Sendet einen Anmeldecode an die angegebene E-Mail-Adresse.
    /// </summary>
    /// <param name="email">Die E-Mail-Adresse des Kontos.</param>
    /// <param name="code">Der Rollen-, Einladungs- oder Bestätigungscode.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    Task SendAsync(string email, string code, CancellationToken cancellationToken);
}
