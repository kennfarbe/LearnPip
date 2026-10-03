// <copyright file="SmtpEmailCodeSender.cs" company="LearnPip contributors">
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

public sealed class SmtpEmailCodeSender(IConfiguration configuration) : IEmailCodeSender
{
    public bool IsAvailable =>
        !string.IsNullOrWhiteSpace(configuration["Mail:Host"]) &&
        !string.IsNullOrWhiteSpace(configuration["Mail:From"]) &&
        !string.IsNullOrWhiteSpace(configuration["Mail:Username"]) &&
        !string.IsNullOrWhiteSpace(configuration["Mail:Password"]);

    public async Task SendAsync(string email, string code, CancellationToken cancellationToken)
    {
        if (!this.IsAvailable)
        {
            throw new InvalidOperationException("Mail transport is not configured.");
        }

        var host = configuration["Mail:Host"]!;
        var port = int.TryParse(configuration["Mail:Port"], out var configuredPort)
            ? configuredPort : 587;
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(
                configuration["Mail:Username"], configuration["Mail:Password"])
        };
        using var message = new MailMessage(configuration["Mail:From"]!, email)
        {
            Subject = "LearnPip Anmeldecode",
            Body = $"Dein einmaliger LearnPip-Code lautet: {code}\nEr ist 10 Minuten gültig."
        };
        await client.SendMailAsync(message, cancellationToken);
    }
}
