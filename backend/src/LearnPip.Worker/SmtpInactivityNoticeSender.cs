// <copyright file="SmtpInactivityNoticeSender.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Mail;
using LearnPip.Data;

namespace LearnPip.Worker;

/// <summary>
/// Versendet Hinweise zum Kontolebenszyklus über den konfigurierten SMTP-Server.
/// </summary>
/// <param name="configuration">Die SMTP- und Ursprungskonfiguration.</param>
public sealed class SmtpInactivityNoticeSender(IConfiguration configuration) : IInactivityNoticeSender
{
    /// <inheritdoc/>
    public bool IsAvailable =>
        !string.IsNullOrWhiteSpace(configuration["Mail:Host"]) &&
        !string.IsNullOrWhiteSpace(configuration["Mail:From"]) &&
        !string.IsNullOrWhiteSpace(configuration["Mail:Username"]) &&
        !string.IsNullOrWhiteSpace(configuration["Mail:Password"]);

    /// <inheritdoc/>
    public async Task SendAsync(
        string email,
        int phaseDays,
        DateTimeOffset lastActivityAtUtc,
        CancellationToken cancellationToken)
    {
        if (!this.IsAvailable)
        {
            throw new InvalidOperationException("Mail transport is not configured.");
        }

        var port = int.TryParse(
            configuration["Mail:Port"],
            out var configuredPort)
            ? configuredPort : 587;
        using var client = new SmtpClient(
            configuration["Mail:Host"]!,
            port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(
            configuration["Mail:Username"],
            configuration["Mail:Password"]),
        };
        var origin = configuration["Authentication:PublicOrigin"]?.TrimEnd('/');
        var link = Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Scheme == "https"
            ? $"\nDeine LearnPip-Instanz: {origin}" : string.Empty;
        using var message = new MailMessage(
            configuration["Mail:From"]!,
            email)
        {
            Subject = "LearnPip: Hinweis zu deinem inaktiven Konto",
            Body = $"Du hast LearnPip seit {phaseDays} Tagen nicht verwendet. " +
                "Eine erfolgreiche Anmeldung oder ein authentifizierter API-Aufruf " +
                "hält dein Konto aktiv.\n" +
                $"Ohne Aktivität wird es frühestens am {lastActivityAtUtc.AddDays(90):dd.MM.yyyy} " +
                "deaktiviert und frühestens 90 Tage später gelöscht. " +
                "Ein deaktiviertes Konto kannst du bis zur Löschung mit deinem " +
                "Wiederherstellungsgeheimnis, deiner verknüpften E-Mail oder OIDC reaktivieren." +
                link,
        };
        await client.SendMailAsync(message, cancellationToken);
    }
}
