// <copyright file="AiKeyVault.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LearnPip.Api.Ai;

/// <summary>
/// Verschlüsselt persönliche KI-Schlüssel mit kontogebundenen Zusatzdaten.
/// </summary>
public static class AiKeyVault
{
    /// <summary>
    /// Die Verfügbarkeit des Betriebsmodus.
    /// </summary>
    /// <param name="config">Die Anwendungskonfiguration.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static bool Available(IConfiguration config)
    {
        try
        {
            return Key(config).Length == 32;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    /// <summary>
    /// Verschlüsselt einen persönlichen KI-Schlüssel für das angegebene Konto.
    /// </summary>
    /// <param name="secret">Das unverarbeitete Geheimnis.</param>
    /// <param name="accountId">Die Kennung des betroffenen Kontos.</param>
    /// <param name="config">Die Anwendungskonfiguration.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static string Seal(
        string secret,
        Guid accountId,
        IConfiguration config)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(secret);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(Key(config), 16);
        aes.Encrypt(nonce, plain, cipher, tag, accountId.ToByteArray());
        return Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
    }

    /// <summary>
    /// Entschlüsselt einen kontogebundenen persönlichen KI-Schlüssel.
    /// </summary>
    /// <param name="sealedValue">Der kontogebunden verschlüsselte Schlüssel.</param>
    /// <param name="accountId">Die Kennung des betroffenen Kontos.</param>
    /// <param name="config">Die Anwendungskonfiguration.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static string Open(
        string sealedValue,
        Guid accountId,
        IConfiguration config)
    {
        var data = Convert.FromBase64String(sealedValue);
        if (data.Length < 29)
        {
            throw new CryptographicException("Invalid encrypted key.");
        }

        var plain = new byte[data.Length - 28];
        using var aes = new AesGcm(Key(config), 16);
        aes.Decrypt(
            data.AsSpan(0, 12),
            data.AsSpan(28),
            data.AsSpan(12, 16),
            plain,
            accountId.ToByteArray());
        return Encoding.UTF8.GetString(plain);
    }

    private static byte[] Key(IConfiguration config)
    {
        var encoded = config["Ai:KeyEncryptionKey"] ?? string.Empty;
        var bytes = Convert.FromBase64String(encoded);
        if (bytes.Length != 32)
        {
            throw new CryptographicException("Invalid AI encryption key.");
        }

        return bytes;
    }
}
